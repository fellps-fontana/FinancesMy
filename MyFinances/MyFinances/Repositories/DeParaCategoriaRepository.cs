using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;

namespace MyFinances.Repositories;

public class DeParaCategoriaRepository : IDeParaCategoriaRepository
{
    private readonly MyFinancesDbContext _context;

    public DeParaCategoriaRepository(MyFinancesDbContext context)
    {
        _context = context;
    }

    public async Task Adicionar(DeParaCategoria deParaCategoria)
    {
        await _context.DeParaCategorias.AddAsync(deParaCategoria);
    }

    public async Task<DeParaCategoria?> ObterPorId(Guid id)
    {
        return await _context.DeParaCategorias
            .Include(d => d.Categoria)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<DeParaCategoria?> ObterPorCategoriaPierre(string categoriaPierre)
    {
        return await _context.DeParaCategorias
            .Include(d => d.Categoria)
            .FirstOrDefaultAsync(d => d.CategoriaPierre == categoriaPierre);
    }

    public async Task<IEnumerable<DeParaCategoria>> Listar(string? categoriaPierre = null)
    {
        var query = _context.DeParaCategorias
            .Include(d => d.Categoria)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(categoriaPierre))
        {
            query = query.Where(d => d.CategoriaPierre == categoriaPierre);
        }

        return await query.ToListAsync();
    }

    public async Task Remover(DeParaCategoria deParaCategoria)
    {
        _context.DeParaCategorias.Remove(deParaCategoria);
    }

    public async Task Salvar()
    {
        await _context.SaveChangesAsync();
    }
}
