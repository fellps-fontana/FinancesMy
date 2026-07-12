using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFinances.Domain;

namespace MyFinances.Infrastructure.Configurations;

public class TransferenciaConfiguration : IEntityTypeConfiguration<Transferencia>
{
    public void Configure(EntityTypeBuilder<Transferencia> builder)
    {
        builder.ToTable("transferencia");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id");

        builder.Property(t => t.Data)
            .HasColumnName("data")
            .IsRequired();

        builder.Property(t => t.Valor)
            .HasColumnName("valor")
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(t => t.ContaOrigemId)
            .HasColumnName("conta_origem_id")
            .IsRequired();

        builder.Property(t => t.ContaDestinoId)
            .HasColumnName("conta_destino_id")
            .IsRequired();

        builder.Property(t => t.FaturaId)
            .HasColumnName("fatura_id");

        builder.Property(t => t.Descricao)
            .HasColumnName("descricao")
            .HasMaxLength(1000);

        builder.HasOne(t => t.ContaOrigem)
            .WithMany()
            .HasForeignKey(t => t.ContaOrigemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.ContaDestino)
            .WithMany()
            .HasForeignKey(t => t.ContaDestinoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Fatura)
            .WithMany(f => f.Transferencias)
            .HasForeignKey(t => t.FaturaId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
