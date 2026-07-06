using Microsoft.EntityFrameworkCore;
using MyFinances.Models;

namespace MyFinances.Data;

public class MyFinancesDbContext : DbContext
{
    public MyFinancesDbContext(DbContextOptions<MyFinancesDbContext> options)
        : base(options)
    {
    }

    public DbSet<Conta> Contas { get; set; }

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
    }
}
