using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Respawn;
using Testcontainers.MsSql;

namespace Projeto.Api.IntegrationTests.Infra;

/// <summary>
/// Sobe um SQL Server real em container, cria o schema com o MESMO script usado pelo
/// docker-compose (database/init/01-schema.sql) e hospeda a API apontando para ele.
/// Compartilhado por todos os testes da coleção "api"; os dados são limpos entre testes com Respawn.
/// </summary>
public sealed partial class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Mesma imagem do docker-compose.yml.
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    private Respawner _respawner = null!;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await ExecutarScriptAsync(_container.GetConnectionString(), LocalizarArquivo("database", "init", "01-schema.sql"));

        ConnectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = "PedidosDb",
        }.ConnectionString;

        _respawner = await Respawner.CreateAsync(ConnectionString, new RespawnerOptions { WithReseed = true });
    }

    public async Task LimparBancoAsync() => await _respawner.ResetAsync(ConnectionString);

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _container.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:PedidosDb", ConnectionString);
    }

    /// <summary>Executa um script .sql dividindo-o nos separadores GO (como o sqlcmd faz).</summary>
    private static async Task ExecutarScriptAsync(string connectionString, string caminho)
    {
        var lotes = SeparadorGo().Split(await File.ReadAllTextAsync(caminho))
            .Where(l => !string.IsNullOrWhiteSpace(l));

        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync();
        foreach (var lote in lotes)
        {
            await using var comando = new SqlCommand(lote, conexao);
            await comando.ExecuteNonQueryAsync();
        }
    }

    private static string LocalizarArquivo(params string[] partes)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.GetFiles("*.sln").Any())
        {
            dir = dir.Parent;
        }

        return Path.Combine([dir?.FullName ?? throw new InvalidOperationException("Raiz do repositório não encontrada."), .. partes]);
    }

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex SeparadorGo();
}
