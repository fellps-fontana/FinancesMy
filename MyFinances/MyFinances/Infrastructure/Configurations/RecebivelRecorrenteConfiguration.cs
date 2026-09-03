using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFinances.Domain;

namespace MyFinances.Infrastructure.Configurations;

public class RecebivelRecorrenteConfiguration : IEntityTypeConfiguration<RecebivelRecorrente>
{
    public void Configure(EntityTypeBuilder<RecebivelRecorrente> builder)
    {
        builder.ToTable("recebivel_recorrente");

        builder.HasKey(rr => rr.Id);

        builder.Property(rr => rr.Id).HasColumnName("id");

        builder.Property(rr => rr.Descricao).HasColumnName("descricao").IsRequired();

        builder.Property(rr => rr.Valor).HasColumnName("valor").IsRequired();

        builder.Property(rr => rr.Periodicidade)
            .HasColumnName("periodicidade")
            .IsRequired()
            .HasConversion(
                v => v.ToStorageValue(),
                v => PeriodicidadeRecebivelExtensions.FromStorageValue(v));

        builder.Property(rr => rr.DiaVencimento).HasColumnName("dia_vencimento");

        builder.Property(rr => rr.MesReferencia).HasColumnName("mes_referencia");

        // Conversor aplicado so a valores nao-nulos pelo EF (propriedade nullable).
        builder.Property(rr => rr.DiaDaSemana)
            .HasColumnName("dia_da_semana")
            .HasConversion(
                v => v!.Value.ToStorageValue(),
                v => DiaDaSemanaExtensions.FromStorageValue(v));

        builder.Property(rr => rr.CategoriaId).HasColumnName("categoria_id");

        builder.Property(rr => rr.Ativa).HasColumnName("ativa").IsRequired().HasDefaultValue(true);

        builder.HasOne(rr => rr.Categoria)
            .WithMany()
            .HasForeignKey(rr => rr.CategoriaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(rr => rr.Ocorrencias)
            .WithOne(cr => cr.RecebivelRecorrente)
            .HasForeignKey(cr => cr.RecebivelRecorrenteId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
