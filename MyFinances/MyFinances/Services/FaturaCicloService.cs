using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Domain;
using MyFinances.Models;

namespace MyFinances.Services;

public class FaturaCicloService
{
    private readonly AppDbContext _context;

    public FaturaCicloService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Resolve a Fatura ABERTA vigente para um ciclo de cartao.
    ///
    /// Ciclo: Dado dia_fechamento e dia_vencimento da conta CARTAO:
    /// - Se dataReferencia.Day < dia_fechamento: ciclo fecha neste mes
    /// - Se dataReferencia.Day >= dia_fechamento: ciclo fecha no proximo mes
    ///
    /// Vencimento pode cair no mesmo mes ou no mes seguinte, dependendo se
    /// dia_vencimento > dia_fechamento (mesmo mes) ou dia_vencimento <= dia_fechamento (proximo mes).
    ///
    /// Exemplos:
    /// - dia_fechamento=10, dia_vencimento=20, dataReferencia=05/03 (5 de marco)
    ///   => fecha=10/03, vence=20/03 (mesmo mes, pois 20 > 10)
    /// - dia_fechamento=10, dia_vencimento=05, dataReferencia=05/03 (5 de marco)
    ///   => fecha=10/03, vence=05/04 (proximo mes, pois 05 <= 10)
    /// - dia_fechamento=10, dia_vencimento=20, dataReferencia=15/03 (15 de marco)
    ///   => fecha=10/04, vence=20/04 (mesmo mes, pois 20 > 10)
    ///
    /// Se ja existe Fatura ABERTA para esse ciclo, reaproveita.
    /// Se nao existe, cria nova com status=ABERTA.
    /// </summary>
    public async Task<Fatura> ResolverFaturaAbertaVigenteAsync(Guid contaId, DateOnly dataReferencia)
    {
        var conta = await _context.Contas
            .FirstOrDefaultAsync(c => c.Id == contaId);

        if (conta == null)
        {
            throw new InvalidOperationException($"Conta {contaId} nao encontrada");
        }

        if (!EhContaCartao(conta))
        {
            throw new InvalidOperationException($"Conta {contaId} nao e do tipo CARTAO");
        }

        if (!conta.DiaFechamento.HasValue || !conta.DiaVencimento.HasValue)
        {
            throw new InvalidOperationException($"Conta CARTAO {contaId} nao tem dia_fechamento ou dia_vencimento configurados");
        }

        var (dataFechamento, dataVencimento) = CalcularDatasCiclo(
            dataReferencia,
            conta.DiaFechamento.Value,
            conta.DiaVencimento.Value);

        var faturaExistente = await _context.Faturas
            .FirstOrDefaultAsync(f =>
                f.ContaId == contaId &&
                f.Status == FaturaStatusConstants.Aberta &&
                f.DataFechamento == dataFechamento);

        if (faturaExistente != null)
        {
            return faturaExistente;
        }

        var novaFatura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Conta = conta,
            DataFechamento = dataFechamento,
            DataVencimento = dataVencimento,
            Status = FaturaStatusConstants.Aberta,
            TransferenciaId = null
        };

        _context.Faturas.Add(novaFatura);
        await _context.SaveChangesAsync();

        return novaFatura;
    }

    /// <summary>
    /// Calcula data_fechamento e data_vencimento de um ciclo.
    /// </summary>
    private static (DateOnly DataFechamento, DateOnly DataVencimento) CalcularDatasCiclo(
        DateOnly dataReferencia,
        int diaFechamento,
        int diaVencimento)
    {
        var dataFechamento = CriarDataValida(dataReferencia.Year, dataReferencia.Month, diaFechamento);

        if (dataReferencia >= dataFechamento)
        {
            var proximoMes = dataReferencia.AddMonths(1);
            dataFechamento = CriarDataValida(proximoMes.Year, proximoMes.Month, diaFechamento);
        }

        var dataVencimento = CriarDataValida(dataFechamento.Year, dataFechamento.Month, diaVencimento);

        if (dataVencimento <= dataFechamento)
        {
            var proximoMes = dataFechamento.AddMonths(1);
            dataVencimento = CriarDataValida(proximoMes.Year, proximoMes.Month, diaVencimento);
        }

        return (dataFechamento, dataVencimento);
    }

    /// <summary>
    /// Cria uma data valida para um mes/ano, ajustando se o dia nao existir (ex: 31/02).
    /// Exemplo: 31/02 vira 28/02 ou 29/02 se bissexto.
    /// </summary>
    private static DateOnly CriarDataValida(int year, int month, int day)
    {
        var diasNoMes = DateTime.DaysInMonth(year, month);
        var diaAjustado = Math.Min(day, diasNoMes);
        return new DateOnly(year, month, diaAjustado);
    }

    private static bool EhContaCartao(Conta conta)
    {
        return conta.Tipo == TipoContaConstants.Cartao;
    }
}
