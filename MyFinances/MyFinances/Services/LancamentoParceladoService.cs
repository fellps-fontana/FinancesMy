using MyFinances.Domain;
using MyFinances.DTOs;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class LancamentoParceladoService : ILancamentoParceladoService
{
    // Controller mapeia esta mensagem para 404.
    public const string ErroCompraNaoEncontrada = "Compra parcelada nao encontrada";

    private const string ErroQuantidadeInvalida = "Quantidade de parcelas deve ser no minimo 2";
    private const string ErroCompraDeCartao = "Compra de cartao nao pode ser cancelada aqui; use a fatura do cartao";
    private const string ErroSemParcelaPendente = "Nenhuma parcela PENDENTE para cancelar";
    private const int QuantidadeMinimaParcelas = 2;

    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly ICompraParceladaRepository _compraParceladaRepository;
    private readonly ValidadorLancamentoManual _validador;

    public LancamentoParceladoService(
        ILancamentoRepository lancamentoRepository,
        ICompraParceladaRepository compraParceladaRepository,
        IContaRepository contaRepository,
        ICategoriaRepository categoriaRepository)
    {
        _lancamentoRepository = lancamentoRepository;
        _compraParceladaRepository = compraParceladaRepository;
        _validador = new ValidadorLancamentoManual(contaRepository, categoriaRepository);
    }

    public async Task<(bool Sucesso, LancamentoParceladoResponse? Resposta, string? Erro)> CriarAsync(
        Guid contaId,
        CriarLancamentoParceladoRequest request)
    {
        var (dados, erro) = await ValidarCriacaoAsync(contaId, request);
        if (dados == null)
        {
            return (false, null, erro);
        }

        var compra = new CompraParcelada
        {
            Id = Guid.NewGuid(),
            Descricao = request.Descricao,
            ValorTotal = request.ValorTotal,
            QuantidadeParcelas = request.QuantidadeParcelas,
            DataCompra = request.DataPrimeiraParcela
        };
        compra.Lancamentos = MontarParcelas(compra, dados, request);

        await GravarCompraAsync(compra);

        return (true, MontarResposta(compra), null);
    }

    public async Task<(bool Sucesso, int Excluidas, int Mantidas, string? Erro)> CancelarAsync(
        Guid contaId,
        Guid compraParceladaId)
    {
        var compra = await _compraParceladaRepository.ObterPorId(compraParceladaId);
        if (compra == null || !PertenceAConta(compra, contaId))
        {
            return (false, 0, 0, ErroCompraNaoEncontrada);
        }

        if (EhCompraDeCartao(compra))
        {
            return (false, 0, 0, ErroCompraDeCartao);
        }

        var pendentes = compra.Lancamentos.Where(EhPendente).ToList();
        if (pendentes.Count == 0)
        {
            return (false, 0, 0, ErroSemParcelaPendente);
        }

        foreach (var parcela in pendentes)
        {
            await _lancamentoRepository.Remover(parcela);
        }
        await _lancamentoRepository.Salvar();

        var mantidas = compra.Lancamentos.Count - pendentes.Count;
        return (true, pendentes.Count, mantidas, null);
    }

    private async Task<(DadosCriacaoValidados? Dados, string? Erro)> ValidarCriacaoAsync(
        Guid contaId,
        CriarLancamentoParceladoRequest request)
    {
        if (request.QuantidadeParcelas < QuantidadeMinimaParcelas)
        {
            return (null, ErroQuantidadeInvalida);
        }

        var validacaoDados = ValidadorLancamentoManual.ValidarDadosLancamento(request.Descricao, request.ValorTotal);
        if (!validacaoDados.Valido)
        {
            return (null, validacaoDados.Erro);
        }

        var status = ValidadorLancamentoManual.ValidarStatusManual(request.Status);
        if (status.Status == null)
        {
            return (null, status.Erro);
        }

        var conta = await _validador.ValidarContaDeLancamentoManualAsync(contaId);
        if (!conta.Valido)
        {
            return (null, conta.Erro);
        }

        var categoria = await _validador.ValidarCategoriaDoTipoAsync(request.CategoriaId, TipoLancamento.Debit);
        if (!categoria.Valido)
        {
            return (null, categoria.Erro);
        }

        return (new DadosCriacaoValidados(conta.Conta!, status.Status.Value), null);
    }

    // Valores via ParcelamentoCalculator (resto na ultima), datas via
    // ParcelamentoContaBancoDatas. Parcela 1 com o status do formulario, demais PENDENTE.
    private static List<Lancamento> MontarParcelas(
        CompraParcelada compra,
        DadosCriacaoValidados dados,
        CriarLancamentoParceladoRequest request)
    {
        var valores = ParcelamentoCalculator.CalcularValoresParcelas(request.ValorTotal, request.QuantidadeParcelas);
        var datas = ParcelamentoContaBancoDatas.Calcular(request.DataPrimeiraParcela, request.QuantidadeParcelas);

        var parcelas = new List<Lancamento>();
        for (var indice = 0; indice < request.QuantidadeParcelas; indice++)
        {
            parcelas.Add(new Lancamento
            {
                Id = Guid.NewGuid(),
                ContaId = dados.Conta.Id,
                Conta = dados.Conta,
                CategoriaId = request.CategoriaId,
                Descricao = request.Descricao,
                Valor = valores[indice],
                Tipo = TipoLancamento.Debit,
                Data = datas[indice],
                Status = ObterStatusParcela(indice, dados.StatusPrimeiraParcela),
                Manual = true,
                Oculto = false,
                FaturaId = null,
                CompraParceladaId = compra.Id,
                CompraParcelada = compra,
                ParcelaNumero = indice + 1
            });
        }

        return parcelas;
    }

    private static StatusLancamento ObterStatusParcela(int indice, StatusLancamento statusPrimeiraParcela)
    {
        return indice == 0 ? statusPrimeiraParcela : StatusLancamento.Pendente;
    }

    private async Task GravarCompraAsync(CompraParcelada compra)
    {
        using var transaction = await _compraParceladaRepository.BeginTransactionAsync();
        try
        {
            await _compraParceladaRepository.Adicionar(compra);

            foreach (var parcela in compra.Lancamentos)
            {
                await _lancamentoRepository.Adicionar(parcela);
            }

            await _compraParceladaRepository.Salvar();
            await _compraParceladaRepository.CommitAsync();
        }
        catch
        {
            await _compraParceladaRepository.RollbackAsync();
            throw;
        }
    }

    private static LancamentoParceladoResponse MontarResposta(CompraParcelada compra)
    {
        return new LancamentoParceladoResponse
        {
            CompraParceladaId = compra.Id,
            Parcelas = compra.Lancamentos
                .OrderBy(l => l.ParcelaNumero)
                .Select(LancamentoResponseDto.FromLancamento)
                .ToList()
        };
    }

    // Compra pertence a conta quando tem parcelas e todas sao da conta.
    private static bool PertenceAConta(CompraParcelada compra, Guid contaId)
    {
        return compra.Lancamentos.Count > 0 && compra.Lancamentos.All(l => l.ContaId == contaId);
    }

    private static bool EhCompraDeCartao(CompraParcelada compra)
    {
        return compra.Lancamentos.Any(l => l.FaturaId != null);
    }

    private static bool EhPendente(Lancamento parcela)
    {
        return parcela.Status == StatusLancamento.Pendente;
    }

    private sealed record DadosCriacaoValidados(Conta Conta, StatusLancamento StatusPrimeiraParcela);
}
