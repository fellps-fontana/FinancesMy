using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFinances.Domain;

namespace MyFinances.Infrastructure.Configurations;

public class ContaReceberConfiguration : IEntityTypeConfiguration<ContaReceber>
{
    public void Configure(EntityTypeBuilder<ContaReceber> builder)
    {
        builder.ToTable("conta_receber");

        builder.HasKey(cr => cr.Id);

        builder.Property(cr => cr.Id)
            .HasColumnName("id");

        builder.Property(cr => cr.Tipo)
            .HasColumnName("tipo")
            .IsRequired()
            .HasConversion(
                v => v.ToStorageValue(),
                v => TipoContaReceberExtensions.FromStorageValue(v));

        builder.Property(cr => cr.Descricao)
            .HasColumnName("descricao")
            .IsRequired();

        builder.Property(cr => cr.Pessoa)
            .HasColumnName("pessoa");

        builder.Property(cr => cr.ValorTotal)
            .HasColumnName("valor_total")
            .IsRequired();

        builder.Property(cr => cr.DataRegistro)
            .HasColumnName("data_registro")
            .IsRequired();

        builder.Property(cr => cr.DataPrevista)
            .HasColumnName("data_prevista");

        builder.Property(cr => cr.CategoriaId)
            .HasColumnName("categoria_id");

        builder.Property(cr => cr.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasConversion(
                v => v.ToStorageValue(),
                v => StatusContaReceberExtensions.FromStorageValue(v));

        builder.HasOne(cr => cr.Categoria)
            .WithMany()
            .HasForeignKey(cr => cr.CategoriaId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
