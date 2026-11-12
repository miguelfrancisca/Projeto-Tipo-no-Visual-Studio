using System.Net;
using System.Net.Http.Json;
using Projeto.Api.IntegrationTests.Infra;
using Projeto.Application.Common;
using Projeto.Application.Pedidos;

namespace Projeto.Api.IntegrationTests.Pedidos;

/// <summary>
/// RN14 — período no fuso de quem consulta (CA18–CA20), contra o SQL Server real.
///
/// Os testes rodam no mesmo processo da API. Nesta máquina de desenvolvimento o fuso local é
/// UTC-03:00 e na CI é UTC: os resultados esperados são os mesmos nos dois, o que só é possível
/// porque nenhuma conversão usa o fuso do servidor.
/// </summary>
public sealed class FiltroPorFusoTests(ApiFactory factory) : TesteDeApi(factory)
{
    private const string DiaDezEmSaoPaulo = "inicio=2025-03-10T00:00:00-03:00&fim=2025-03-11T00:00:00-03:00";

    private static DateTime Utc(int dia, int hora, int minuto = 0, int segundo = 0, int ms = 0) =>
        new(2025, 3, dia, hora, minuto, segundo, ms, DateTimeKind.Utc);

    private async Task<List<int>> IdsAsync(string queryString)
    {
        var resposta = await Http.GetAsync($"/pedidos?tamanhoPagina=100&{queryString}");
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return (await LerAsync<PaginaResultado<PedidoResumoResponse>>(resposta)).Itens.Select(p => p.Id).ToList();
    }

    [Fact]
    public async Task CA18_PedidoPertoDaMeiaNoiteUtc_CaiNoDiaDoFusoDeQuemConsulta()
    {
        var cliente = await Banco.InserirClienteAsync();
        var dia11Utc_dia10SP = await Banco.InserirPedidoAsync(cliente, Utc(11, 1, 30)); // 10/03 22:30 em SP
        var dia10Utc_dia09SP = await Banco.InserirPedidoAsync(cliente, Utc(10, 1, 30)); // 09/03 22:30 em SP

        var emSaoPaulo = await IdsAsync(DiaDezEmSaoPaulo);
        var emUtcComInstantes = await IdsAsync("inicio=2025-03-10T00:00:00Z&fim=2025-03-11T00:00:00Z");
        var emUtcLegado = await IdsAsync("dataInicio=2025-03-10&dataFim=2025-03-10");

        Assert.Equal([dia11Utc_dia10SP], emSaoPaulo);
        Assert.Equal([dia10Utc_dia09SP], emUtcComInstantes);
        Assert.Equal([dia10Utc_dia09SP], emUtcLegado); // forma antiga: comportamento inalterado
    }

    [Fact]
    public async Task CA19_InicioInclusivoEFimExclusivo()
    {
        var cliente = await Banco.InserirClienteAsync();
        var noInicio = await Banco.InserirPedidoAsync(cliente, Utc(10, 3));                    // = inicio
        var ultimoMs = await Banco.InserirPedidoAsync(cliente, Utc(11, 2, 59, 59, 999));       // fim - 1 ms
        var noFim = await Banco.InserirPedidoAsync(cliente, Utc(11, 3));                       // = fim
        var antes = await Banco.InserirPedidoAsync(cliente, Utc(10, 2, 59, 59, 999));          // inicio - 1 ms

        var ids = await IdsAsync(DiaDezEmSaoPaulo);

        Assert.Contains(noInicio, ids);
        Assert.Contains(ultimoMs, ids);
        Assert.DoesNotContain(noFim, ids);
        Assert.DoesNotContain(antes, ids);
    }

    [Fact]
    public async Task VariosDiasEmSaoPaulo_IncluiOUltimoDiaLocalInteiro()
    {
        var cliente = await Banco.InserirClienteAsync();
        var fimDoDia12SP = await Banco.InserirPedidoAsync(cliente, Utc(13, 2, 59));  // 12/03 23:59 em SP
        var inicioDoDia13SP = await Banco.InserirPedidoAsync(cliente, Utc(13, 3));   // 13/03 00:00 em SP
        var dia11 = await Banco.InserirPedidoAsync(cliente, Utc(11, 15));
        var fimDoDia09SP = await Banco.InserirPedidoAsync(cliente, Utc(10, 2, 59));  // 09/03 23:59 em SP

        var ids = await IdsAsync("inicio=2025-03-10T00:00:00-03:00&fim=2025-03-13T00:00:00-03:00");

        Assert.Equal([fimDoDia12SP, dia11], ids);
        Assert.DoesNotContain(inicioDoDia13SP, ids);
        Assert.DoesNotContain(fimDoDia09SP, ids);
    }

    [Fact]
    public async Task OffsetPositivoCodificado_EhRespeitado()
    {
        var cliente = await Banco.InserirClienteAsync();
        var dia10NaIndia = await Banco.InserirPedidoAsync(cliente, Utc(9, 19));  // 10/03 00:30 +05:30
        var dia11NaIndia = await Banco.InserirPedidoAsync(cliente, Utc(10, 19)); // 11/03 00:30 +05:30

        var ids = await IdsAsync("inicio=2025-03-10T00:00:00%2B05:30&fim=2025-03-11T00:00:00%2B05:30");

        Assert.Equal([dia10NaIndia], ids);
        Assert.DoesNotContain(dia11NaIndia, ids);
    }

    [Theory]
    [InlineData("inicio=2025-03-10T00:00:00", "inicio")]                        // sem offset: seria o fuso do servidor
    [InlineData("fim=2025-03-11T00:00:00", "fim")]
    [InlineData("inicio=2025-03-10", "inicio")]                                 // só data
    [InlineData("inicio=2025-03-10T00:00:00+05:30", "inicio")]                  // '+' cru vira espaço
    [InlineData("inicio=amanha", "inicio")]
    [InlineData("inicio=2025-03-10T00:00:00Z&inicio=2025-03-11T00:00:00Z", "inicio")] // repetido
    [InlineData("inicio=2025-03-10T03:00:00Z&fim=2025-03-10T00:00:00-03:00", "fim")]  // fim == inicio (mesmo instante)
    [InlineData("inicio=2025-03-11T00:00:00Z&fim=2025-03-10T00:00:00Z", "fim")]       // fim antes do inicio
    [InlineData("dataInicio=2025-03-10&fim=2025-03-11T00:00:00Z", "periodo")]         // formas misturadas
    [InlineData("inicio=2025-03-10T00:00:00Z&dataFim=2025-03-10", "periodo")]
    public async Task CA20_PeriodoInvalido_Retorna400NoCampoCerto(string queryString, string campo)
    {
        var resposta = await Http.GetAsync($"/pedidos?{queryString}");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var problema = await LerJsonAsync(resposta);
        Assert.Equal("Requisição inválida", problema.GetProperty("title").GetString());
        Assert.True(problema.GetProperty("errors").TryGetProperty(campo, out _), $"esperava erro em '{campo}': {problema}");
    }
}
