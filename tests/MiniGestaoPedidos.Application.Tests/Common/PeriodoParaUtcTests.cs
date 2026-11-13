using MiniGestaoPedidos.Application.Common;
using MiniGestaoPedidos.Application.Pedidos;

namespace MiniGestaoPedidos.Application.Tests.Common;

/// <summary>
/// RN14 — conversão do período para um intervalo UTC semiaberto, ANTES da consulta ao banco.
/// Os valores esperados são calculados à mão: o resultado não pode depender do fuso da máquina
/// que roda os testes (DateTimeOffset.UtcDateTime é aritmética pura sobre o offset informado).
/// </summary>
public class PeriodoParaUtcTests
{
    private static DateTime Utc(int ano, int mes, int dia, int hora = 0, int minuto = 0) =>
        new(ano, mes, dia, hora, minuto, 0, DateTimeKind.Utc);

    [Fact]
    public void ParaUtc_InstantesEmSaoPaulo_SomaTresHoras()
    {
        var filtro = new ListarPedidosQuery
        {
            Inicio = new DateTimeOffset(2025, 3, 10, 0, 0, 0, TimeSpan.FromHours(-3)),
            Fim = new DateTimeOffset(2025, 3, 11, 0, 0, 0, TimeSpan.FromHours(-3)),
        };

        var intervalo = Periodo.ParaUtc(filtro);

        Assert.Equal(Utc(2025, 3, 10, 3), intervalo.Inicio);
        Assert.Equal(Utc(2025, 3, 11, 3), intervalo.FimExclusivo);
        Assert.Equal(DateTimeKind.Utc, intervalo.Inicio!.Value.Kind);
    }

    [Fact]
    public void ParaUtc_InstantesEmUtc_MantemOsValores()
    {
        var filtro = new ListarPedidosQuery
        {
            Inicio = new DateTimeOffset(2025, 3, 10, 0, 0, 0, TimeSpan.Zero),
            Fim = new DateTimeOffset(2025, 3, 11, 0, 0, 0, TimeSpan.Zero),
        };

        var intervalo = Periodo.ParaUtc(filtro);

        Assert.Equal(Utc(2025, 3, 10), intervalo.Inicio);
        Assert.Equal(Utc(2025, 3, 11), intervalo.FimExclusivo);
    }

    [Fact]
    public void ParaUtc_OffsetPositivoFracionario_SubtraiCincoHorasEMeia()
    {
        var filtro = new ListarPedidosQuery { Inicio = new DateTimeOffset(2025, 3, 10, 0, 0, 0, new TimeSpan(5, 30, 0)) };

        Assert.Equal(new DateTime(2025, 3, 9, 18, 30, 0, DateTimeKind.Utc), Periodo.ParaUtc(filtro).Inicio);
    }

    [Fact]
    public void ParaUtc_LimitesComOffsetsDiferentes_ConverteCadaUmComOSeu()
    {
        // Transição de horário de verão (Nova York, 09/03/2025): o dia local tem 23 horas.
        var filtro = new ListarPedidosQuery
        {
            Inicio = new DateTimeOffset(2025, 3, 9, 0, 0, 0, TimeSpan.FromHours(-5)),
            Fim = new DateTimeOffset(2025, 3, 10, 0, 0, 0, TimeSpan.FromHours(-4)),
        };

        var intervalo = Periodo.ParaUtc(filtro);

        Assert.Equal(TimeSpan.FromHours(23), intervalo.FimExclusivo - intervalo.Inicio);
    }

    [Fact]
    public void ParaUtc_DiasEmUtcLegados_ComportamentoInalterado()
    {
        var filtro = new ListarPedidosQuery { DataInicio = new DateOnly(2025, 3, 10), DataFim = new DateOnly(2025, 3, 10) };

        var intervalo = Periodo.ParaUtc(filtro);

        Assert.Equal(Utc(2025, 3, 10), intervalo.Inicio);
        Assert.Equal(Utc(2025, 3, 11), intervalo.FimExclusivo);
    }

    [Fact]
    public void ParaUtc_SoUmLimite_OutroFicaAberto()
    {
        var intervalo = Periodo.ParaUtc(new ListarPedidosQuery { Fim = new DateTimeOffset(2025, 3, 11, 0, 0, 0, TimeSpan.Zero) });

        Assert.Null(intervalo.Inicio);
        Assert.Equal(Utc(2025, 3, 11), intervalo.FimExclusivo);
    }

    [Fact]
    public void ParaUtc_SemPeriodo_IntervaloAberto()
    {
        var intervalo = Periodo.ParaUtc(new ListarPedidosQuery());

        Assert.Null(intervalo.Inicio);
        Assert.Null(intervalo.FimExclusivo);
    }

    [Fact]
    public void ParaUtc_FormasMisturadas_LancaInvalidOperation()
    {
        // O validator barra antes (400); aqui é a defesa caso alguém chame sem validar.
        var filtro = new ListarPedidosQuery { DataInicio = new DateOnly(2025, 3, 10), Fim = DateTimeOffset.UnixEpoch };

        Assert.Throws<InvalidOperationException>(() => Periodo.ParaUtc(filtro));
    }
}
