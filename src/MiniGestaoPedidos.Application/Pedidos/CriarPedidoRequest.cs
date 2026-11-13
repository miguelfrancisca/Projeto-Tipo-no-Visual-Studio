namespace MiniGestaoPedidos.Application.Pedidos;

/// <summary>Corpo de POST /pedidos.</summary>
public sealed record CriarPedidoRequest(int ClienteId, IReadOnlyList<CriarPedidoItemRequest>? Itens);
