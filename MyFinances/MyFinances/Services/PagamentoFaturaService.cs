using MyFinances.DTOs;
using MyFinances.Domain;
using MyFinances.Models;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class PagamentoFaturaService
{
    private readonly IFaturaRepository _faturaRepository;
    private readonly ITransferenciaRepository _transferenciaRepository;
    private readonly IContaRepository _contaRepository;

    public PagamentoFaturaService(
        IFaturaRepository faturaRepository,
        ITransferenciaRepository transferenciaRepository,
        IContaRepository contaRepository)
    {
        _faturaRepository = faturaRepository;
        _transferenciaRepository = transferenciaRepository;
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

        var novoSaldoPendente = saldoFatura.ValorPendente - request.Valor;
        if (novoSaldoPendente <= 0)
        {
            fatura.Status = StatusFatura.Paga;
        }

        await _faturaRepository.Atualizar(fatura);
        await _transferenciaRepository.Salvar();

        return (true, transferencia, null);
    }
}
