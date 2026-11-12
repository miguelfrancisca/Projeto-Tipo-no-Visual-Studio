using Projeto.Domain.Entities;

namespace Projeto.Application.Pedidos;

public sealed record PedidoResponse(
    int Id,
    int ClienteId,
    string ClienteNome,
    DateTime DataPedido,
    decimal ValorTotal,
    IReadOnlyList<PedidoItemResponse> Itens)
{
    public static PedidoResponse De(Pedido pedido) => new(
        pedido.Id,
        pedido.ClienteId,
        pedido.Cliente.Nome,
        pedido.DataPedido,
        pedido.ValorTotal,
        pedido.Itens
            .Select(i => new PedidoItemResponse(i.ProdutoId, i.Produto.Nome, i.Quantidade, i.PrecoUnitario, i.Subtotal))
            .ToList());
}
