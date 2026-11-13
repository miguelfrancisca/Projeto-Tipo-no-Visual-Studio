namespace MiniGestaoPedidos.Application.Catalogo;

public sealed record ProdutoResponse(int Id, string Nome, string Sku, decimal Preco, int Estoque);
