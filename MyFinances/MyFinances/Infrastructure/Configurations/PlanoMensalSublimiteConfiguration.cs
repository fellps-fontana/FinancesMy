using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFinances.Domain;

namespace MyFinances.Infrastructure.Configurations;

public class PlanoMensalSublimiteConfiguration : IEntityTypeConfiguration<PlanoMensalSublimite>
{
    public void Configure(EntityTypeBuilder<PlanoMensalSublimite> builder)
    {
        builder.ToTable("plano_mensal_sublimite");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id");

        builder.Property(s => s.PlanoMensalId)
            .HasColumnName("plano_mensal_id")
            .IsRequired();

        builder.Property(s => s.CategoriaId)
            .HasColumnName("categoria_id")
            .IsRequired();

        builder.Property(s => s.Percentual)
            .HasColumnName("percentual")
            .IsRequired()
            .HasPrecision(5, 2);

        // Relacao com o plano (cascade) e configurada em PlanoMensalConfiguration.
        builder.HasOne(s => s.Categoria)
            .WithMany()
            .HasForeignKey(s => s.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.PlanoMensalId, s.CategoriaId })
            .IsUnique()
            .HasDatabaseName("IX_plano_mensal_sublimite_plano_categoria");
    }
}
