using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFinances.Domain;

namespace MyFinances.Infrastructure.Configurations;

public class AtivoAporteConfiguration : IEntityTypeConfiguration<AtivoAporte>
{
    public void Configure(EntityTypeBuilder<AtivoAporte> builder)
    {
        builder.ToTable("ativo_aporte");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id");

        builder.Property(a => a.AtivoId)
            .HasColumnName("ativo_id")
            .IsRequired();

        builder.Property(a => a.Data)
            .HasColumnName("data")
            .IsRequired();

        builder.Property(a => a.Quantidade)
            .HasColumnName("quantidade")
            .IsRequired()
            .HasPrecision(18, 8);

        builder.Property(a => a.PrecoUnitario)
            .HasColumnName("preco_unitario")
            .IsRequired()
            .HasPrecision(18, 6);

        builder.Property(a => a.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        builder.Ignore(a => a.ValorTotal);

        builder.HasOne<Ativo>()
            .WithMany()
            .HasForeignKey(a => a.AtivoId);
    }
}
