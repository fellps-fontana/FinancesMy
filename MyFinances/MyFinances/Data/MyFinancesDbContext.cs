using Microsoft.EntityFrameworkCore;

namespace MyFinances.Data;

public class MyFinancesDbContext : DbContext
{
    public MyFinancesDbContext(DbContextOptions<MyFinancesDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
