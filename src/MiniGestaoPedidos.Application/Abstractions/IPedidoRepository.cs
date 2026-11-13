using MiniGestaoPedidos.Domain.Entities;

namespace MiniGestaoPedidos.Application.Abstractions;

public interface IPedidoRepository
{
    void Adicionar(Pedido pedido);
}
