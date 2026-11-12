namespace Projeto.Application.Common.Exceptions;

/// <summary>
/// RN10 — outro pedido alterou o estoque do mesmo produto ao mesmo tempo.
/// Lançada pela Infrastructure ao detectar conflito de rowversion. A API traduz para HTTP 409.
/// </summary>
public sealed class ConflitoConcorrenciaException : Exception
{
    public ConflitoConcorrenciaException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
