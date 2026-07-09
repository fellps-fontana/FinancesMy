using Microsoft.EntityFrameworkCore;
using MyFinances.Infrastructure.Configurations;
using MyFinances.Models;

namespace MyFinances.Data;

public class MyFinancesDbContext : DbContext
{
    public MyFinancesDbContext(DbContextOptions<MyFinancesDbContext> options)
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

        modelBuilder.Entity<Conta>()
            .Property(c => c.Origem)
            .HasConversion(
                v => v.ToStorageValue(),
                v => OrigemContaExtensions.FromStorageValue(v));

        modelBuilder.Entity<Conta>()
            .Property(c => c.Tipo)
            .HasConversion(
                v => v.HasValue ? v.Value.ToStorageValue() : null,
                v => v == null ? null : TipoContaExtensions.FromStorageValue(v));

        modelBuilder.ApplyConfiguration(new CategoriaConfiguration());
        modelBuilder.ApplyConfiguration(new LancamentoConfiguration());
        modelBuilder.ApplyConfiguration(new TransferenciaConfiguration());
        modelBuilder.ApplyConfiguration(new FaturaConfiguration());
    }
}
