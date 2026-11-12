namespace Projeto.Application.Catalogo;

/// <summary>
/// Listas de apoio ao frontend (GET /clientes, GET /produtos).
/// Leitura pura, sem regra nem filtro: os controllers usam esta interface diretamente,
/// sem um service intermediário que só repassaria a chamada.
/// </summary>
public interface ICatalogoQueries
{
    Task<IReadOnlyList<ClienteResponse>> ListarClientesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ProdutoResponse>> ListarProdutosAsync(CancellationToken cancellationToken);
}
