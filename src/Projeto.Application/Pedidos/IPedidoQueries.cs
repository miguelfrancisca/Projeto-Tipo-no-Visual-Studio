using Projeto.Application.Common;

namespace Projeto.Application.Pedidos;

/// <summary>Consultas de leitura de pedidos (projeção direta para DTO).</summary>
public interface IPedidoQueries
{
    Task<PaginaResultado<PedidoResumoResponse>> ListarAsync(ListarPedidosQuery query, CancellationToken cancellationToken);

    Task<PedidoResponse?> ObterPorIdAsync(int id, CancellationToken cancellationToken);
}
