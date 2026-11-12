using Microsoft.EntityFrameworkCore;
using Projeto.Application.Abstractions;
using Projeto.Domain.Entities;

namespace Projeto.Infrastructure.Persistence.Repositories;

internal sealed class ProdutoRepository(AppDbContext context) : IProdutoRepository
{
    public async Task<IReadOnlyList<Produto>> ObterPorIdsAsync(
        IReadOnlyCollection<int> ids, CancellationToken cancellationToken) =>
        await context.Produtos
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(cancellationToken); // rastreados: a baixa de estoque é salva no SaveChanges
}
