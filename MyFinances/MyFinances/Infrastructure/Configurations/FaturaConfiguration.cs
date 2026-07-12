using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFinances.Domain;

namespace MyFinances.Infrastructure.Configurations;

public class FaturaConfiguration : IEntityTypeConfiguration<Fatura>
{
    public void Configure(EntityTypeBuilder<Fatura> builder)
    {
        builder.ToTable("fatura");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasColumnName("id");

        builder.Property(f => f.ContaId)
            .HasColumnName("conta_id")
            .IsRequired();

        builder.Property(f => f.DataFechamento)
            .HasColumnName("data_fechamento")
            .IsRequired();

        builder.Property(f => f.DataVencimento)
            .HasColumnName("data_vencimento")
            .IsRequired();

        builder.Property(f => f.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasConversion(
                v => v.ToStorageValue(),
                v => StatusFaturaExtensions.FromStorageValue(v));

        builder.HasOne(f => f.Conta)
            .WithMany()
            .HasForeignKey(f => f.ContaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indice unico: uma fatura aberta por conta
        builder.HasIndex(f => new { f.ContaId, f.Status })
            .HasFilter("status = 'ABERTA'")
            .IsUnique()
            .HasDatabaseName("IX_fatura_conta_aberta");
    }
}
