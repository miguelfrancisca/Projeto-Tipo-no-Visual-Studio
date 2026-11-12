using FluentValidation;
using Projeto.Application.Common;

namespace Projeto.Application.Relatorios;

public sealed class RelatorioService(
    IRelatorioQueries relatorioQueries,
    IValidator<TopClientesQuery> validator) : IRelatorioService
{
    public async Task<IReadOnlyList<TopClienteResponse>> TopClientesAsync(
        TopClientesQuery query, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(query, cancellationToken);

        return await relatorioQueries.TopClientesAsync(
            Periodo.Inicio(query.DataInicio)!.Value,
            Periodo.FimExclusivo(query.DataFim)!.Value,
            TopClientesQuery.Limite,
            cancellationToken);
    }
}
