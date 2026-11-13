namespace MiniGestaoPedidos.Domain.Exceptions;

/// <summary>RN06 — a quantidade solicitada excede o estoque disponível.</summary>
public sealed class EstoqueInsuficienteException : DomainException
{
    public EstoqueInsuficienteException(int produtoId, string produtoNome, int disponivel, int solicitado)
        : base($"Estoque insuficiente para o produto '{produtoNome}' (Id {produtoId}): disponível {disponivel}, solicitado {solicitado}.")
    {
        ProdutoId = produtoId;
        Disponivel = disponivel;
        Solicitado = solicitado;
    }

    public int ProdutoId { get; }

    public int Disponivel { get; }

    public int Solicitado { get; }
}
