using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFinances.Domain;

namespace MyFinances.Infrastructure.Configurations;

public class MovimentacaoAtivoConfiguration : IEntityTypeConfiguration<MovimentacaoAtivo>
{
    public void Configure(EntityTypeBuilder<MovimentacaoAtivo> builder)
    {
        builder.ToTable("movimentacao_ativo");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("id");

        builder.Property(m => m.AtivoId)
            .HasColumnName("ativo_id")
            .IsRequired();

        builder.Property(m => m.Tipo)
            .HasColumnName("tipo")
            .IsRequired()
            .HasConversion(
                v => v.ToStorageValue(),
                v => TipoMovimentacaoAtivoExtensions.FromStorageValue(v));

        builder.Property(m => m.Quantidade)
            .HasColumnName("quantidade")
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(m => m.PrecoUnitario)
            .HasColumnName("preco_unitario")
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(m => m.Data)
            .HasColumnName("data")
            .IsRequired();

        builder.Property(m => m.Observacao)
            .HasColumnName("observacao")
            .HasMaxLength(1000);
    }
}
