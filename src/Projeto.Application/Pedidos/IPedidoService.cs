using Projeto.Application.Common;

namespace Projeto.Application.Pedidos;

public interface IPedidoService
{
    Task<PedidoResponse> CriarAsync(CriarPedidoRequest request, CancellationToken cancellationToken);

    Task<PedidoResponse> ObterPorIdAsync(int id, CancellationToken cancellationToken);

    Task<PaginaResultado<PedidoResumoResponse>> ListarAsync(ListarPedidosQuery query, CancellationToken cancellationToken);
}
