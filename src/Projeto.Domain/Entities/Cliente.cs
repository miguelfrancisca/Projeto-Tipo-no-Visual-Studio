using Projeto.Domain.Exceptions;

namespace Projeto.Domain.Entities;

public class Cliente
{
    public Cliente(string nome, string email)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new DomainException("O nome do cliente é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("O e-mail do cliente é obrigatório.");
        }

        Nome = nome.Trim();
        Email = email.Trim();
        DataCadastro = DateTime.UtcNow;
    }

    // Usado pelo EF Core.
    protected Cliente()
    {
    }

    public int Id { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public DateTime DataCadastro { get; private set; }
}
