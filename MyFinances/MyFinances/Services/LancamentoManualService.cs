using MyFinances.DTOs;
using MyFinances.Domain;
using MyFinances.Exceptions;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class LancamentoManualService : ILancamentoManualService
{
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly ValidadorLancamentoManual _validador;

    public LancamentoManualService(
        ILancamentoRepository lancamentoRepository,
        IContaRepository contaRepository,
        ICategoriaRepository categoriaRepository)
    {
        _lancamentoRepository = lancamentoRepository;
        _validador = new ValidadorLancamentoManual(contaRepository, categoriaRepository);
    }

    public async Task<(bool Sucesso, LancamentoResponseDto? Lancamento, string? Erro)> CriarAsync(
        Guid contaId,
        CriarLancamentoRequest request)
    {
        var validacao = await _validador.ValidarContaDeLancamentoManualAsync(contaId);
        if (!validacao.Valido)
        {
            return (false, null, validacao.Erro);
        }

        var validacaoDados = ValidadorLancamentoManual.ValidarDadosLancamento(request.Descricao, request.Valor);
        if (!validacaoDados.Valido)
        {
            return (false, null, validacaoDados.Erro);
        }

        var statusParsed = ValidadorLancamentoManual.ValidarStatusManual(request.Status);
        if (statusParsed.Status == null)
        {
            return (false, null, statusParsed.Erro);
        }

        var tipoParsed = ValidadorLancamentoManual.TentarParsearTipo(request.Tipo);
        if (tipoParsed.Tipo == null)
        {
            return (false, null, tipoParsed.Erro);
        }

        var validacaoCategoria = await _validador.ValidarCategoriaDoTipoAsync(request.CategoriaId, tipoParsed.Tipo.Value);
        if (!validacaoCategoria.Valido)
        {
            return (false, null, validacaoCategoria.Erro);
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

        GarantirAvulso(lancamento);

        if (lancamento.Conta != null && !lancamento.Conta.Ativa)
        {
            return (false, null, ValidadorLancamentoManual.MensagemContaInativa);
        }

        var (dados, erro) = await ValidarEdicao(lancamento, request);
        if (dados == null)
        {
            return (false, null, erro);
        }

        AplicarEdicao(lancamento, request, dados);

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

        if (lancamento.Conta != null && !lancamento.Conta.Ativa)
        {
            return (false, ValidadorLancamentoManual.MensagemContaInativa);
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

        GarantirAvulso(lancamento);

        await _lancamentoRepository.Remover(lancamento);
        await _lancamentoRepository.Salvar();

        return (true, null);
    }

    // Regra-de-negocio item 18: so avulso e editado ou excluido aqui.
    private static void GarantirAvulso(Lancamento lancamento)
    {
        var origem = LancamentoVinculoResolver.ResolverOrigem(lancamento);
        if (origem != OrigemVinculoLancamento.Avulso)
        {
            throw new LancamentoNaoAvulsoException(origem);
        }
    }

    private async Task<(DadosEdicaoValidados? Dados, string? Erro)> ValidarEdicao(
        Lancamento lancamento,
        EditarLancamentoRequest request)
    {
        var validacaoDados = ValidadorLancamentoManual.ValidarDadosLancamento(request.Descricao, request.Valor);
        if (!validacaoDados.Valido)
        {
            return (null, validacaoDados.Erro);
        }

        var tipoParsed = ValidadorLancamentoManual.TentarParsearTipo(request.Tipo);
        if (tipoParsed.Tipo == null)
        {
            return (null, tipoParsed.Erro);
        }

        var status = ValidadorLancamentoManual.ValidarStatusOpcional(request.Status);
        if (status.Erro != null)
        {
            return (null, status.Erro);
        }

        var validacaoCategoria = await _validador.ValidarCategoriaDoTipoAsync(request.CategoriaId, tipoParsed.Tipo.Value);
        if (!validacaoCategoria.Valido)
        {
            return (null, validacaoCategoria.Erro);
        }

        var contaDestino = await ValidarTrocaDeConta(lancamento, request.ContaId);
        if (!contaDestino.Valido)
        {
            return (null, contaDestino.Erro);
        }

        return (new DadosEdicaoValidados(tipoParsed.Tipo.Value, status.Status, contaDestino.Conta), null);
    }

    private static void AplicarEdicao(Lancamento lancamento, EditarLancamentoRequest request, DadosEdicaoValidados dados)
    {
        lancamento.Descricao = request.Descricao;
        lancamento.Valor = request.Valor;
        lancamento.Tipo = dados.Tipo;
        lancamento.Data = request.Data;
        lancamento.CategoriaId = request.CategoriaId;

        if (dados.Status != null)
        {
            lancamento.Status = dados.Status.Value;
        }

        if (dados.ContaDestino != null)
        {
            lancamento.ContaId = dados.ContaDestino.Id;
            lancamento.Conta = dados.ContaDestino;
        }
    }

    // Conta omitida ou igual a atual = mantem a conta (Conta null, sem validacao).
    private async Task<(bool Valido, Conta? Conta, string? Erro)> ValidarTrocaDeConta(
        Lancamento lancamento,
        Guid? contaDestinoId)
    {
        if (contaDestinoId == null || contaDestinoId.Value == lancamento.ContaId)
        {
            return (true, null, null);
        }

        return await _validador.ValidarContaDeLancamentoManualAsync(contaDestinoId.Value);
    }

    private sealed record DadosEdicaoValidados(TipoLancamento Tipo, StatusLancamento? Status, Conta? ContaDestino);
}
