using Projeto.Application.Pedidos;

namespace Projeto.Application.Tests.Pedidos;

public class CriarPedidoRequestValidatorTests
{
    private readonly CriarPedidoRequestValidator _validator = new();

    [Fact]
    public void Validate_RequestValida_SemErros()
    {
        var resultado = _validator.Validate(new CriarPedidoRequest(1, [new(1, 2), new(2, 1)]));

        Assert.True(resultado.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ClienteIdInvalido_Erro(int clienteId)
    {
        var resultado = _validator.Validate(new CriarPedidoRequest(clienteId, [new(1, 1)]));

        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(CriarPedidoRequest.ClienteId));
    }

    [Fact]
    public void Validate_ItensNulos_Erro()
    {
        var resultado = _validator.Validate(new CriarPedidoRequest(1, null));

        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(CriarPedidoRequest.Itens)); // RN02
    }

    [Fact]
    public void Validate_ItensVazios_Erro()
    {
        var resultado = _validator.Validate(new CriarPedidoRequest(1, []));

        Assert.Contains(resultado.Errors, e => e.ErrorMessage.Contains("pelo menos um item")); // RN02
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_QuantidadeNaoPositiva_Erro(int quantidade)
    {
        var resultado = _validator.Validate(new CriarPedidoRequest(1, [new(1, quantidade)]));

        Assert.Contains(resultado.Errors, e => e.PropertyName == "Itens[0].Quantidade"); // RN03
    }

    [Fact]
    public void Validate_ProdutoIdInvalido_Erro()
    {
        var resultado = _validator.Validate(new CriarPedidoRequest(1, [new(0, 1)]));

        Assert.Contains(resultado.Errors, e => e.PropertyName == "Itens[0].ProdutoId");
    }

    [Fact]
    public void Validate_ProdutoRepetido_Erro()
    {
        var resultado = _validator.Validate(new CriarPedidoRequest(1, [new(3, 1), new(3, 2)]));

        Assert.Contains(resultado.Errors, e => e.ErrorMessage.Contains("mesmo produto")); // RN05
    }
}
