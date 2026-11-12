using Projeto.Application.Pedidos;

namespace Projeto.Application.Tests.Pedidos;

public class ListarPedidosQueryValidatorTests
{
    private readonly ListarPedidosQueryValidator _validator = new();

    [Fact]
    public void Validate_SemFiltros_ValidoComPadroes()
    {
        var query = new ListarPedidosQuery();

        var resultado = _validator.Validate(query);

        Assert.True(resultado.IsValid);
        Assert.Equal(1, query.Pagina);
        Assert.Equal(10, query.TamanhoPagina);
    }

    [Fact]
    public void Validate_MesmaDataInicioEFim_Valido()
    {
        var dia = new DateOnly(2026, 3, 15);

        var resultado = _validator.Validate(new ListarPedidosQuery { DataInicio = dia, DataFim = dia });

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void Validate_DataInicioPosteriorADataFim_Erro()
    {
        var query = new ListarPedidosQuery { DataInicio = new DateOnly(2026, 5, 2), DataFim = new DateOnly(2026, 5, 1) };

        var resultado = _validator.Validate(query);

        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(ListarPedidosQuery.DataFim)); // RN11
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_PaginaMenorQueUm_Erro(int pagina)
    {
        var resultado = _validator.Validate(new ListarPedidosQuery { Pagina = pagina });

        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(ListarPedidosQuery.Pagina)); // RN12
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_TamanhoPaginaForaDoLimite_Erro(int tamanho)
    {
        var resultado = _validator.Validate(new ListarPedidosQuery { TamanhoPagina = tamanho });

        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(ListarPedidosQuery.TamanhoPagina)); // RN12
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Validate_TamanhoPaginaNosLimites_Valido(int tamanho)
    {
        Assert.True(_validator.Validate(new ListarPedidosQuery { TamanhoPagina = tamanho }).IsValid);
    }
}
