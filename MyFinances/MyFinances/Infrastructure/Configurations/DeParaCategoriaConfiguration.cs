using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFinances.Domain;

namespace MyFinances.Infrastructure.Configurations;

public class DeParaCategoriaConfiguration : IEntityTypeConfiguration<DeParaCategoria>
{
    public void Configure(EntityTypeBuilder<DeParaCategoria> builder)
    {
        builder.ToTable("de_para_categoria");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id");

        builder.Property(d => d.CategoriaPierre)
            .HasColumnName("categoria_pierre")
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.CategoriaId)
            .HasColumnName("categoria_id")
            .IsRequired();

        builder.HasOne(d => d.Categoria)
            .WithMany()
            .HasForeignKey(d => d.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.CategoriaPierre)
            .IsUnique();
    }
}
