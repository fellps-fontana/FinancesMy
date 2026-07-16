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

        builder.Property(a => a.Nome)
            .HasColumnName("nome")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(a => a.Tipo)
            .HasColumnName("tipo")
            .IsRequired()
            .HasConversion(
                v => v.ToStorageValue(),
                v => TipoAtivoExtensions.FromStorageValue(v));

        builder.Property(a => a.Instituicao)
            .HasColumnName("instituicao")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(a => a.ValorInvestido)
            .HasColumnName("valor_investido")
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(a => a.ValorAtual)
            .HasColumnName("valor_atual")
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(a => a.DataCompra)
            .HasColumnName("data_compra")
            .IsRequired();

        builder.Property(a => a.Ativa)
            .HasColumnName("ativa")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.CriadoEm)
            .HasColumnName("criado_em")
            .IsRequired();

        builder.Property(a => a.AtualizadoEm)
            .HasColumnName("atualizado_em");
    }
}
