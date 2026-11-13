using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniGestaoPedidos.Domain.Entities;

namespace MiniGestaoPedidos.Infrastructure.Persistence.Configurations;

internal sealed class ItemPedidoConfiguration : IEntityTypeConfiguration<ItemPedido>
{
    public void Configure(EntityTypeBuilder<ItemPedido> builder)
    {
        builder.ToTable("ItensPedido");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.PrecoUnitario).HasPrecision(18, 2);
        builder.Ignore(i => i.Subtotal); // calculado, não é coluna

        builder.HasOne(i => i.Produto)
            .WithMany()
            .HasForeignKey(i => i.ProdutoId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(i => new { i.PedidoId, i.ProdutoId }).IsUnique().HasDatabaseName("UX_ItensPedido_PedidoId_ProdutoId");
        builder.HasIndex(i => i.ProdutoId).HasDatabaseName("IX_ItensPedido_ProdutoId");
    }
}
