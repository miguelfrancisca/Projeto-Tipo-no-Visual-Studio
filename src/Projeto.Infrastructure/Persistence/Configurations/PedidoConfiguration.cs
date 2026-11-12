using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Projeto.Domain.Entities;

namespace Projeto.Infrastructure.Persistence.Configurations;

internal sealed class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.ToTable("Pedidos");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.DataPedido).HasColumnType("datetime2(3)");
        builder.Property(p => p.ValorTotal).HasPrecision(18, 2);

        builder.HasOne(p => p.Cliente)
            .WithMany()
            .HasForeignKey(p => p.ClienteId)
            .OnDelete(DeleteBehavior.NoAction);

        // Coleção exposta como IReadOnlyCollection; o EF usa o campo privado _itens.
        builder.HasMany(p => p.Itens)
            .WithOne()
            .HasForeignKey(i => i.PedidoId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(p => new { p.ClienteId, p.DataPedido }).HasDatabaseName("IX_Pedidos_ClienteId_DataPedido");
        builder.HasIndex(p => p.DataPedido).HasDatabaseName("IX_Pedidos_DataPedido");
    }
}
