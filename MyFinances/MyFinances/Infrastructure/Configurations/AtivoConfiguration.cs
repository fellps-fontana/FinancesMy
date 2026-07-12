using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFinances.Domain;

namespace MyFinances.Infrastructure.Configurations;

public class AtivoConfiguration : IEntityTypeConfiguration<Ativo>
{
    public void Configure(EntityTypeBuilder<Ativo> builder)
    {
        builder.ToTable("ativo");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id");

        builder.Property(a => a.ContaId)
            .HasColumnName("conta_id")
            .IsRequired();

        builder.Property(a => a.Ticker)
            .HasColumnName("ticker")
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(a => a.Nome)
            .HasColumnName("nome")
            .HasMaxLength(255);

        builder.Property(a => a.Quantidade)
            .HasColumnName("quantidade")
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(a => a.PrecoMedio)
            .HasColumnName("preco_medio")
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(a => a.PrecoAtual)
            .HasColumnName("preco_atual")
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(a => a.Ativa)
            .HasColumnName("ativa")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();
    }
}
