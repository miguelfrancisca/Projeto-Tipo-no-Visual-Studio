namespace Projeto.Application.Common.Exceptions;

/// <summary>O recurso identificado na URL não existe. A API traduz para HTTP 404.</summary>
public sealed class NaoEncontradoException : Exception
{
    public NaoEncontradoException(string message)
        : base(message)
    {
    }
}
