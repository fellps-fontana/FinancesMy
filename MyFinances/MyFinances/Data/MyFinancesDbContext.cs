using Microsoft.EntityFrameworkCore;
using MyFinances.Domain;
using MyFinances.Infrastructure.Configurations;

namespace MyFinances.Data;

public class MyFinancesDbContext : DbContext
{
    public MyFinancesDbContext(DbContextOptions<MyFinancesDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Conta> Contas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UsuarioConfiguration());
        modelBuilder.ApplyConfiguration(new ContaConfiguration());
    }
}
