namespace MiniGestaoPedidos.Application.Abstractions;

public interface IUnitOfWork
{
    /// <summary>
    /// Persiste todas as alterações de forma atômica (RN10).
    /// Lança <see cref="Common.Exceptions.ConflitoConcorrenciaException"/> em conflito de concorrência.
    /// </summary>
    Task SalvarAlteracoesAsync(CancellationToken cancellationToken);
}
