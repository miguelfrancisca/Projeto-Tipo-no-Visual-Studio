using Projeto.Application.Abstractions;
using Projeto.Domain.Entities;

namespace Projeto.Infrastructure.Persistence.Repositories;

internal sealed class PedidoRepository(AppDbContext context) : IPedidoRepository
{
    public void Adicionar(Pedido pedido) => context.Pedidos.Add(pedido);
}
