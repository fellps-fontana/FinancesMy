using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFinances.Domain;

namespace MyFinances.Infrastructure.Configurations;

public class ContaConfiguration : IEntityTypeConfiguration<Conta>
{
    public void Configure(EntityTypeBuilder<Conta> builder)
    {
        builder.ToTable("conta");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id");

        builder.Property(c => c.Nome)
            .HasColumnName("nome")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(c => c.Origem)
            .HasColumnName("origem")
            .IsRequired()
            .HasConversion(
                v => v.ToStorageValue(),
                v => OrigemContaExtensions.FromStorageValue(v));

        builder.Property(c => c.Tipo)
            .HasColumnName("tipo")
            .HasConversion(
                v => v.HasValue ? v.Value.ToStorageValue() : null,
                v => v == null ? null : TipoContaExtensions.FromStorageValue(v));

        builder.Property(c => c.PierreAccountId)
            .HasColumnName("pierre_account_id")
            .HasMaxLength(500);

        builder.Property(c => c.SaldoManual)
            .HasColumnName("saldo_manual")
            .HasPrecision(18, 2);

        builder.Property(c => c.DiaFechamento)
            .HasColumnName("dia_fechamento");

        builder.Property(c => c.DiaVencimento)
            .HasColumnName("dia_vencimento");

        builder.Property(c => c.Ativa)
            .HasColumnName("ativa")
            .IsRequired()
            .HasDefaultValue(true);
    }
}
