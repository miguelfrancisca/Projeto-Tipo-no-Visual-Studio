using Microsoft.EntityFrameworkCore;
using Projeto.Application.Catalogo;

namespace Projeto.Infrastructure.Persistence.Queries;

internal sealed class CatalogoQueries(AppDbContext context) : ICatalogoQueries
{
    public async Task<IReadOnlyList<ClienteResponse>> ListarClientesAsync(CancellationToken cancellationToken) =>
        await context.Clientes
            .AsNoTracking()
            .OrderBy(c => c.Nome)
            .ThenBy(c => c.Id)
            .Select(c => new ClienteResponse(c.Id, c.Nome, c.Email))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProdutoResponse>> ListarProdutosAsync(CancellationToken cancellationToken) =>
        await context.Produtos
            .AsNoTracking()
            .OrderBy(p => p.Nome)
            .Select(p => new ProdutoResponse(p.Id, p.Nome, p.Sku, p.Preco, p.Estoque))
            .ToListAsync(cancellationToken);
}
