using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Models;

namespace MyFinances.Repositories;

public class CategoriaRepository : ICategoriaRepository
{
    private readonly MyFinancesDbContext _context;

    public CategoriaRepository(MyFinancesDbContext context)
    {
        _context = context;
    }

    public async Task Adicionar(Categoria categoria)
    {
        await _context.Categorias.AddAsync(categoria);
    }

    public async Task<Categoria?> ObterPorId(Guid id)
    {
        return await _context.Categorias
            .Include(c => c.Subcategorias)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Categoria>> Listar(TipoCategoria? tipo = null, bool? arquivada = null, Guid? parentId = null)
    {
        var query = _context.Categorias
            .Include(c => c.Subcategorias)
            .AsQueryable();

        if (tipo.HasValue)
        {
            query = query.Where(c => c.Tipo == tipo.Value);
        }

        if (arquivada.HasValue)
        {
            query = query.Where(c => c.Arquivada == arquivada.Value);
        }
        else
        {
            query = query.Where(c => !c.Arquivada);
        }

        if (parentId.HasValue)
        {
            query = query.Where(c => c.ParentId == parentId.Value);
        }

        return await query.ToListAsync();
    }

    public async Task Salvar()
    {
        await _context.SaveChangesAsync();
    }
}
