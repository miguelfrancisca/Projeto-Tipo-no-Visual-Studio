using Microsoft.EntityFrameworkCore;
using MiniGestaoPedidos.Application.Abstractions;
using MiniGestaoPedidos.Domain.Entities;

namespace MiniGestaoPedidos.Infrastructure.Persistence.Repositories;

internal sealed class ProdutoRepository(AppDbContext context) : IProdutoRepository
{
    public async Task<IReadOnlyList<Produto>> ObterPorIdsAsync(
        IReadOnlyCollection<int> ids, CancellationToken cancellationToken) =>
        await context.Produtos
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(cancellationToken); // rastreados: a baixa de estoque é salva no SaveChanges
}
