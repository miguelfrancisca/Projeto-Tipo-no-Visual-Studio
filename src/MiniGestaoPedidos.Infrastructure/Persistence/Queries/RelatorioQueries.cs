using Microsoft.EntityFrameworkCore;
using MiniGestaoPedidos.Application.Relatorios;

namespace MiniGestaoPedidos.Infrastructure.Persistence.Queries;

internal sealed class RelatorioQueries(AppDbContext context) : IRelatorioQueries
{
    public async Task<IReadOnlyList<TopClienteResponse>> TopClientesAsync(
        DateTime inicio, DateTime fimExclusivo, int limite, CancellationToken cancellationToken)
    {
        // Agrega por ClienteId (nunca por nome: homônimos são clientes diferentes) usando
        // Pedidos.ValorTotal — coberto por IX_Pedidos_DataPedido INCLUDE (ClienteId, ValorTotal),
        // sem ler ItensPedido. Clientes sem pedidos no período não aparecem (RN13).
        var linhas = await context.Pedidos
            .AsNoTracking()
            .Where(p => p.DataPedido >= inicio && p.DataPedido < fimExclusivo)
            .GroupBy(p => p.ClienteId)
            .Select(g => new
            {
                ClienteId = g.Key,
                ValorTotal = g.Sum(p => p.ValorTotal),
                QuantidadePedidos = g.Count(),
            })
            .OrderByDescending(t => t.ValorTotal)
            .ThenBy(t => t.ClienteId)
            .Take(limite)
            .Join(
                context.Clientes,
                t => t.ClienteId,
                c => c.Id,
                (t, c) => new { t.ClienteId, c.Nome, t.ValorTotal, t.QuantidadePedidos })
            .ToListAsync(cancellationToken);

        return linhas
            .OrderByDescending(l => l.ValorTotal)
            .ThenBy(l => l.ClienteId)
            .Select(l => new TopClienteResponse(l.ClienteId, l.Nome, l.ValorTotal, l.QuantidadePedidos))
            .ToList();
    }
}
