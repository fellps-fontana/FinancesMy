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

    public DbSet<Categoria> Categorias { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Conta>()
            .Property(c => c.Ativa)
            .HasDefaultValue(true);

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

        modelBuilder.Entity<Categoria>()
            .Property(c => c.Arquivada)
            .HasDefaultValue(false);

        modelBuilder.Entity<Categoria>()
            .Property(c => c.Tipo)
            .HasConversion(
                v => v.ToStorageValue(),
                v => TipoCategoriaExtensions.FromStorageValue(v));

        modelBuilder.Entity<Categoria>()
            .HasOne(c => c.Parent)
            .WithMany(c => c.Subcategorias)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);
    }
}
