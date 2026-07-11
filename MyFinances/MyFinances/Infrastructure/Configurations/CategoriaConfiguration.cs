using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFinances.Domain;

namespace MyFinances.Infrastructure.Configurations;

public class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.ToTable("categoria");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id");

        builder.Property(c => c.Nome)
            .HasColumnName("nome")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(c => c.Tipo)
            .HasColumnName("tipo")
            .IsRequired()
            .HasConversion(
                v => v.ToStorageValue(),
                v => TipoCategoriaExtensions.FromStorageValue(v));

        builder.Property(c => c.ParentId)
            .HasColumnName("parent_id");

        builder.Property(c => c.Arquivada)
            .HasColumnName("arquivada")
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasOne(c => c.Parent)
            .WithMany(c => c.Subcategorias)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
