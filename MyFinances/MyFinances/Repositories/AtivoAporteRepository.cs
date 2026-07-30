using MyFinances.Data;
using MyFinances.Domain;

namespace MyFinances.Repositories;

public class AtivoAporteRepository : IAtivoAporteRepository
{
    private readonly MyFinancesDbContext _context;

    public AtivoAporteRepository(MyFinancesDbContext context)
    {
        _context = context;
    }

    public Task Adicionar(AtivoAporte aporte)
    {
        _context.AtivoAportes.Add(aporte);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<AtivoAporte>> ListarPorAtivo(Guid ativoId)
    {
        return await Task.FromResult(_context.AtivoAportes
            .Where(a => a.AtivoId == ativoId)
            .OrderBy(a => a.Data)
            .AsEnumerable());
    }

    public async Task Salvar()
    {
        await _context.SaveChangesAsync();
    }
}
