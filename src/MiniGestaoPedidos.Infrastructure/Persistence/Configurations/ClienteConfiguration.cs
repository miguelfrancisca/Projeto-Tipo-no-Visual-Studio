using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniGestaoPedidos.Domain.Entities;

namespace MiniGestaoPedidos.Infrastructure.Persistence.Configurations;

internal sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nome).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(200).IsRequired();
        builder.Property(c => c.DataCadastro).HasColumnType("datetime2(0)");

        builder.HasIndex(c => c.Email).IsUnique().HasDatabaseName("UX_Clientes_Email");
    }
}
