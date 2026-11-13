using MiniGestaoPedidos.Domain.Entities;
using MiniGestaoPedidos.Domain.Exceptions;
using MiniGestaoPedidos.Domain.Tests.Builders;

namespace MiniGestaoPedidos.Domain.Tests.Entities;

public class ProdutoTests
{
    [Fact]
    public void BaixarEstoque_QuantidadeDisponivel_DiminuiEstoque()
    {
        var produto = Construtores.Produto(estoque: 10);

        produto.BaixarEstoque(3);

        Assert.Equal(7, produto.Estoque);
    }

    [Fact]
    public void BaixarEstoque_QuantidadeIgualAoEstoque_ZeraEstoque()
    {
        var produto = Construtores.Produto(estoque: 5);

        produto.BaixarEstoque(5);

        Assert.Equal(0, produto.Estoque);
    }

    [Fact]
    public void BaixarEstoque_QuantidadeMaiorQueEstoque_LancaEstoqueInsuficienteENaoAltera()
    {
        var produto = Construtores.Produto(id: 7, estoque: 2, nome: "Teclado");

        var ex = Assert.Throws<EstoqueInsuficienteException>(() => produto.BaixarEstoque(3));

        Assert.Equal(7, ex.ProdutoId);
        Assert.Equal(2, ex.Disponivel);
        Assert.Equal(3, ex.Solicitado);
        Assert.Contains("Teclado", ex.Message);
        Assert.Equal(2, produto.Estoque);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void BaixarEstoque_QuantidadeNaoPositiva_LancaDomainException(int quantidade)
    {
        var produto = Construtores.Produto(estoque: 10);

        Assert.Throws<DomainException>(() => produto.BaixarEstoque(quantidade));
        Assert.Equal(10, produto.Estoque);
    }

    [Fact]
    public void TemEstoque_ConsideraQuantidadeSolicitada()
    {
        var produto = Construtores.Produto(estoque: 4);

        Assert.True(produto.TemEstoque(4));
        Assert.False(produto.TemEstoque(5));
    }

    [Theory]
    [InlineData(-0.01, 1)]
    [InlineData(10, -1)]
    public void Construtor_PrecoOuEstoqueNegativo_LancaDomainException(decimal preco, int estoque)
    {
        Assert.Throws<DomainException>(() => new Produto("Mouse", "MOU-001", preco, estoque));
    }
}
