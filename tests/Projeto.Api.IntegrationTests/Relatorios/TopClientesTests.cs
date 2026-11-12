using System.Net;
using System.Net.Http.Json;
using Projeto.Api.IntegrationTests.Infra;
using Projeto.Application.Relatorios;

namespace Projeto.Api.IntegrationTests.Relatorios;

/// <summary>GET /relatorios/top-clientes — critérios CA13 e CA14.</summary>
public sealed class TopClientesTests(ApiFactory factory) : TesteDeApi(factory)
{
    private static readonly DateTime Marco = new(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CA13_RetornaNoMaximo5OrdenadosPorValorConsiderandoSoOPeriodo()
    {
        // 6 clientes com compras em março; o de menor valor fica de fora do top 5.
        var valores = new[] { 500m, 900m, 100m, 700m, 300m, 50m };
        var ids = new List<int>();
        foreach (var valor in valores)
        {
            var id = await Banco.InserirClienteAsync($"Cliente {valor}");
            ids.Add(id);
            await Banco.InserirPedidoAsync(id, Marco, valor);
        }

        // Fora do período: não pode influenciar o ranking (senão o cliente de 50 venceria).
        await Banco.InserirPedidoAsync(ids[5], Marco.AddMonths(2), 10_000m);

        var top = await Http.GetFromJsonAsync<List<TopClienteResponse>>(
            "/relatorios/top-clientes?dataInicio=2026-03-01&dataFim=2026-03-31") ?? [];

        Assert.Equal([900m, 700m, 500m, 300m, 100m], top.Select(t => t.ValorTotal));
        Assert.DoesNotContain(top, t => t.ClienteId == ids[5]);
    }

    [Fact]
    public async Task CA13_SomaVariosPedidosEContaQuantidade()
    {
        var cliente = await Banco.InserirClienteAsync();
        await Banco.InserirPedidoAsync(cliente, Marco, 100m);
        await Banco.InserirPedidoAsync(cliente, Marco.AddDays(1), 250m);

        var top = await Http.GetFromJsonAsync<List<TopClienteResponse>>(
            "/relatorios/top-clientes?dataInicio=2026-03-01&dataFim=2026-03-31");

        var unico = Assert.Single(top!);
        Assert.Equal(350m, unico.ValorTotal);
        Assert.Equal(2, unico.QuantidadePedidos);
    }

    [Fact]
    public async Task CA13_HomonimosSaoClientesDiferentes()
    {
        // Mesmo bug da Parte 4: agrupar por nome juntaria os dois.
        var ana1 = await Banco.InserirClienteAsync("Ana Souza");
        var ana2 = await Banco.InserirClienteAsync("Ana Souza");
        await Banco.InserirPedidoAsync(ana1, Marco, 100m);
        await Banco.InserirPedidoAsync(ana2, Marco, 200m);

        var top = await Http.GetFromJsonAsync<List<TopClienteResponse>>(
            "/relatorios/top-clientes?dataInicio=2026-03-01&dataFim=2026-03-31");

        Assert.Equal([ana2, ana1], top!.Select(t => t.ClienteId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("?dataInicio=2026-01-01")]
    [InlineData("?dataFim=2026-01-31")]
    public async Task CA14_PeriodoAusente_Retorna400(string queryString)
    {
        var resposta = await Http.GetAsync($"/relatorios/top-clientes{queryString}");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }
}
