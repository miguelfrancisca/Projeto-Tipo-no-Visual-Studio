using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using MiniGestaoPedidos.Api.IntegrationTests.Infra;

namespace MiniGestaoPedidos.Api.IntegrationTests.Pedidos;

/// <summary>
/// RN08 + RN14 — o instante gravado em Pedidos.DataPedido é EXATAMENTE o UTC do relógio do servidor
/// (truncado a milissegundos, a precisão da coluna datetime2(3)).
///
/// Lê o valor BRUTO por SQL (sem EF) e as datas como STRING no JSON (sem desserializar), para que
/// nenhuma conversão consiga mascarar outra. Esta máquina de desenvolvimento roda em UTC-03:00:
/// uma conversão pelo fuso local apareceria aqui como diferença de horas.
/// </summary>
public sealed class DataPedidoGravadaTests(ApiFactory factory) : TesteDeApi(factory)
{
    private readonly ApiFactory _factory = factory;

    [Theory]
    // Hora comum com sub-milissegundos: trunca .1234567 → .123.
    [InlineData("2026-11-12T14:30:00.1234567Z", "2026-11-12T14:30:00.123", "2026-11-12T14:30:00.123Z")]
    // Perto da meia-noite UTC: truncar (e não arredondar) mantém o pedido no dia 12.
    [InlineData("2026-11-12T23:59:59.9994567Z", "2026-11-12T23:59:59.999", "2026-11-12T23:59:59.999Z")]
    public async Task Criar_ComRelogioFixo_GravaEDevolveOInstanteUtcExato(string relogio, string esperadoNoBanco, string esperadoNoJson)
    {
        var agora = DateTimeOffset.Parse(relogio, System.Globalization.CultureInfo.InvariantCulture);
        using var http = ClienteComRelogio(agora);
        var cliente = await Banco.InserirClienteAsync();
        var produto = await Banco.InserirProdutoAsync(estoque: 10);

        var resposta = await http.PostAsJsonAsync("/pedidos", new
        {
            clienteId = cliente,
            itens = new[] { new { produtoId = produto, quantidade = 1 } },
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var criado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        var id = criado.GetProperty("id").GetInt32();

        // 1) Banco: valor bruto da coluna, sem passar pelo EF.
        Assert.Equal(esperadoNoBanco, await DataPedidoBrutaAsync(id));

        // 2) API: a mesma string em todos os pontos de leitura (201, GET por id, listagem).
        Assert.Equal(esperadoNoJson, criado.GetProperty("dataPedido").GetString());
        var porId = await http.GetFromJsonAsync<JsonElement>($"/pedidos/{id}");
        Assert.Equal(esperadoNoJson, porId.GetProperty("dataPedido").GetString());
        var lista = await http.GetFromJsonAsync<JsonElement>($"/pedidos?clienteId={cliente}");
        Assert.Equal(esperadoNoJson, lista.GetProperty("itens")[0].GetProperty("dataPedido").GetString());
    }

    /// <summary>Host da API com o TimeProvider substituído só para este teste (mesmo container SQL).</summary>
    private HttpClient ClienteComRelogio(DateTimeOffset agora) =>
        _factory
            .WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton<TimeProvider>(new RelogioFixo(agora))))
            .CreateClient();

    private async Task<string> DataPedidoBrutaAsync(int id)
    {
        await using var conexao = new SqlConnection(_factory.ConnectionString);
        await conexao.OpenAsync();
        await using var comando = new SqlCommand("SELECT CONVERT(varchar(23), DataPedido, 126) FROM Pedidos WHERE Id = @id", conexao);
        comando.Parameters.AddWithValue("@id", id);
        return (string)(await comando.ExecuteScalarAsync())!;
    }

    private sealed class RelogioFixo(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
    }
}
