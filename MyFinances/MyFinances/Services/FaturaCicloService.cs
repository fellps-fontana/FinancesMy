using MyFinances.Models;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class FaturaCicloService
{
    private readonly IFaturaRepository _faturaRepository;
    private readonly IContaRepository _contaRepository;

    public FaturaCicloService(IFaturaRepository faturaRepository, IContaRepository contaRepository)
    {
        _faturaRepository = faturaRepository;
        _contaRepository = contaRepository;
    }

    public async Task<(Fatura? Fatura, bool Rejeitada, string? Motivo)> ResolverFaturaParaLancamentoAsync(
        Guid contaId,
        DateOnly dataLancamento)
    {
        var conta = await _contaRepository.ObterPorId(contaId);
        if (conta == null)
        {
            return (null, true, "Conta nao encontrada");
        }

        var faturaAberta = await _faturaRepository.ObterFaturaAbertaPorConta(contaId);
        if (faturaAberta != null)
        {
            if (dataLancamento < faturaAberta.DataFechamento)
            {
                return (faturaAberta, false, null);
            }

            faturaAberta.Status = StatusFatura.Fechada;
            await _faturaRepository.Atualizar(faturaAberta);
        }

        var (novaDataFechamento, novaDataVencimento) = CalcularDatasCiclo(
            conta.DiaFechamento ?? 1,
            conta.DiaVencimento ?? 1,
            dataLancamento);

        var novaFatura = new Fatura
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            DataFechamento = novaDataFechamento,
            DataVencimento = novaDataVencimento,
            Status = StatusFatura.Aberta
        };

        await _faturaRepository.Adicionar(novaFatura);
        await _faturaRepository.Salvar();

        return (novaFatura, false, null);
    }

    public (DateOnly DataFechamento, DateOnly DataVencimento) CalcularDatasCiclo(
        int diaFechamento,
        int diaVencimento,
        DateOnly dataLancamento)
    {
        var dataFechamento = CriarDataValida(dataLancamento.Year, dataLancamento.Month, diaFechamento);

        if (dataLancamento > dataFechamento)
        {
            dataFechamento = dataFechamento.AddMonths(1);
        }

        var mesVencimento = dataFechamento.Month;
        var anoVencimento = dataFechamento.Year;
        var dataVencimento = CriarDataValida(anoVencimento, mesVencimento, diaVencimento);

        if (dataVencimento <= dataFechamento)
        {
            var proximoMes = dataFechamento.AddMonths(1);
            dataVencimento = CriarDataValida(proximoMes.Year, proximoMes.Month, diaVencimento);
        }

        return (dataFechamento, dataVencimento);
    }

    private static DateOnly CriarDataValida(int ano, int mes, int dia)
    {
        var diasNoMes = DateTime.DaysInMonth(ano, mes);
        var diaAjustado = Math.Min(dia, diasNoMes);
        return new DateOnly(ano, mes, diaAjustado);
    }
}
