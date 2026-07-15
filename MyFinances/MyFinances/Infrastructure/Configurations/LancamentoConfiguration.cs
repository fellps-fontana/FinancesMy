using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFinances.Domain;

namespace MyFinances.Infrastructure.Configurations;

public class LancamentoConfiguration : IEntityTypeConfiguration<Lancamento>
{
    public void Configure(EntityTypeBuilder<Lancamento> builder)
    {
        builder.ToTable("lancamento");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasColumnName("id");

        builder.Property(l => l.PierreTxnId)
            .HasColumnName("pierre_txn_id")
            .HasMaxLength(500);

        builder.Property(l => l.ContaId)
            .HasColumnName("conta_id")
            .IsRequired();

        builder.Property(l => l.CategoriaId)
            .HasColumnName("categoria_id");

        builder.Property(l => l.Descricao)
            .HasColumnName("descricao")
            .HasMaxLength(1000);

        builder.Property(l => l.Valor)
            .HasColumnName("valor")
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(l => l.Tipo)
            .HasColumnName("tipo")
            .IsRequired()
            .HasConversion(
                v => v.ToStorageValue(),
                v => TipoLancamentoExtensions.FromStorageValue(v));

        builder.Property(l => l.Data)
            .HasColumnName("data")
            .IsRequired();

        builder.Property(l => l.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasConversion(
                v => v.ToStorageValue(),
                v => StatusLancamentoExtensions.FromStorageValue(v));

        builder.Property(l => l.Manual)
            .HasColumnName("manual")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(l => l.Oculto)
            .HasColumnName("oculto")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(l => l.ContaFixaId)
            .HasColumnName("conta_fixa_id");

        builder.Property(l => l.ConciliadoCom)
            .HasColumnName("conciliado_com");

        builder.Property(l => l.TransferenciaId)
            .HasColumnName("transferencia_id");

        builder.Property(l => l.FaturaId)
            .HasColumnName("fatura_id");

        builder.Property(l => l.ContaReceberId)
            .HasColumnName("conta_receber_id");

        builder.HasOne(l => l.Conta)
            .WithMany()
            .HasForeignKey(l => l.ContaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Categoria)
            .WithMany(c => c.Lancamentos)
            .HasForeignKey(l => l.CategoriaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(l => l.LancamentoConciliado)
            .WithMany(l => l.LancamentosConciliados)
            .HasForeignKey(l => l.ConciliadoCom)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(l => l.Transferencia)
            .WithMany(t => t.Lancamentos)
            .HasForeignKey(l => l.TransferenciaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(l => l.Fatura)
            .WithMany(f => f.Lancamentos)
            .HasForeignKey(l => l.FaturaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(l => l.ContaReceber)
            .WithMany(cr => cr.Recebimentos)
            .HasForeignKey(l => l.ContaReceberId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indice para PierreTxnId se nao nulo (dedup)
        builder.HasIndex(l => l.PierreTxnId)
            .HasFilter("pierre_txn_id IS NOT NULL")
            .IsUnique()
            .HasDatabaseName("IX_lancamento_pierre_txn_id");
    }
}
