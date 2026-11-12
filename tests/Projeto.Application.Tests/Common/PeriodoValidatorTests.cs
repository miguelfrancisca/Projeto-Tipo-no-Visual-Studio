using Projeto.Application.Pedidos;
using Projeto.Application.Relatorios;

namespace Projeto.Application.Tests.Common;

/// <summary>RN11 + RN14 — regras de período comuns à listagem e ao relatório.</summary>
public class PeriodoValidatorTests
{
    private static readonly DateTimeOffset InicioSp = new(2025, 3, 10, 0, 0, 0, TimeSpan.FromHours(-3));
    private static readonly DateTimeOffset FimSp = new(2025, 3, 11, 0, 0, 0, TimeSpan.FromHours(-3));

    private readonly ListarPedidosQueryValidator _listar = new();
    private readonly TopClientesQueryValidator _topClientes = new();

    [Fact]
    public void Listar_InstantesValidos_SemErros()
    {
        Assert.True(_listar.Validate(new ListarPedidosQuery { Inicio = InicioSp, Fim = FimSp }).IsValid);
    }

    [Fact]
    public void Listar_FimIgualAoInicio_Erro()
    {
        // O fim é exclusivo: [t, t) é um intervalo vazio, não "o instante t".
        var resultado = _listar.Validate(new ListarPedidosQuery { Inicio = InicioSp, Fim = InicioSp });

        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(ListarPedidosQuery.Fim));
    }

    [Fact]
    public void Listar_FimAntesDoInicio_ComparaInstantesENaoHorarioLocal()
    {
        // 10/03 01:00-03:00 (= 04:00Z) é DEPOIS de 10/03 03:00+00:00 (= 03:00Z), apesar do "01:00" < "03:00".
        var query = new ListarPedidosQuery
        {
            Inicio = new DateTimeOffset(2025, 3, 10, 3, 0, 0, TimeSpan.Zero),
            Fim = new DateTimeOffset(2025, 3, 10, 1, 0, 0, TimeSpan.FromHours(-3)),
        };

        Assert.True(_listar.Validate(query).IsValid);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Listar_MisturaFormaAntigaENova_Erro(bool antigaNoInicio, bool antigaNoFim)
    {
        var query = new ListarPedidosQuery
        {
            DataInicio = antigaNoInicio ? new DateOnly(2025, 3, 10) : null,
            DataFim = antigaNoFim ? new DateOnly(2025, 3, 10) : null,
            Inicio = antigaNoInicio ? null : InicioSp,
            Fim = antigaNoFim ? null : FimSp,
        };

        var resultado = _listar.Validate(query);

        Assert.Contains(resultado.Errors, e => e.PropertyName == "periodo");
    }

    [Fact]
    public void Listar_SoInicio_Valido()
    {
        Assert.True(_listar.Validate(new ListarPedidosQuery { Inicio = InicioSp }).IsValid);
    }

    [Fact]
    public void TopClientes_InstantesCompletos_Valido()
    {
        Assert.True(_topClientes.Validate(new TopClientesQuery { Inicio = InicioSp, Fim = FimSp }).IsValid);
    }

    [Fact]
    public void TopClientes_SoInicio_ExigeFim()
    {
        var resultado = _topClientes.Validate(new TopClientesQuery { Inicio = InicioSp });

        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(TopClientesQuery.Fim));
        Assert.DoesNotContain(resultado.Errors, e => e.PropertyName == nameof(TopClientesQuery.DataInicio));
    }

    [Fact]
    public void TopClientes_Misturado_Erro()
    {
        var query = new TopClientesQuery { DataInicio = new DateOnly(2025, 3, 10), Fim = FimSp };

        Assert.Contains(_topClientes.Validate(query).Errors, e => e.PropertyName == "periodo");
    }
}
