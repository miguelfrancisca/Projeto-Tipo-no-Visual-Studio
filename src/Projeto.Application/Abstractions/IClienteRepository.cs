using Projeto.Domain.Entities;

namespace Projeto.Application.Abstractions;

public interface IClienteRepository
{
    Task<Cliente?> ObterPorIdAsync(int id, CancellationToken cancellationToken);
}
