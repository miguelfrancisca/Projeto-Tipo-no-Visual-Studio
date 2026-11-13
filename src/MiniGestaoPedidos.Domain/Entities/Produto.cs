using MiniGestaoPedidos.Domain.Exceptions;

namespace MiniGestaoPedidos.Domain.Entities;

public class Produto
{
    public Produto(string nome, string sku, decimal preco, int estoque)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DomainException("O nome do produto é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new DomainException("O SKU do produto é obrigatório.");
        }

        if (preco < 0)
        {
            throw new DomainException("O preço do produto não pode ser negativo.");
        }

        if (estoque < 0)
        {
            throw new DomainException("O estoque do produto não pode ser negativo.");
        }

        Nome = nome.Trim();
        Sku = sku.Trim();
        Preco = preco;
        Estoque = estoque;
    }

    // Usado pelo EF Core.
    protected Produto()
    {
    }

    public int Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public string Sku { get; private set; } = string.Empty;

    public decimal Preco { get; private set; }

    public int Estoque { get; private set; }

    /// <summary>Token de concorrência otimista (rowversion) — RN10.</summary>
    public byte[] RowVersion { get; private set; } = [];

    public bool TemEstoque(int quantidade) => quantidade <= Estoque;

    /// <summary>RN06 — baixa o estoque ou falha sem alterar nada.</summary>
    public void BaixarEstoque(int quantidade)
    {
        if (quantidade <= 0)
        {
            throw new DomainException("A quantidade a baixar do estoque deve ser maior que zero.");
        }

        if (!TemEstoque(quantidade))
        {
            throw new EstoqueInsuficienteException(Id, Nome, Estoque, quantidade);
        }

        Estoque -= quantidade;
    }
}
