using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Dtos;

namespace MyFinances.Services;

public class RelatorioCategoriaService
{
    private readonly AppDbContext _context;

    public RelatorioCategoriaService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<RelatorioCategoriaResponseDto> ObterGastoPorCategoriaAsync(
        int ano,
        int mes,
        Guid? contaIdFiltro = null)
    {
        ValidarMesAno(ano, mes);

        var primeiroDia = new DateOnly(ano, mes, 1);
        var ultimoDia = new DateOnly(ano, mes, DateTime.DaysInMonth(ano, mes));

        var query = _context.Lancamentos
            .Where(l => l.FaturaId != null)
            .Where(l => l.Data >= primeiroDia && l.Data <= ultimoDia);

        if (contaIdFiltro.HasValue)
        {
            query = query.Where(l => l.ContaId == contaIdFiltro.Value);
        }

        var itens = await query
            .GroupBy(l => l.CategoriaId)
            .Select(g => new RelatorioCategoriaResponseDto.ItemPorCategoria
            {
                CategoriaId = g.Key,
                NomeCategoria = g.Select(l => l.Categoria != null ? l.Categoria.Nome : null).FirstOrDefault(),
                Total = g.Sum(l => l.Valor)
            })
            .OrderBy(i => i.NomeCategoria)
            .ToListAsync();

        return new RelatorioCategoriaResponseDto
        {
            Itens = itens,
            Mes = mes,
            Ano = ano
        };
    }

    private static void ValidarMesAno(int ano, int mes)
    {
        if (mes < 1 || mes > 12)
        {
            throw new ArgumentException("Mes deve estar entre 1 e 12.", nameof(mes));
        }

        if (ano < 2000 || ano > 2099)
        {
            throw new ArgumentException("Ano deve estar entre 2000 e 2099.", nameof(ano));
        }
    }
}
