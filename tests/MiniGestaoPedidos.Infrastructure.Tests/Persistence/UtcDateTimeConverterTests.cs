using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MiniGestaoPedidos.Domain.Entities;
using MiniGestaoPedidos.Infrastructure.Persistence;

namespace MiniGestaoPedidos.Infrastructure.Tests.Persistence;

/// <summary>
/// RN14 — conversor de DateTime do AppDbContext (gravação, parâmetros de consulta e leitura).
/// O modelo é lido sem abrir conexão: não precisa de banco nem de Docker.
///
/// Atenção: Assert.Equal(DateTime, DateTime) compara só os ticks e IGNORA o Kind;
/// por isso o Kind é verificado separadamente em todos os testes.
/// </summary>
public class UtcDateTimeConverterTests
{
    private static readonly DateTime Referencia = new(2026, 11, 12, 14, 30, 0, 123);

    public static TheoryData<Type, string> PropriedadesDeData => new()
    {
        { typeof(Pedido), nameof(Pedido.DataPedido) },
        { typeof(Cliente), nameof(Cliente.DataCadastro) },
    };

    [Theory]
    [MemberData(nameof(PropriedadesDeData))]
    public void Gravacao_KindUtc_PassaInalterado(Type entidade, string propriedade)
    {
        var utc = DateTime.SpecifyKind(Referencia, DateTimeKind.Utc);

        var gravado = (DateTime)Conversor(entidade, propriedade).ConvertToProvider(utc)!;

        Assert.Equal(utc.Ticks, gravado.Ticks);
        Assert.Equal(DateTimeKind.Utc, gravado.Kind);
    }

    [Theory]
    [MemberData(nameof(PropriedadesDeData))]
    public void Gravacao_KindUnspecified_TrataComoUtcSemDeslocar(Type entidade, string propriedade)
    {
        // Nesta máquina (UTC-03:00), o código antigo (ToUniversalTime) somaria 3 horas aqui.
        var semKind = DateTime.SpecifyKind(Referencia, DateTimeKind.Unspecified);

        var gravado = (DateTime)Conversor(entidade, propriedade).ConvertToProvider(semKind)!;

        Assert.Equal(semKind.Ticks, gravado.Ticks);
        Assert.Equal(DateTimeKind.Utc, gravado.Kind);
    }

    [Theory]
    [MemberData(nameof(PropriedadesDeData))]
    public void Gravacao_KindLocal_LancaInvalidOperation(Type entidade, string propriedade)
    {
        var local = DateTime.SpecifyKind(Referencia, DateTimeKind.Local);

        var ex = Assert.Throws<InvalidOperationException>(() => Conversor(entidade, propriedade).ConvertToProvider(local));

        Assert.Contains("Kind=Local", ex.Message);
    }

    [Theory]
    [MemberData(nameof(PropriedadesDeData))]
    public void Leitura_ValorDoBanco_MarcaComoUtcSemDeslocar(Type entidade, string propriedade)
    {
        // O SqlClient devolve datetime2 como Kind=Unspecified.
        var doBanco = DateTime.SpecifyKind(Referencia, DateTimeKind.Unspecified);

        var lido = (DateTime)Conversor(entidade, propriedade).ConvertFromProvider(doBanco)!;

        Assert.Equal(doBanco.Ticks, lido.Ticks);
        Assert.Equal(DateTimeKind.Utc, lido.Kind);
    }

    [Fact]
    public void TodaPropriedadeDateTime_DoModelo_UsaOConversorUtc()
    {
        using var contexto = NovoContexto();

        var propriedades = contexto.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?))
            .ToList();

        Assert.NotEmpty(propriedades);
        Assert.All(propriedades, p =>
            Assert.True(
                p.GetValueConverter()?.GetType().Name == "UtcDateTimeConverter",
                $"{p.DeclaringType.ClrType.Name}.{p.Name} não usa o UtcDateTimeConverter."));
    }

    private static ValueConverter Conversor(Type entidade, string propriedade)
    {
        using var contexto = NovoContexto();
        return contexto.Model.FindEntityType(entidade)!.FindProperty(propriedade)!.GetValueConverter()
            ?? throw new InvalidOperationException($"{entidade.Name}.{propriedade} sem conversor.");
    }

    // Construir o modelo não abre conexão; a connection string nunca é usada.
    private static AppDbContext NovoContexto() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer("Server=nao-usado;Database=nao-usado").Options);
}
