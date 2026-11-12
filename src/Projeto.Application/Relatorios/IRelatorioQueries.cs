namespace Projeto.Application.Relatorios;

public interface IRelatorioQueries
{
    /// <summary>
    /// Clientes com maior valor total de pedidos em [inicio, fimExclusivo),
    /// ordenados por valor desc e ClienteId asc (desempate determinístico).
    /// </summary>
    Task<IReadOnlyList<TopClienteResponse>> TopClientesAsync(
        DateTime inicio, DateTime fimExclusivo, int limite, CancellationToken cancellationToken);
}
