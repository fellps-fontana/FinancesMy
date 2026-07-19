using MyFinances.DTOs;
using MyFinances.Domain;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class TransferenciaService : ITransferenciaService
{
    private readonly ITransferenciaRepository _transferenciaRepository;
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly IContaRepository _contaRepository;

    public TransferenciaService(
        ITransferenciaRepository transferenciaRepository,
        ILancamentoRepository lancamentoRepository,
        IContaRepository contaRepository)
    {
        _transferenciaRepository = transferenciaRepository;
        _lancamentoRepository = lancamentoRepository;
        _contaRepository = contaRepository;
    }

    public async Task<(bool Sucesso, Transferencia? Transferencia, string? Erro)> RegistrarTransferenciaManualAsync(
        CriarTransferenciaRequest request)
    {
        var validacao = await ValidarTransferencia(request);
        if (!validacao.Valido)
        {
            return (false, null, validacao.Erro);
        }

        var transferencia = CriarTransferencia(request);
        await _transferenciaRepository.Adicionar(transferencia);

        var lancamentoSaida = CriarLancamentoSaida(transferencia, request);
        var lancamentoEntrada = CriarLancamentoEntrada(transferencia, request);

        await _lancamentoRepository.Adicionar(lancamentoSaida);
        await _lancamentoRepository.Adicionar(lancamentoEntrada);

        await _lancamentoRepository.Salvar();

        return (true, transferencia, null);
    }

    private async Task<(bool Valido, string? Erro)> ValidarTransferencia(CriarTransferenciaRequest request)
    {
        if (request.Valor <= 0)
        {
            return (false, "Valor da transferencia deve ser maior que zero");
        }

        var contaOrigem = await _contaRepository.ObterPorId(request.ContaOrigemId);
        if (contaOrigem == null)
        {
            return (false, "Conta de origem nao encontrada");
        }

        if (!contaOrigem.Ativa)
        {
            return (false, "Conta de origem esta inativa");
        }

        var contaDestino = await _contaRepository.ObterPorId(request.ContaDestinoId);
        if (contaDestino == null)
        {
            return (false, "Conta de destino nao encontrada");
        }

        if (!contaDestino.Ativa)
        {
            return (false, "Conta de destino esta inativa");
        }

        if (request.ContaOrigemId == request.ContaDestinoId)
        {
            return (false, "Conta de origem nao pode ser igual a conta de destino");
        }

        return (true, null);
    }

    private static Transferencia CriarTransferencia(CriarTransferenciaRequest request)
    {
        return new Transferencia
        {
            Id = Guid.NewGuid(),
            Data = request.Data,
            Valor = request.Valor,
            ContaOrigemId = request.ContaOrigemId,
            ContaDestinoId = request.ContaDestinoId,
            Descricao = request.Descricao ?? "Transferencia entre contas"
        };
    }

    private static Lancamento CriarLancamentoSaida(Transferencia transferencia, CriarTransferenciaRequest request)
    {
        return new Lancamento
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
    }

    private static Lancamento CriarLancamentoEntrada(Transferencia transferencia, CriarTransferenciaRequest request)
    {
        return new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = request.ContaDestinoId,
            Descricao = transferencia.Descricao,
            Valor = request.Valor,
            Tipo = TipoLancamento.Credit,
            Data = request.Data,
            Status = StatusLancamento.Pago,
            Manual = true,
            Oculto = false,
            TransferenciaId = transferencia.Id
        };
    }
}
