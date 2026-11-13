using System.Reflection;
using MiniGestaoPedidos.Domain.Entities;

namespace MiniGestaoPedidos.Application.Tests.Builders;

/// <summary>Entidades "como se tivessem vindo do banco" (Id definido via reflexão).</summary>
internal static class Construtores
{
    public static Cliente Cliente(int id = 1, string nome = "Ana Souza") =>
        ComId(new Cliente(nome, $"cliente{id}@exemplo.com"), id);

    public static Produto Produto(int id = 1, decimal preco = 10m, int estoque = 100, string? nome = null) =>
        ComId(new Produto(nome ?? $"Produto {id}", $"SKU-{id:000}", preco, estoque), id);

    private static T ComId<T>(T entidade, int id)
        where T : class
    {
        typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)!.SetValue(entidade, id);
        return entidade;
    }
}
