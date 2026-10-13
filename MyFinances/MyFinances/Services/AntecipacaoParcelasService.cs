using MyFinances.Domain;
using MyFinances.Repositories;

namespace MyFinances.Services;

// Orquestracao da antecipacao (item 12). A regra pura vive em AntecipacaoParcelasSelector.
// AnteciparAsync: compra existe e e da conta -> conta Cartao (ValidacaoCartaoService) ->
// existe fatura aberta (senao rejeita, sem criar) -> Selector.Selecionar -> dentro de
// transacao: fatura destino via FaturaCicloService.ResolverFaturaParaLancamentoAsync e
// move as parcelas (Rollback em qualquer falha).
public class AntecipacaoParcelasService : IAntecipacaoParcelasService
{
    private const string DescricaoOperacao = "Antecipacao de parcelas";
    private const string ErroCompraNaoEncontrada = "Compra parcelada nao encontrada";
    private const string ErroCompraOutraConta = "Compra parcelada nao pertence a esta conta";
    private const string ErroSemFaturaAberta = "Nao existe fatura aberta para a conta de cartao";
    private const string ErroFalhaPersistencia = "Falha ao persistir a antecipacao de parcelas";

    private readonly ICompraParceladaRepository _compraParceladaRepository;
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly IFaturaRepository _faturaRepository;
    private readonly FaturaCicloService _faturaCicloService;
    private readonly ValidacaoCartaoService _validacaoCartaoService;

    public AntecipacaoParcelasService(
        ICompraParceladaRepository compraParceladaRepository,
        ILancamentoRepository lancamentoRepository,
        IFaturaRepository faturaRepository,
        FaturaCicloService faturaCicloService,
        ValidacaoCartaoService validacaoCartaoService)
    {
        _compraParceladaRepository = compraParceladaRepository;
        _lancamentoRepository = lancamentoRepository;
        _faturaRepository = faturaRepository;
        _faturaCicloService = faturaCicloService;
        _validacaoCartaoService = validacaoCartaoService;
    }

    public async Task<(bool Sucesso, IReadOnlyList<Lancamento>? Parcelas, string? Erro)> ObterElegiveisAsync(
        Guid contaId,
        Guid compraParceladaId)
    {
        var (contexto, erro) = await CarregarContextoAsync(contaId, compraParceladaId);
        if (contexto == null)
        {
            return (false, null, erro);
        }

        var elegiveis = AntecipacaoParcelasSelector.ObterElegiveis(contexto.Compra.Lancamentos, contexto.FaturaAberta);
        return (true, elegiveis, null);
    }

    public async Task<(bool Sucesso, Fatura? FaturaDestino, IReadOnlyList<Lancamento>? Parcelas, string? Erro)> AnteciparAsync(
        Guid contaId,
        Guid compraParceladaId,
        int quantidade,
        DateOnly dataAntecipacao)
    {
        var (contexto, erro) = await CarregarContextoAsync(contaId, compraParceladaId);
        if (contexto == null)
        {
            return Falha(erro);
        }

        var selecao = AntecipacaoParcelasSelector.Selecionar(contexto.Compra.Lancamentos, contexto.FaturaAberta, quantidade);
        if (!selecao.Sucesso)
        {
            return Falha(selecao.Erro);
        }

        return await MoverParcelasAsync(contaId, selecao.Selecionadas, dataAntecipacao);
    }

    private async Task<(ContextoAntecipacao? Contexto, string? Erro)> CarregarContextoAsync(Guid contaId, Guid compraParceladaId)
    {
        var compra = await _compraParceladaRepository.ObterPorId(compraParceladaId);
        if (compra == null)
        {
            return (null, ErroCompraNaoEncontrada);
        }

        if (compra.Lancamentos.Any(l => l.ContaId != contaId))
        {
            return (null, ErroCompraOutraConta);
        }

        var (valido, _, erroValidacao) = await _validacaoCartaoService.ValidarOperacaoCartaoAsync(
            contaId,
            DescricaoOperacao,
            compra.ValorTotal);
        if (!valido)
        {
            return (null, erroValidacao);
        }

        var faturaAberta = await _faturaRepository.ObterFaturaAbertaPorConta(contaId);
        if (faturaAberta == null)
        {
            return (null, ErroSemFaturaAberta);
        }

        return (new ContextoAntecipacao(compra, faturaAberta), null);
    }

    // Transacao unica: a fatura destino pode ser criada aqui (ciclo novo), entao
    // tambem entra no rollback. Qualquer excecao vira Sucesso = false.
    private async Task<(bool Sucesso, Fatura? FaturaDestino, IReadOnlyList<Lancamento>? Parcelas, string? Erro)> MoverParcelasAsync(
        Guid contaId,
        IReadOnlyList<Lancamento> parcelas,
        DateOnly dataAntecipacao)
    {
        using var transaction = await _compraParceladaRepository.BeginTransactionAsync();
        try
        {
            var (faturaDestino, rejeitada, motivo) = await _faturaCicloService.ResolverFaturaParaLancamentoAsync(
                contaId,
                dataAntecipacao);
            if (rejeitada || faturaDestino == null)
            {
                await _compraParceladaRepository.RollbackAsync();
                return Falha(motivo);
            }

            foreach (var parcela in parcelas)
            {
                MoverParcela(parcela, faturaDestino, dataAntecipacao);
                await _lancamentoRepository.Atualizar(parcela);
            }

            await _lancamentoRepository.Salvar();
            await _compraParceladaRepository.CommitAsync();

            return (true, faturaDestino, parcelas, null);
        }
        catch (Exception)
        {
            // Falha de persistencia nao deve vazar como excecao para o chamador:
            // o rollback garante que nenhuma parcela ficou meio movida.
            await _compraParceladaRepository.RollbackAsync();
            return Falha(ErroFalhaPersistencia);
        }
    }

    private static void MoverParcela(Lancamento parcela, Fatura faturaDestino, DateOnly dataAntecipacao)
    {
        parcela.FaturaId = faturaDestino.Id;
        parcela.Fatura = faturaDestino;
        parcela.Data = dataAntecipacao;
        parcela.Antecipada = true;
    }

    private static (bool Sucesso, Fatura? FaturaDestino, IReadOnlyList<Lancamento>? Parcelas, string? Erro) Falha(string? erro)
    {
        return (false, null, null, erro);
    }

    private sealed record ContextoAntecipacao(CompraParcelada Compra, Fatura FaturaAberta);
}
