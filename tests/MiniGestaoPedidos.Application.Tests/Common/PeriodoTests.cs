using MiniGestaoPedidos.Application.Common;

namespace MiniGestaoPedidos.Application.Tests.Common;

public class PeriodoTests
{
    [Fact]
    public void FimExclusivo_RetornaMeiaNoiteDoDiaSeguinte()
    {
        // CA11: um pedido às 23:59 do dia final precisa entrar no filtro.
        var fim = Periodo.FimExclusivo(new DateOnly(2026, 12, 31));

        Assert.Equal(new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc), fim);
    }

    [Fact]
    public void Inicio_RetornaMeiaNoiteDoProprioDia()
    {
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Periodo.Inicio(new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public void Inicio_E_FimExclusivo_DataNula_RetornamNulo()
    {
        Assert.Null(Periodo.Inicio(null));
        Assert.Null(Periodo.FimExclusivo(null));
    }

    [Fact]
    public void TotalPaginas_ArredondaParaCima()
    {
        Assert.Equal(3, new PaginaResultado<int>([], 1, 10, 21).TotalPaginas);
        Assert.Equal(0, new PaginaResultado<int>([], 1, 10, 0).TotalPaginas);
    }
}
