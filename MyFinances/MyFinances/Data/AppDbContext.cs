using Microsoft.EntityFrameworkCore;
using MyFinances.Models;

namespace MyFinances.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Conta> Contas { get; set; }
    public DbSet<Categoria> Categorias { get; set; }
    public DbSet<Lancamento> Lancamentos { get; set; }
    public DbSet<Transferencia> Transferencias { get; set; }
    public DbSet<Fatura> Faturas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Conta
        modelBuilder.Entity<Conta>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Origem).IsRequired().HasMaxLength(50); // OPEN_FINANCE | MANUAL
            entity.Property(e => e.Tipo).HasMaxLength(50); // BANCO | CARTAO | INVESTIMENTO
            entity.Property(e => e.PierreAccountId).HasMaxLength(255);
            entity.Property(e => e.SaldoManual).HasPrecision(18, 2);
            entity.Property(e => e.Ativa).HasDefaultValue(true);

            entity.HasMany(e => e.Lancamentos)
                .WithOne(l => l.Conta)
                .HasForeignKey(l => l.ContaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.TransferenciasOrigem)
                .WithOne(t => t.ContaOrigem)
                .HasForeignKey(t => t.ContaOrigemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.TransferenciasDestino)
                .WithOne(t => t.ContaDestino)
                .HasForeignKey(t => t.ContaDestinoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Faturas)
                .WithOne(f => f.Conta)
                .HasForeignKey(f => f.ContaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Categoria
        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Tipo).IsRequired().HasMaxLength(50); // DESPESA | RECEITA
            entity.Property(e => e.Arquivada).HasDefaultValue(false);

            entity.HasOne(e => e.Parent)
                .WithMany(e => e.Subcategorias)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Lancamentos)
                .WithOne(l => l.Categoria)
                .HasForeignKey(l => l.CategoriaId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Lancamento
        modelBuilder.Entity<Lancamento>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Descricao).HasMaxLength(500);
            entity.Property(e => e.Valor).IsRequired().HasPrecision(18, 2);
            entity.Property(e => e.Tipo).IsRequired().HasMaxLength(50); // DEBIT | CREDIT
            entity.Property(e => e.Data).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50); // PENDENTE | SUGERIDO | PAGO
            entity.Property(e => e.Manual).HasDefaultValue(false);
            entity.Property(e => e.Oculto).HasDefaultValue(false);
            entity.Property(e => e.PierreTxnId).HasMaxLength(255);

            // Indice unico em pierre_txn_id quando nao nulo
            entity.HasIndex(e => e.PierreTxnId)
                .IsUnique()
                .HasFilter("\"PierreTxnId\" IS NOT NULL");

            entity.HasOne(e => e.Categoria)
                .WithMany(c => c.Lancamentos)
                .HasForeignKey(e => e.CategoriaId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.LancamentoConciliado)
                .WithMany(e => e.LancamentosConciliados)
                .HasForeignKey(e => e.ConciliadoCom)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Transferencia)
                .WithMany(t => t.Lancamentos)
                .HasForeignKey(e => e.TransferenciaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Fatura)
                .WithMany(f => f.Lancamentos)
                .HasForeignKey(e => e.FaturaId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Transferencia
        modelBuilder.Entity<Transferencia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Data).IsRequired();
            entity.Property(e => e.Valor).IsRequired().HasPrecision(18, 2);
            entity.Property(e => e.Descricao).HasMaxLength(500);

            entity.HasOne(e => e.ContaOrigem)
                .WithMany(c => c.TransferenciasOrigem)
                .HasForeignKey(e => e.ContaOrigemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ContaDestino)
                .WithMany(c => c.TransferenciasDestino)
                .HasForeignKey(e => e.ContaDestinoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Lancamentos)
                .WithOne(l => l.Transferencia)
                .HasForeignKey(l => l.TransferenciaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Fatura
        modelBuilder.Entity<Fatura>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DataFechamento).IsRequired();
            entity.Property(e => e.DataVencimento).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50); // ABERTA | FECHADA | PAGA

            entity.HasOne(e => e.Transferencia)
                .WithOne(t => t.Fatura)
                .HasForeignKey<Fatura>(e => e.TransferenciaId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Lancamentos)
                .WithOne(l => l.Fatura)
                .HasForeignKey(l => l.FaturaId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.ContaId)
                .IsUnique()
                .HasFilter("\"Status\" = 'ABERTA'");
        });
    }
}
