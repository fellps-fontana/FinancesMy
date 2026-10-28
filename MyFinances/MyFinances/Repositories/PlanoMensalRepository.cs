using Microsoft.EntityFrameworkCore;
using Npgsql;
using MyFinances.Data;
using MyFinances.Domain;

namespace MyFinances.Repositories;

public class PlanoMensalRepository : IPlanoMensalRepository
{
    private const string IndiceAnoMes = "IX_plano_mensal_ano_mes";

    private readonly MyFinancesDbContext _context;

    public PlanoMensalRepository(MyFinancesDbContext context)
    {
        _context = context;
    }

    public async Task<PlanoMensal?> ObterPorAnoMes(int ano, int mes)
    {
        return await QueryComSublimites()
            .FirstOrDefaultAsync(p => p.Ano == ano && p.Mes == mes);
    }

    public async Task<PlanoMensal?> ObterMaisRecenteAnteriorA(int ano, int mes)
    {
        return await QueryComSublimites()
            .Where(p => p.Ano < ano || (p.Ano == ano && p.Mes < mes))
            .OrderByDescending(p => p.Ano)
            .ThenByDescending(p => p.Mes)
            .FirstOrDefaultAsync();
    }

    // Geracao concorrente do mesmo (ano, mes): quem perde a corrida no indice unico descarta o proprio
    // plano e recebe o que ja foi gravado.
    // LIMITACAO: a recuperacao pelo vencedor so vale FORA de transacao explicita no Postgres. Dentro de
    // BeginTransactionAsync, a violacao do indice aborta a transacao (SQLSTATE 25P02) e a releitura
    // falha; entao o erro sobe e a operacao inteira e desfeita, sem duplicata gravada.
    public async Task<PlanoMensal> AdicionarOuObterExistente(PlanoMensal plano)
    {
        _context.PlanosMensais.Add(plano);

        try
        {
            await _context.SaveChangesAsync();
            return plano;
        }
        catch (DbUpdateException ex) when (EhViolacaoIndiceAnoMes(ex))
        {
            DescartarPlano(plano);

            var vencedor = await ObterPorAnoMes(plano.Ano, plano.Mes);
            if (vencedor is null)
            {
                throw;
            }

            return vencedor;
        }
    }

    public Task AdicionarSublimite(PlanoMensalSublimite sublimite)
    {
        _context.PlanoMensalSublimites.Add(sublimite);
        return Task.CompletedTask;
    }

    public Task RemoverSublimite(PlanoMensalSublimite sublimite)
    {
        _context.PlanoMensalSublimites.Remove(sublimite);
        return Task.CompletedTask;
    }

    public async Task Salvar()
    {
        await _context.SaveChangesAsync();
    }

    private IQueryable<PlanoMensal> QueryComSublimites()
    {
        return _context.PlanosMensais
            .Include(p => p.Sublimites);
    }

    private static bool EhViolacaoIndiceAnoMes(DbUpdateException ex) =>
        ex.InnerException is PostgresException pgEx
        && pgEx.SqlState == PostgresErrorCodes.UniqueViolation
        && pgEx.ConstraintName == IndiceAnoMes;

    // Desfaz o rastreamento do plano e dos sublimites que entraram junto no mesmo SaveChanges.
    private void DescartarPlano(PlanoMensal plano)
    {
        foreach (var sublimite in plano.Sublimites)
        {
            _context.Entry(sublimite).State = EntityState.Detached;
        }

        _context.Entry(plano).State = EntityState.Detached;
    }
}
