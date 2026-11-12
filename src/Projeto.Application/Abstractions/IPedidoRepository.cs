using Projeto.Domain.Entities;

namespace Projeto.Application.Abstractions;

public interface IPedidoRepository
{
    void Adicionar(Pedido pedido);
}
