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
            .Include(c => c.Parent)
            .Include(c => c.Subcategorias)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Categoria>> ListarTodas()
    {
        return await _context.Categorias
            .Include(c => c.Subcategorias)
            .Where(c => c.ParentId == null)
            .ToListAsync();
    }

    public async Task<IEnumerable<Categoria>> ListarPorTipo(TipoCategoria tipo)
    {
        return await _context.Categorias
            .Include(c => c.Subcategorias)
            .Where(c => c.Tipo == tipo && c.ParentId == null && !c.Arquivada)
            .ToListAsync();
    }

    public async Task Atualizar(Categoria categoria)
    {
        _context.Categorias.Update(categoria);
    }

    public async Task Salvar()
    {
        await _context.SaveChangesAsync();
    }
}
