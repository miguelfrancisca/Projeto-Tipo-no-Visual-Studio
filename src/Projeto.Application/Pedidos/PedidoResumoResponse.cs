namespace Projeto.Application.Pedidos;

/// <summary>Linha da listagem de pedidos (GET /pedidos).</summary>
public sealed record PedidoResumoResponse(
    int Id,
    int ClienteId,
    string ClienteNome,
    DateTime DataPedido,
    decimal ValorTotal,
    int QuantidadeItens);
