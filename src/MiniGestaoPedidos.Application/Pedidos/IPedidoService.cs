using MiniGestaoPedidos.Application.Common;

namespace MiniGestaoPedidos.Application.Pedidos;

public interface IPedidoService
{
    Task<PedidoResponse> CriarAsync(CriarPedidoRequest request, CancellationToken cancellationToken);

    Task<PedidoResponse> ObterPorIdAsync(int id, CancellationToken cancellationToken);

    Task<PaginaResultado<PedidoResumoResponse>> ListarAsync(ListarPedidosQuery query, CancellationToken cancellationToken);
}
