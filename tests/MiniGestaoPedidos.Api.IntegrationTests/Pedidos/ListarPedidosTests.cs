using System.Net;
using System.Net.Http.Json;
using MiniGestaoPedidos.Api.IntegrationTests.Infra;
using MiniGestaoPedidos.Application.Common;
using MiniGestaoPedidos.Application.Pedidos;

namespace MiniGestaoPedidos.Api.IntegrationTests.Pedidos;

/// <summary>GET /pedidos e GET /pedidos/{id} — critérios CA09 a CA12.</summary>
public sealed class ListarPedidosTests(ApiFactory factory) : TesteDeApi(factory)
{
    private static DateTime Utc(int ano, int mes, int dia, int hora = 12, int minuto = 0, int segundo = 0, int ms = 0) =>
        new(ano, mes, dia, hora, minuto, segundo, ms, DateTimeKind.Utc);

    [Fact]
    public async Task CA09_SemFiltros_PrimeiraPaginaCom10MaisRecentesETotalGeral()
    {
        var cliente = await Banco.InserirClienteAsync();
        for (var dia = 1; dia <= 12; dia++)
        {
            await Banco.InserirPedidoAsync(cliente, Utc(2026, 1, dia));
        }

        var pagina = await Http.GetFromJsonAsync<PaginaResultado<PedidoResumoResponse>>("/pedidos");

        Assert.Equal(10, pagina!.Itens.Count);
        Assert.Equal(12, pagina.TotalItens);
        Assert.Equal(2, pagina.TotalPaginas);
        Assert.Equal(Utc(2026, 1, 12), pagina.Itens[0].DataPedido);
        Assert.Equal(pagina.Itens.OrderByDescending(p => p.DataPedido).ToList(), pagina.Itens);
    }

    [Fact]
    public async Task CA09_SegundaPagina_TrazORestante()
    {
        var cliente = await Banco.InserirClienteAsync();
        for (var dia = 1; dia <= 12; dia++)
        {
            await Banco.InserirPedidoAsync(cliente, Utc(2026, 1, dia));
        }

        var pagina = await Http.GetFromJsonAsync<PaginaResultado<PedidoResumoResponse>>("/pedidos?pagina=2");

        Assert.Equal(2, pagina!.Itens.Count);
        Assert.Equal(Utc(2026, 1, 1), pagina.Itens[^1].DataPedido);
    }

    [Fact]
    public async Task CA10_FiltroPorCliente_RetornaApenasPedidosDele()
    {
        var ana = await Banco.InserirClienteAsync("Ana");
        var bruno = await Banco.InserirClienteAsync("Bruno");
        await Banco.InserirPedidoAsync(ana, Utc(2026, 2, 1));
        await Banco.InserirPedidoAsync(ana, Utc(2026, 2, 2));
        await Banco.InserirPedidoAsync(bruno, Utc(2026, 2, 3));

        var pagina = await Http.GetFromJsonAsync<PaginaResultado<PedidoResumoResponse>>($"/pedidos?clienteId={ana}");

        Assert.Equal(2, pagina!.TotalItens);
        Assert.All(pagina.Itens, p => Assert.Equal(ana, p.ClienteId));
    }

    [Fact]
    public async Task CA11_FiltroPorPeriodo_IncluiODiaFinalInteiro()
    {
        var cliente = await Banco.InserirClienteAsync();
        var antes = await Banco.InserirPedidoAsync(cliente, Utc(2026, 2, 28, 23, 59, 59, 999));
        var inicio = await Banco.InserirPedidoAsync(cliente, Utc(2026, 3, 1, 0, 0));
        var fimDoUltimoDia = await Banco.InserirPedidoAsync(cliente, Utc(2026, 3, 31, 23, 59, 59, 997));
        var depois = await Banco.InserirPedidoAsync(cliente, Utc(2026, 4, 1, 0, 0));

        var pagina = await Http.GetFromJsonAsync<PaginaResultado<PedidoResumoResponse>>(
            "/pedidos?dataInicio=2026-03-01&dataFim=2026-03-31");

        var ids = pagina!.Itens.Select(p => p.Id).ToList();
        Assert.Equal([fimDoUltimoDia, inicio], ids);
        Assert.DoesNotContain(antes, ids);
        Assert.DoesNotContain(depois, ids);
    }

    [Theory]
    [InlineData("dataInicio=2026-03-02&dataFim=2026-03-01", "dataFim")]
    [InlineData("pagina=0", "pagina")]
    [InlineData("tamanhoPagina=0", "tamanhoPagina")]
    [InlineData("tamanhoPagina=101", "tamanhoPagina")]
    public async Task CA12_ParametrosInvalidos_Retorna400ComCampo(string queryString, string campo)
    {
        var resposta = await Http.GetAsync($"/pedidos?{queryString}");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.True((await LerJsonAsync(resposta)).GetProperty("errors").TryGetProperty(campo, out _));
    }

    [Fact]
    public async Task ObterPorId_PedidoInexistente_Retorna404()
    {
        var resposta = await Http.GetAsync("/pedidos/987654");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}
