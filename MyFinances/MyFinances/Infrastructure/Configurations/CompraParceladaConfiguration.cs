using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFinances.Domain;

namespace MyFinances.Infrastructure.Configurations;

public class CompraParceladaConfiguration : IEntityTypeConfiguration<CompraParcelada>
{
    public void Configure(EntityTypeBuilder<CompraParcelada> builder)
    {
        builder.ToTable("compra_parcelada");

        builder.HasKey(cp => cp.Id);

        builder.Property(cp => cp.Id)
            .HasColumnName("id");

        builder.Property(cp => cp.Descricao)
            .HasColumnName("descricao")
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(cp => cp.ValorTotal)
            .HasColumnName("valor_total")
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(cp => cp.QuantidadeParcelas)
            .HasColumnName("quantidade_parcelas")
            .IsRequired();

        builder.Property(cp => cp.DataCompra)
            .HasColumnName("data_compra")
            .IsRequired();

        builder.HasMany(cp => cp.Lancamentos)
            .WithOne()
            .HasForeignKey("CompraParceladaId")
            .OnDelete(DeleteBehavior.SetNull);
    }
}
