using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniGestaoPedidos.Domain.Entities;

namespace MiniGestaoPedidos.Infrastructure.Persistence.Configurations;

internal sealed class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("Produtos");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nome).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Sku).HasMaxLength(30).IsUnicode(false).IsRequired();
        builder.Property(p => p.Preco).HasPrecision(18, 2);

        // RN10: UPDATE ... WHERE Id = @id AND RowVersion = @original. 0 linhas afetadas → DbUpdateConcurrencyException.
        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.HasIndex(p => p.Sku).IsUnique().HasDatabaseName("UX_Produtos_Sku");
    }
}
