namespace MiniGestaoPedidos.Domain.Exceptions;

/// <summary>
/// Violação de uma invariante do domínio. A API traduz para HTTP 422.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }
}
