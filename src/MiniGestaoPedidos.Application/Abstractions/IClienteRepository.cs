using MiniGestaoPedidos.Domain.Entities;

namespace MiniGestaoPedidos.Application.Abstractions;

public interface IClienteRepository
{
    Task<Cliente?> ObterPorIdAsync(int id, CancellationToken cancellationToken);
}
