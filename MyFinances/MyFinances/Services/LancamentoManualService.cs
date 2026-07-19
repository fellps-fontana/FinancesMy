using MyFinances.DTOs;
using MyFinances.Domain;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class LancamentoManualService : ILancamentoManualService
{
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly IContaRepository _contaRepository;

    public LancamentoManualService(
        ILancamentoRepository lancamentoRepository,
        IContaRepository contaRepository)
    {
        _lancamentoRepository = lancamentoRepository;
        _contaRepository = contaRepository;
    }

    public async Task<(bool Sucesso, LancamentoResponseDto? Lancamento, string? Erro)> CriarAsync(
        Guid contaId,
        CriarLancamentoRequest request)
    {
        var validacao = await ValidarContaAtiva(contaId);
        if (!validacao.Valido)
        {
            return (false, null, validacao.Erro);
        }

        var validacaoDados = ValidarDadosLancamento(request.Descricao, request.Valor);
        if (!validacaoDados.Valido)
        {
            return (false, null, validacaoDados.Erro);
        }

        var statusParsed = TentarParsearStatus(request.Status);
        if (statusParsed.Status == null)
        {
            return (false, null, statusParsed.Erro);
        }

        if (statusParsed.Status == StatusLancamento.Sugerido)
        {
            return (false, null, "Status SUGERIDO nao e permitido para lancamentos manuais");
        }

        var tipoParsed = TentarParsearTipo(request.Tipo);
        if (tipoParsed.Tipo == null)
        {
            return (false, null, tipoParsed.Erro);
        }

        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaId,
            Conta = validacao.Conta!,
            CategoriaId = request.CategoriaId,
            Descricao = request.Descricao,
            Valor = request.Valor,
            Tipo = tipoParsed.Tipo.Value,
            Data = request.Data,
            Status = statusParsed.Status.Value,
            Manual = true,
            Oculto = false,
            PierreTxnId = null,
            FaturaId = null,
            TransferenciaId = null,
            ConciliadoCom = null,
            ContaFixaId = null
        };

        await _lancamentoRepository.Adicionar(lancamento);
        await _lancamentoRepository.Salvar();

        return (true, LancamentoResponseDto.FromLancamento(lancamento), null);
    }

    public async Task<(bool Sucesso, LancamentoResponseDto? Lancamento, string? Erro)> EditarAsync(
        Guid contaId,
        Guid lancamentoId,
        EditarLancamentoRequest request)
    {
        var lancamento = await _lancamentoRepository.ObterPorId(lancamentoId);

        if (lancamento == null || lancamento.ContaId != contaId)
        {
            return (false, null, "Lancamento nao encontrado");
        }

        var validacaoDados = ValidarDadosLancamento(request.Descricao, request.Valor);
        if (!validacaoDados.Valido)
        {
            return (false, null, validacaoDados.Erro);
        }

        var tipoParsed = TentarParsearTipo(request.Tipo);
        if (tipoParsed.Tipo == null)
        {
            return (false, null, tipoParsed.Erro);
        }

        lancamento.Descricao = request.Descricao;
        lancamento.Valor = request.Valor;
        lancamento.Tipo = tipoParsed.Tipo.Value;
        lancamento.Data = request.Data;
        lancamento.CategoriaId = request.CategoriaId;

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var statusParsed = TentarParsearStatus(request.Status);
            if (statusParsed.Status == null)
            {
                return (false, null, statusParsed.Erro);
            }

            if (statusParsed.Status == StatusLancamento.Sugerido)
            {
                return (false, null, "Status SUGERIDO nao e permitido para lancamentos manuais");
            }

            lancamento.Status = statusParsed.Status.Value;
        }

        await _lancamentoRepository.Atualizar(lancamento);
        await _lancamentoRepository.Salvar();

        return (true, LancamentoResponseDto.FromLancamento(lancamento), null);
    }

    public async Task<(bool Sucesso, string? Erro)> MarcarComoPagoAsync(
        Guid contaId,
        Guid lancamentoId)
    {
        var lancamento = await _lancamentoRepository.ObterPorId(lancamentoId);

        if (lancamento == null || lancamento.ContaId != contaId)
        {
            return (false, "Lancamento nao encontrado");
        }

        if (lancamento.Status != StatusLancamento.Pendente)
        {
            return (false, "Apenas lancamentos PENDENTE podem ser marcados como PAGO");
        }

        lancamento.Status = StatusLancamento.Pago;

        await _lancamentoRepository.Atualizar(lancamento);
        await _lancamentoRepository.Salvar();

        return (true, null);
    }

    public async Task<(bool Sucesso, string? Erro)> RemoverAsync(
        Guid contaId,
        Guid lancamentoId)
    {
        var lancamento = await _lancamentoRepository.ObterPorId(lancamentoId);

        if (lancamento == null || lancamento.ContaId != contaId)
        {
            return (false, "Lancamento nao encontrado");
        }

        await _lancamentoRepository.Remover(lancamento);
        await _lancamentoRepository.Salvar();

        return (true, null);
    }

    private async Task<(bool Valido, Conta? Conta, string? Erro)> ValidarContaAtiva(Guid contaId)
    {
        var conta = await _contaRepository.ObterPorId(contaId);
        if (conta == null)
        {
            return (false, null, "Conta nao encontrada");
        }

        if (!conta.Ativa)
        {
            return (false, null, "Conta inativa nao pode ser utilizada");
        }

        return (true, conta, null);
    }

    private static (bool Valido, string? Erro) ValidarDadosLancamento(string? descricao, decimal valor)
    {
        if (string.IsNullOrWhiteSpace(descricao))
        {
            return (false, "Descricao do lancamento e obrigatoria");
        }

        if (valor <= 0)
        {
            return (false, "Valor do lancamento deve ser maior que zero");
        }

        return (true, null);
    }

    private static (StatusLancamento? Status, string? Erro) TentarParsearStatus(string status)
    {
        try
        {
            var parsed = StatusLancamentoExtensions.FromStorageValue(status);
            return (parsed, null);
        }
        catch
        {
            return (null, $"Status invalido: {status}. Use PENDENTE, SUGERIDO ou PAGO");
        }
    }

    private static (TipoLancamento? Tipo, string? Erro) TentarParsearTipo(string tipo)
    {
        try
        {
            var parsed = TipoLancamentoExtensions.FromStorageValue(tipo);
            return (parsed, null);
        }
        catch
        {
            return (null, $"Tipo invalido: {tipo}. Use DEBIT ou CREDIT");
        }
    }
}
