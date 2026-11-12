using Microsoft.EntityFrameworkCore;
using Projeto.Application.Common;
using Projeto.Application.Pedidos;

namespace Projeto.Infrastructure.Persistence.Queries;

internal sealed class PedidoQueries(AppDbContext context) : IPedidoQueries
{
    public async Task<PaginaResultado<PedidoResumoResponse>> ListarAsync(
        ListarPedidosQuery query, CancellationToken cancellationToken)
    {
        var pedidos = context.Pedidos.AsNoTracking();

        if (query.ClienteId is { } clienteId)
        {
            pedidos = pedidos.Where(p => p.ClienteId == clienteId);
        }

        // Período já convertido para UTC pela Application (RN14). Intervalo semiaberto,
        // sem função sobre a coluna → index seek (CA11, CA18, CA19).
        var intervalo = Periodo.ParaUtc(query);

        if (intervalo.Inicio is { } inicio)
        {
            pedidos = pedidos.Where(p => p.DataPedido >= inicio);
        }

        if (intervalo.FimExclusivo is { } fimExclusivo)
        {
            pedidos = pedidos.Where(p => p.DataPedido < fimExclusivo);
        }

        var total = await pedidos.CountAsync(cancellationToken);

        var itens = await pedidos
            .OrderByDescending(p => p.DataPedido)
            .ThenByDescending(p => p.Id)
            .Skip((query.Pagina - 1) * query.TamanhoPagina)
            .Take(query.TamanhoPagina)
            .Select(p => new PedidoResumoResponse(
                p.Id,
                p.ClienteId,
                p.Cliente.Nome,
                p.DataPedido,
                p.ValorTotal,
                p.Itens.Count))
            .ToListAsync(cancellationToken);

        return new PaginaResultado<PedidoResumoResponse>(itens, query.Pagina, query.TamanhoPagina, total);
    }

    public async Task<PedidoResponse?> ObterPorIdAsync(int id, CancellationToken cancellationToken) =>
        await context.Pedidos
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PedidoResponse(
                p.Id,
                p.ClienteId,
                p.Cliente.Nome,
                p.DataPedido,
                p.ValorTotal,
                p.Itens
                    .OrderBy(i => i.Id)
                    .Select(i => new PedidoItemResponse(
                        i.ProdutoId,
                        i.Produto.Nome,
                        i.Quantidade,
                        i.PrecoUnitario,
                        i.Quantidade * i.PrecoUnitario))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);
}
