namespace Projeto.Domain.Entities;

public class ItemPedido
{
    internal ItemPedido(Produto produto, int quantidade)
    {
        Produto = produto;
        ProdutoId = produto.Id;
        Quantidade = quantidade;
        PrecoUnitario = produto.Preco; // RN07: preço congelado no momento da compra
    }

    // Usado pelo EF Core.
    protected ItemPedido()
    {
    }

    public int Id { get; private set; }

    public int PedidoId { get; private set; }

    public int ProdutoId { get; private set; }

    public Produto Produto { get; private set; } = null!;

    public int Quantidade { get; private set; }

    public decimal PrecoUnitario { get; private set; }

    public decimal Subtotal => Quantidade * PrecoUnitario;
}
