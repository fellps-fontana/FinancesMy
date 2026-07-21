using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFinances.Domain;

namespace MyFinances.Infrastructure.Configurations;

public class ContaFixaConfiguration : IEntityTypeConfiguration<ContaFixa>
{
    public void Configure(EntityTypeBuilder<ContaFixa> builder)
    {
        builder.ToTable("conta_fixa");

        builder.HasKey(cf => cf.Id);

        builder.Property(cf => cf.Id)
            .HasColumnName("id");

        builder.Property(cf => cf.ContaId)
            .HasColumnName("conta_id")
            .IsRequired();

        builder.Property(cf => cf.CategoriaId)
            .HasColumnName("categoria_id");

        builder.Property(cf => cf.Descricao)
            .HasColumnName("descricao")
            .IsRequired();

        builder.Property(cf => cf.Valor)
            .HasColumnName("valor")
            .IsRequired();

        builder.Property(cf => cf.DiaVencimento)
            .HasColumnName("dia_vencimento")
            .IsRequired();

        builder.Property(cf => cf.Ativa)
            .HasColumnName("ativa")
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasOne(cf => cf.Conta)
            .WithMany()
            .HasForeignKey(cf => cf.ContaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cf => cf.Categoria)
            .WithMany()
            .HasForeignKey(cf => cf.CategoriaId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
