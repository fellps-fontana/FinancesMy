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
    ///
    /// Retorna tuple (Fatura, Rejeitada, Motivo):
    /// - Fatura: a fatura resolvida (nao-null se nao rejeitada)
    /// - Rejeitada: true se houver bloqueio, false caso contrario
    /// - Motivo: mensagem de erro se rejeitada, null caso contrario
    /// </summary>
    public async Task<(Fatura? Fatura, bool Rejeitada, string? Motivo)> ResolverFaturaAbertaVigenteAsync(Guid contaId, DateOnly dataReferencia)
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
            return (faturaExistente, false, null);
        }

        var resultado = await CriarOuReutilizarFaturaAbertaAsync(contaId, dataFechamento, dataVencimento, conta);
        return resultado;
    }

    /// <summary>
    /// Resolve a Fatura mais apropriada para registrar um lancamento (compra) em um cartao.
    ///
    /// Regra:
    /// - Se existe Fatura com Status == PAGA: REJEITA (fatura ja foi paga)
    /// - Se existe Fatura com Status == FECHADA ou ABERTA: ACEITA (mesmo retroativa)
    /// - Se nao existe Fatura: tenta criar nova com status ABERTA
    ///   Bloqueio: se houver fatura ABERTA mais recente para a mesma conta,
    ///   rejeita para evitar violacao de constraint unico no banco
    ///
    /// Retorna tuple (Fatura, Rejeitada, Motivo):
    /// - Fatura: a fatura resolvida (nao-null se nao rejeitada)
    /// - Rejeitada: true se fatura PAGA ou bloqueio de fatura ABERTA mais recente
    /// - Motivo: mensagem de erro se rejeitada, null caso contrario
    /// </summary>
    public async Task<(Fatura? Fatura, bool Rejeitada, string? Motivo)> ResolverFaturaParaLancamentoAsync(
        Guid contaId,
        DateOnly data)
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
            data,
            conta.DiaFechamento.Value,
            conta.DiaVencimento.Value);

        // Se data for muito retroativa (anterior a uma fatura ABERTA mais recente ja
        // existente pra mesma conta) e nenhuma fatura existir para esse ciclo, a rejeicao
        // acontece dentro de CriarOuReutilizarFaturaAbertaAsync (regra definitiva: nao ha
        // suporte a compra retroativa alem do ciclo ainda aberto mais antigo).

        var faturaExistente = await _context.Faturas
            .FirstOrDefaultAsync(f =>
                f.ContaId == contaId &&
                f.DataFechamento == dataFechamento);

        if (faturaExistente != null)
        {
            if (faturaExistente.Status == FaturaStatusConstants.Paga)
            {
                return (null, true, "Fatura ja foi paga, nao aceita mais lancamentos");
            }

            return (faturaExistente, false, null);
        }

        var resultado = await CriarOuReutilizarFaturaAbertaAsync(contaId, dataFechamento, dataVencimento, conta);
        return resultado;
    }

    /// <summary>
    /// Cria uma fatura ABERTA novo ciclo ou rejeita se houver bloqueio.
    /// Bloqueia se existir Fatura ABERTA mais recente para a mesma conta,
    /// evitando violacao de constraint unico no banco.
    /// </summary>
    private async Task<(Fatura? Fatura, bool Rejeitada, string? Motivo)> CriarOuReutilizarFaturaAbertaAsync(
        Guid contaId,
        DateOnly novaDataFechamento,
        DateOnly novaDataVencimento,
        Conta conta)
    {
        await FecharFaturaAbertaAnteriorAsync(contaId, novaDataFechamento);

        var faturaAbertaMaisRecente = await _context.Faturas
            .FirstOrDefaultAsync(f =>
                f.ContaId == contaId &&
                f.Status == FaturaStatusConstants.Aberta &&
                f.DataFechamento > novaDataFechamento);

        if (faturaAbertaMaisRecente != null)
        {
            return (null, true, "Nao e possivel criar fatura retroativa: ja existe fatura ABERTA mais recente para esta conta");
        }

        var novaFatura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Conta = conta,
            DataFechamento = novaDataFechamento,
            DataVencimento = novaDataVencimento,
            Status = FaturaStatusConstants.Aberta,
            TransferenciaId = null
        };

        _context.Faturas.Add(novaFatura);
        await _context.SaveChangesAsync();

        return (novaFatura, false, null);
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

    private async Task FecharFaturaAbertaAnteriorAsync(Guid contaId, DateOnly novaDataFechamento)
    {
        var faturaAnterior = await _context.Faturas
            .FirstOrDefaultAsync(f =>
                f.ContaId == contaId &&
                f.Status == FaturaStatusConstants.Aberta &&
                f.DataFechamento < novaDataFechamento);

        if (faturaAnterior != null)
        {
            faturaAnterior.Status = FaturaStatusConstants.Fechada;
        }
    }

    private static bool EhContaCartao(Conta conta)
    {
        return conta.Tipo == TipoContaConstants.Cartao;
    }
}
