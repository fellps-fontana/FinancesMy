using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFinances.Domain;

namespace MyFinances.Infrastructure.Configurations;

public class AssinaturaCartaoConfiguration : IEntityTypeConfiguration<AssinaturaCartao>
{
    public void Configure(EntityTypeBuilder<AssinaturaCartao> builder)
    {
        builder.ToTable("assinatura_cartao");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id).HasColumnName("id");

        builder.Property(a => a.ContaId)
            .HasColumnName("conta_id")
            .IsRequired();

        builder.Property(a => a.CategoriaId)
            .HasColumnName("categoria_id");

        builder.Property(a => a.Descricao)
            .HasColumnName("descricao")
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(a => a.Valor)
            .HasColumnName("valor")
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(a => a.DiaReferencia)
            .HasColumnName("dia_referencia")
            .IsRequired();

        builder.Property(a => a.Ativa)
            .HasColumnName("ativa")
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasOne(a => a.Conta)
            .WithMany()
            .HasForeignKey(a => a.ContaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Categoria)
            .WithMany()
            .HasForeignKey(a => a.CategoriaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(a => a.Compras)
            .WithOne(l => l.AssinaturaCartao)
            .HasForeignKey(l => l.AssinaturaCartaoId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
