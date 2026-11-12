using Projeto.Domain.Entities;

namespace Projeto.Application.Abstractions;

public interface IProdutoRepository
{
    /// <summary>Retorna os produtos encontrados (rastreados, para baixa de estoque). Ids inexistentes são omitidos.</summary>
    Task<IReadOnlyList<Produto>> ObterPorIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken);
}
