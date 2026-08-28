using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;

namespace MyFinances.Repositories;

public class RecebivelRecorrenteRepository : IRecebivelRecorrenteRepository
{
    private readonly MyFinancesDbContext _context;

    public RecebivelRecorrenteRepository(MyFinancesDbContext context)
    {
        _context = context;
    }

    public async Task Adicionar(RecebivelRecorrente recebivelRecorrente)
    {
        await _context.RecebiveisRecorrentes.AddAsync(recebivelRecorrente);
    }

    public async Task<RecebivelRecorrente?> ObterPorId(Guid id)
    {
        return await QueryComRelacionamentos()
            .FirstOrDefaultAsync(rr => rr.Id == id);
    }

    public async Task<IEnumerable<RecebivelRecorrente>> Listar(bool? ativaFiltro = null)
    {
        var query = QueryComRelacionamentos();

        if (ativaFiltro.HasValue)
        {
            query = query.Where(rr => rr.Ativa == ativaFiltro.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<RecebivelRecorrente>> ListarAtivos()
    {
        return await QueryComRelacionamentos()
            .Where(rr => rr.Ativa)
            .ToListAsync();
    }

    public async Task Atualizar(RecebivelRecorrente recebivelRecorrente)
    {
        _context.RecebiveisRecorrentes.Update(recebivelRecorrente);
    }

    public async Task<bool> ExisteOcorrenciaGerada(Guid recebivelRecorrenteId, DateOnly dataOcorrencia)
    {
        return await _context.ContasReceber
            .AnyAsync(cr =>
                cr.RecebivelRecorrenteId == recebivelRecorrenteId &&
                cr.DataPrevista == dataOcorrencia);
    }

    public async Task Salvar()
    {
        await _context.SaveChangesAsync();
    }

    private IQueryable<RecebivelRecorrente> QueryComRelacionamentos()
    {
        return _context.RecebiveisRecorrentes
            .Include(rr => rr.Categoria)
            .Include(rr => rr.Ocorrencias);
    }
}
