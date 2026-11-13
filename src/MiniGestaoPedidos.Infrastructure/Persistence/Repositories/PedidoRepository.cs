using MiniGestaoPedidos.Application.Abstractions;
using MiniGestaoPedidos.Domain.Entities;

namespace MiniGestaoPedidos.Infrastructure.Persistence.Repositories;

internal sealed class PedidoRepository(AppDbContext context) : IPedidoRepository
{
    public void Adicionar(Pedido pedido) => context.Pedidos.Add(pedido);
}
