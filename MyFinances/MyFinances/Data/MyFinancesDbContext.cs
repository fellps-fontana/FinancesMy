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

    public DbSet<Ativo> Ativos { get; set; }

    public DbSet<Categoria> Categorias { get; set; }

    public DbSet<DeParaCategoria> DeParaCategorias { get; set; }

    public DbSet<Lancamento> Lancamentos { get; set; }

    public DbSet<Transferencia> Transferencias { get; set; }

    public DbSet<Fatura> Faturas { get; set; }

    public DbSet<ContaReceber> ContasReceber { get; set; }

    public DbSet<CompraParcelada> ComprasParceladas { get; set; }

    public DbSet<ContaFixa> ContasFixas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new UsuarioConfiguration());
        modelBuilder.ApplyConfiguration(new ContaConfiguration());
        modelBuilder.ApplyConfiguration(new AtivoConfiguration());
        modelBuilder.ApplyConfiguration(new CategoriaConfiguration());
        modelBuilder.ApplyConfiguration(new DeParaCategoriaConfiguration());
        modelBuilder.ApplyConfiguration(new LancamentoConfiguration());
        modelBuilder.ApplyConfiguration(new TransferenciaConfiguration());
        modelBuilder.ApplyConfiguration(new FaturaConfiguration());
        modelBuilder.ApplyConfiguration(new ContaReceberConfiguration());
        modelBuilder.ApplyConfiguration(new CompraParceladaConfiguration());
        modelBuilder.ApplyConfiguration(new ContaFixaConfiguration());

        // Se nao eh Npgsql (ex: SQLite em testes), remove o default value SQL do campo CriadoEm
        // que eh sintaxe Postgres-only. Em producao (Npgsql), o UsuarioConfiguration mantem
        // o comportamento correto com now() AT TIME ZONE 'UTC'.
        if (!Database.IsNpgsql())
        {
            modelBuilder.Entity<Usuario>()
                .Property(u => u.CriadoEm)
                .HasDefaultValueSql(null);
        }
    }
}
