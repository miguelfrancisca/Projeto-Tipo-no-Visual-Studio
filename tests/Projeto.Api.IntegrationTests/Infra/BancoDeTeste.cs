using Microsoft.Data.SqlClient;

namespace Projeto.Api.IntegrationTests.Infra;

/// <summary>
/// Arrange e Assert direto no banco, com SQL puro — independente do EF e da API,
/// para que os testes verifiquem o estado real persistido.
/// </summary>
public sealed class BancoDeTeste(string connectionString)
{
    private int _sequencial;

    public Task<int> InserirClienteAsync(string nome = "Cliente Teste") =>
        EscalarAsync<int>(
            "INSERT INTO Clientes (Nome, Email) OUTPUT INSERTED.Id VALUES (@nome, @email)",
            ("@nome", nome),
            ("@email", $"cliente{Interlocked.Increment(ref _sequencial)}@teste.com"));

    public Task<int> InserirProdutoAsync(decimal preco = 10m, int estoque = 100, string? nome = null)
    {
        var n = Interlocked.Increment(ref _sequencial);
        return EscalarAsync<int>(
            "INSERT INTO Produtos (Nome, Sku, Preco, Estoque) OUTPUT INSERTED.Id VALUES (@nome, @sku, @preco, @estoque)",
            ("@nome", nome ?? $"Produto {n}"),
            ("@sku", $"SKU-{n:0000}"),
            ("@preco", preco),
            ("@estoque", estoque));
    }

    /// <summary>Insere um pedido "histórico" (sem itens) com data e valor controlados — para listagem e relatório.</summary>
    public Task<int> InserirPedidoAsync(int clienteId, DateTime data, decimal valorTotal = 100m) =>
        EscalarAsync<int>(
            "INSERT INTO Pedidos (ClienteId, DataPedido, ValorTotal) OUTPUT INSERTED.Id VALUES (@cliente, @data, @valor)",
            ("@cliente", clienteId),
            ("@data", data),
            ("@valor", valorTotal));

    public Task<int> EstoqueAsync(int produtoId) =>
        EscalarAsync<int>("SELECT Estoque FROM Produtos WHERE Id = @id", ("@id", produtoId));

    public Task<int> ContarAsync(string tabela) =>
        EscalarAsync<int>($"SELECT COUNT(*) FROM {tabela}");

    public Task AlterarPrecoAsync(int produtoId, decimal preco) =>
        EscalarAsync<int>("UPDATE Produtos SET Preco = @preco WHERE Id = @id; SELECT @@ROWCOUNT", ("@preco", preco), ("@id", produtoId));

    private async Task<T> EscalarAsync<T>(string sql, params (string Nome, object Valor)[] parametros)
    {
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync();
        await using var comando = new SqlCommand(sql, conexao);
        foreach (var (nome, valor) in parametros)
        {
            var parametro = comando.Parameters.AddWithValue(nome, valor);

            // AddWithValue infere DateTime como o tipo legado DATETIME (precisão de 1/300 s), que
            // arredonda 23:59:59.999 para o dia seguinte e quebrava o teste de borda do CA11.
            if (valor is DateTime)
            {
                parametro.SqlDbType = System.Data.SqlDbType.DateTime2;
            }
        }

        return (T)Convert.ChangeType((await comando.ExecuteScalarAsync())!, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }
}
