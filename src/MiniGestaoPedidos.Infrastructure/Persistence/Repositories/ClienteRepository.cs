using MiniGestaoPedidos.Application.Abstractions;
using MiniGestaoPedidos.Domain.Entities;

namespace MiniGestaoPedidos.Infrastructure.Persistence.Repositories;

internal sealed class ClienteRepository(AppDbContext context) : IClienteRepository
{
    public async Task<Cliente?> ObterPorIdAsync(int id, CancellationToken cancellationToken) =>
        await context.Clientes.FindAsync([id], cancellationToken);
}
