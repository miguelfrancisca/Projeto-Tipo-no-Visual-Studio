namespace MiniGestaoPedidos.Domain.Entities;

/// <summary>Um produto e a quantidade desejada, antes de virar <see cref="ItemPedido"/>.</summary>
public sealed record ItemSolicitado(Produto Produto, int Quantidade);
