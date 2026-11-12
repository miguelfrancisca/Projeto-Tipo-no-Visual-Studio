namespace Projeto.Application.Common.Exceptions;

/// <summary>
/// Request bem formada, mas que viola uma regra que depende de dados
/// (ex.: cliente ou produto inexistente). A API traduz para HTTP 422.
/// </summary>
public sealed class RegraNegocioException : Exception
{
    public RegraNegocioException(string message)
        : base(message)
    {
    }
}
