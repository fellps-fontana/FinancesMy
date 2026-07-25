using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFinances.Domain;

namespace MyFinances.Infrastructure.Configurations;

public class LimiteGastoConfiguration : IEntityTypeConfiguration<LimiteGasto>
{
    public void Configure(EntityTypeBuilder<LimiteGasto> builder)
    {
        builder.ToTable("limite_gasto");

        builder.HasKey(lg => lg.Id);

        builder.Property(lg => lg.Id)
            .HasColumnName("id");

        builder.Property(lg => lg.CategoriaId)
            .HasColumnName("categoria_id")
            .IsRequired();

        builder.Property(lg => lg.ValorLimite)
            .HasColumnName("valor_limite")
            .IsRequired();

        builder.Property(lg => lg.Periodo)
            .HasColumnName("periodo")
            .IsRequired()
            .HasDefaultValue(PeriodoLimiteGasto.Mensal)
            .HasConversion(
                v => v.ToStorageValue(),
                v => PeriodoLimiteGastoExtensions.FromStorageValue(v));

        builder.HasOne(lg => lg.Categoria)
            .WithMany()
            .HasForeignKey(lg => lg.CategoriaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(lg => lg.CategoriaId)
            .IsUnique();
    }
}
