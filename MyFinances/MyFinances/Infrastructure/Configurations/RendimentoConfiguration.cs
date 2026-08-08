using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFinances.Domain;

namespace MyFinances.Infrastructure.Configurations;

public class RendimentoConfiguration : IEntityTypeConfiguration<Rendimento>
{
    public void Configure(EntityTypeBuilder<Rendimento> builder)
    {
        builder.ToTable("rendimento");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id");

        builder.Property(r => r.AtivoId)
            .HasColumnName("ativo_id")
            .IsRequired();

        builder.Property(r => r.Tipo)
            .HasColumnName("tipo")
            .IsRequired()
            .HasConversion(
                v => v.ToStorageValue(),
                v => TipoRendimentoExtensions.FromStorageValue(v));

        builder.Property(r => r.Origem)
            .HasColumnName("origem")
            .IsRequired()
            .HasConversion(
                v => v.ToStorageValue(),
                v => OrigemRendimentoExtensions.FromStorageValue(v));

        builder.Property(r => r.Valor)
            .HasColumnName("valor")
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(r => r.Data)
            .HasColumnName("data")
            .IsRequired();

        builder.Property(r => r.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        builder.HasOne(r => r.Ativo)
            .WithMany(a => a.Rendimentos)
            .HasForeignKey(r => r.AtivoId)
            .HasConstraintName("fk_rendimento_ativo_id");
    }
}
