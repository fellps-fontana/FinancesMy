using MyFinances.DTOs;
using MyFinances.Domain;
using MyFinances.Domain;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class PagamentoFaturaService
{
    private readonly IFaturaRepository _faturaRepository;
    private readonly ITransferenciaRepository _transferenciaRepository;
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly IContaRepository _contaRepository;

    public PagamentoFaturaService(
        IFaturaRepository faturaRepository,
        ITransferenciaRepository transferenciaRepository,
        ILancamentoRepository lancamentoRepository,
        IContaRepository contaRepository)
    {
        _faturaRepository = faturaRepository;
        _transferenciaRepository = transferenciaRepository;
        _lancamentoRepository = lancamentoRepository;
        _contaRepository = contaRepository;
    }

    public async Task<(bool Sucesso, Transferencia? Pagamento, string? Erro)> PagarFaturaAsync(
        Guid faturaId,
        PagarFaturaRequest request)
    {
        if (request.Valor <= 0)
        {
            return (false, null, "Valor do pagamento deve ser maior que zero");
        }

        var fatura = await _faturaRepository.ObterPorId(faturaId);
        if (fatura == null)
        {
            return (false, null, "Fatura nao encontrada");
        }

        if (fatura.Status == StatusFatura.Paga)
        {
            return (false, null, "Fatura ja foi paga");
        }

        var contaOrigem = await _contaRepository.ObterPorId(request.ContaOrigemId);
        if (contaOrigem == null)
        {
            return (false, null, "Conta de origem nao encontrada");
        }

        var saldoFatura = FaturaSaldoCalculator.Calcular(fatura);
        if (request.Valor > saldoFatura.ValorPendente)
        {
            return (false, null, $"Valor de pagamento ({request.Valor}) nao pode exceder o saldo pendente da fatura ({saldoFatura.ValorPendente})");
        }

        var transferencia = new Transferencia
        {
            Id = Guid.NewGuid(),
            Data = request.Data,
            Valor = request.Valor,
            ContaOrigemId = request.ContaOrigemId,
            ContaDestinoId = fatura.ContaId,
            FaturaId = faturaId,
            Descricao = $"Pagamento de fatura - {fatura.DataFechamento} a {fatura.DataVencimento}"
        };

        await _transferenciaRepository.Adicionar(transferencia);

        var lancamentoSaida = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = request.ContaOrigemId,
            Descricao = transferencia.Descricao,
            Valor = request.Valor,
            Tipo = TipoLancamento.Debit,
            Data = request.Data,
            Status = StatusLancamento.Pago,
            Manual = true,
            Oculto = false,
            TransferenciaId = transferencia.Id
        };

        var lancamentoEntrada = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = fatura.ContaId,
            Descricao = transferencia.Descricao,
            Valor = request.Valor,
            Tipo = TipoLancamento.Credit,
            Data = request.Data,
            Status = StatusLancamento.Pago,
            Manual = true,
            Oculto = false,
            TransferenciaId = transferencia.Id
        };

        await _lancamentoRepository.Adicionar(lancamentoSaida);
        await _lancamentoRepository.Adicionar(lancamentoEntrada);

        var novoSaldoPendente = saldoFatura.ValorPendente - request.Valor;
        if (novoSaldoPendente <= 0)
        {
            fatura.Status = StatusFatura.Paga;
        }

        await _faturaRepository.Atualizar(fatura);
        await _lancamentoRepository.Salvar();

        return (true, transferencia, null);
    }
}
