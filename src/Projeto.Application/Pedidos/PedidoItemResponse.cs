namespace Projeto.Application.Pedidos;

public sealed record PedidoItemResponse(
    int ProdutoId,
    string ProdutoNome,
    int Quantidade,
    decimal PrecoUnitario,
    decimal Subtotal);
