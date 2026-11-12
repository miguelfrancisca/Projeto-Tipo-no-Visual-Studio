using Projeto.Domain.Entities;
using Projeto.Domain.Exceptions;
using Projeto.Domain.Tests.Builders;

namespace Projeto.Domain.Tests.Entities;

public class PedidoTests
{
    private static readonly DateTime Agora = new(2026, 11, 12, 14, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Criar_ItensValidos_CalculaValorTotalEMantemItens()
    {
        var cliente = Construtores.Cliente(id: 5);
        var teclado = Construtores.Produto(id: 1, preco: 149.90m);
        var mouse = Construtores.Produto(id: 2, preco: 59.90m);

        var pedido = Pedido.Criar(cliente, Agora, [new ItemSolicitado(teclado, 2), new ItemSolicitado(mouse, 1)]);

        Assert.Equal(5, pedido.ClienteId);
        Assert.Equal(Agora, pedido.DataPedido);
        Assert.Equal(2, pedido.Itens.Count);
        Assert.Equal(359.70m, pedido.ValorTotal); // RN09: 2 × 149,90 + 1 × 59,90
    }

    [Fact]
    public void Criar_ItensValidos_CongelaPrecoAtualDoProduto()
    {
        var produto = Construtores.Produto(id: 1, preco: 100m);

        var pedido = Pedido.Criar(Construtores.Cliente(), Agora, [new ItemSolicitado(produto, 1)]);

        Assert.Equal(100m, pedido.Itens.Single().PrecoUnitario); // RN07
    }

    [Fact]
    public void Criar_ItensValidos_BaixaEstoqueDeCadaProduto()
    {
        var a = Construtores.Produto(id: 1, estoque: 10);
        var b = Construtores.Produto(id: 2, estoque: 3);

        Pedido.Criar(Construtores.Cliente(), Agora, [new ItemSolicitado(a, 4), new ItemSolicitado(b, 3)]);

        Assert.Equal(6, a.Estoque);
        Assert.Equal(0, b.Estoque);
    }

    [Fact]
    public void Criar_SemItens_LancaDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => Pedido.Criar(Construtores.Cliente(), Agora, []));

        Assert.Contains("pelo menos um item", ex.Message); // RN02
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Criar_QuantidadeNaoPositiva_LancaDomainException(int quantidade)
    {
        var produto = Construtores.Produto(estoque: 10);

        Assert.Throws<DomainException>(
            () => Pedido.Criar(Construtores.Cliente(), Agora, [new ItemSolicitado(produto, quantidade)])); // RN03
        Assert.Equal(10, produto.Estoque);
    }

    [Fact]
    public void Criar_ProdutoRepetido_LancaDomainException()
    {
        var produto = Construtores.Produto(id: 9);

        var ex = Assert.Throws<DomainException>(() => Pedido.Criar(
            Construtores.Cliente(), Agora, [new ItemSolicitado(produto, 1), new ItemSolicitado(produto, 2)]));

        Assert.Contains("repetido", ex.Message); // RN05
    }

    [Fact]
    public void Criar_UmItemSemEstoque_NaoBaixaEstoqueDeNenhumProduto()
    {
        // CA06: o primeiro item é válido, o segundo não — nenhum estoque pode mudar.
        var comEstoque = Construtores.Produto(id: 1, estoque: 10);
        var semEstoque = Construtores.Produto(id: 2, estoque: 1);

        Assert.Throws<EstoqueInsuficienteException>(() => Pedido.Criar(
            Construtores.Cliente(), Agora, [new ItemSolicitado(comEstoque, 5), new ItemSolicitado(semEstoque, 2)]));

        Assert.Equal(10, comEstoque.Estoque);
        Assert.Equal(1, semEstoque.Estoque);
    }

    [Fact]
    public void Subtotal_MultiplicaQuantidadePeloPreco()
    {
        var pedido = Pedido.Criar(
            Construtores.Cliente(), Agora, [new ItemSolicitado(Construtores.Produto(preco: 12.50m), 3)]);

        Assert.Equal(37.50m, pedido.Itens.Single().Subtotal);
    }
}
