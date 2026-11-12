using Projeto.Application.Abstractions;
using Projeto.Domain.Entities;

namespace Projeto.Infrastructure.Persistence.Repositories;

internal sealed class ClienteRepository(AppDbContext context) : IClienteRepository
{
    public async Task<Cliente?> ObterPorIdAsync(int id, CancellationToken cancellationToken) =>
        await context.Clientes.FindAsync([id], cancellationToken);
}
