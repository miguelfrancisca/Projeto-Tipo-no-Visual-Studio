using FluentValidation;
using MiniGestaoPedidos.Application.Common;

namespace MiniGestaoPedidos.Application.Relatorios;

public sealed class RelatorioService(
    IRelatorioQueries relatorioQueries,
    IValidator<TopClientesQuery> validator) : IRelatorioService
{
    public async Task<IReadOnlyList<TopClienteResponse>> TopClientesAsync(
        TopClientesQuery query, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(query, cancellationToken);

        // Validado: o período está completo, numa única forma. Conversão para UTC antes da consulta.
        var intervalo = Periodo.ParaUtc(query);

        return await relatorioQueries.TopClientesAsync(
            intervalo.Inicio!.Value,
            intervalo.FimExclusivo!.Value,
            TopClientesQuery.Limite,
            cancellationToken);
    }
}
