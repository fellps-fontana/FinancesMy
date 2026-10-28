using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFinances.Domain;

namespace MyFinances.Infrastructure.Configurations;

public class PlanoMensalConfiguration : IEntityTypeConfiguration<PlanoMensal>
{
    public void Configure(EntityTypeBuilder<PlanoMensal> builder)
    {
        builder.ToTable("plano_mensal");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.Ano)
            .HasColumnName("ano")
            .IsRequired();

        builder.Property(p => p.Mes)
            .HasColumnName("mes")
            .IsRequired();

        builder.Property(p => p.RendaPrevista)
            .HasColumnName("renda_prevista")
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.PercentualInvestimento)
            .HasColumnName("percentual_investimento")
            .IsRequired()
            .HasPrecision(5, 2);

        builder.HasMany(p => p.Sublimites)
            .WithOne(s => s.PlanoMensal)
            .HasForeignKey(s => s.PlanoMensalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.Ano, p.Mes })
            .IsUnique()
            .HasDatabaseName("IX_plano_mensal_ano_mes");
    }
}
