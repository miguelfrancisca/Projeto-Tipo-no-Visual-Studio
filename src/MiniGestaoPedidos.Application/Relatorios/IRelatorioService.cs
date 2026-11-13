namespace MiniGestaoPedidos.Application.Relatorios;

public interface IRelatorioService
{
    Task<IReadOnlyList<TopClienteResponse>> TopClientesAsync(TopClientesQuery query, CancellationToken cancellationToken);
}
