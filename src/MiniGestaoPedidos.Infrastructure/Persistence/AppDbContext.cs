using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MiniGestaoPedidos.Domain.Entities;

namespace MiniGestaoPedidos.Infrastructure.Persistence;

/// <summary>
/// Mapeia as tabelas criadas por database/init/01-schema.sql.
/// O schema NÃO é gerado pelo EF (sem migrations / EnsureCreated).
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();

    public DbSet<Produto> Produtos => Set<Produto>();

    public DbSet<Pedido> Pedidos => Set<Pedido>();

    public DbSet<ItemPedido> ItensPedido => Set<ItemPedido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Todas as datas são gravadas em UTC; ao ler, marca o Kind como Utc para a API serializar com "Z".
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    private sealed class UtcDateTimeConverter()
        : ValueConverter<DateTime, DateTime>(
            v => ParaGravacao(v),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    /// <summary>
    /// Também é aplicado aos PARÂMETROS das consultas. Nunca chama ToUniversalTime(), que usaria o
    /// fuso da máquina do servidor (RN14): Utc passa direto; Unspecified é tratado como UTC (é o
    /// que o banco guarda); Local é erro de programação — a conversão deve vir de um DateTimeOffset.
    /// </summary>
    private static DateTime ParaGravacao(DateTime valor) => valor.Kind switch
    {
        DateTimeKind.Utc => valor,
        DateTimeKind.Unspecified => DateTime.SpecifyKind(valor, DateTimeKind.Utc),
        _ => throw new InvalidOperationException(
            "DateTime com Kind=Local não pode ir para o banco: converta a partir de um DateTimeOffset (UtcDateTime)."),
    };
}
