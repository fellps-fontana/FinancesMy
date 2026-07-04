using Microsoft.EntityFrameworkCore;

namespace MyFinances.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
}
