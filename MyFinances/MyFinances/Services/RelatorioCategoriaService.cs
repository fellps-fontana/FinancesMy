using MyFinances.Domain;
using MyFinances.DTOs.Relatorio;
using MyFinances.Exceptions;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class RelatorioCategoriaService : IRelatorioCategoriaService
{
    private const int AnoMinimo = 1;
    private const int AnoMaximo = 9999;
    private const int MesMinimo = 1;
    private const int MesMaximo = 12;

    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IPlanejamentoMensalService _planejamentoMensalService;

    public RelatorioCategoriaService(
        ILancamentoRepository lancamentoRepository,
        ICategoriaRepository categoriaRepository,
        IPlanejamentoMensalService planejamentoMensalService)
    {
        _lancamentoRepository = lancamentoRepository;
        _categoriaRepository = categoriaRepository;
        _planejamentoMensalService = planejamentoMensalService;
    }

    // validar ano/mes -> debitos do mes e do anterior (janeiro: dez/ano-1) + todas as
    // categorias -> Calcular nos dois meses -> VariacaoMensalCalculator -> response.
    public async Task<RelatorioGastoPorCategoriaResponse> ObterResumoAsync(int ano, int mes)
    {
        ValidarAnoMes(ano, mes);
        var (anoAnterior, mesAnterior) = CalcularMesAnterior(ano, mes);

        var categorias = await CarregarCategoriasAsync();
        var atual = GastoPorCategoriaCalculator.Calcular(await CarregarDebitosAsync(ano, mes), categorias);
        var anterior = GastoPorCategoriaCalculator.Calcular(
            await CarregarDebitosAsync(anoAnterior, mesAnterior),
            categorias);

        return new RelatorioGastoPorCategoriaResponse(
            ano,
            mes,
            atual.Total,
            anterior.Total,
            VariacaoMensalCalculator.Calcular(atual.Total, anterior.Total),
            atual.Linhas.Select(MapearLinhaResumo).ToList());
    }

    // validar ano/mes -> se categoriaId != null: existe, ParentId == null, Tipo == Despesa
    // -> Calcular (total e percentual da linha) + LancamentosDaLinha -> limite = sublimite da
    // categoria no plano do mes (IPlanejamentoMensalService.ObterAsync; GastoRealizado do sublimite).
    // Categoria valida sem gasto: total 0, lista vazia (nao e erro).
    public async Task<RelatorioCategoriaDetalheResponse> ObterDetalheAsync(int ano, int mes, Guid? categoriaId)
    {
        ValidarAnoMes(ano, mes);

        var categoria = categoriaId.HasValue
            ? await ObterCategoriaDePrimeiroNivelAsync(categoriaId.Value)
            : null;

        var categorias = await CarregarCategoriasAsync();
        var lancamentos = await CarregarDebitosAsync(ano, mes);

        var resultado = GastoPorCategoriaCalculator.Calcular(lancamentos, categorias);
        var linha = resultado.Linhas.SingleOrDefault(item => item.CategoriaId == categoriaId);
        var totalDaLinha = linha?.Valor ?? 0m;

        LimiteRelatorioResponse? limite = categoria == null
            ? null
            : await ObterLimiteRelatorioAsync(ano, mes, categoria.Id);

        var lancamentosDaLinha = GastoPorCategoriaCalculator.LancamentosDaLinha(lancamentos, categorias, categoriaId);

        return new RelatorioCategoriaDetalheResponse(
            categoriaId,
            categoria?.Nome ?? GastoPorCategoriaCalculator.NomeSemCategoria,
            categoria?.Icone,
            totalDaLinha,
            linha?.Percentual ?? 0m,
            limite,
            MapearLancamentos(lancamentosDaLinha, categoriaId, categorias.ToDictionary(item => item.Id)));
    }

    private static void ValidarAnoMes(int ano, int mes)
    {
        if (ano < AnoMinimo || ano > AnoMaximo)
            throw new ValorInvalidoException("ano", ano, AnoMinimo, AnoMaximo);

        if (mes < MesMinimo || mes > MesMaximo)
            throw new ValorInvalidoException("mes", mes, MesMinimo, MesMaximo);
    }

    private static (int Ano, int Mes) CalcularMesAnterior(int ano, int mes) =>
        mes == MesMinimo ? (ano - 1, MesMaximo) : (ano, mes - 1);

    // Arquivadas inclusive: gasto de categoria arquivada entra no mes (item 19).
    private async Task<List<Categoria>> CarregarCategoriasAsync() =>
        (await _categoriaRepository.ListarTodas()).ToList();

    private async Task<List<Lancamento>> CarregarDebitosAsync(int ano, int mes) =>
        (await _lancamentoRepository.ListarDebitosParaRelatorioAsync(ano, mes)).ToList();

    private async Task<Categoria> ObterCategoriaDePrimeiroNivelAsync(Guid categoriaId)
    {
        var categoria = await _categoriaRepository.ObterPorId(categoriaId)
            ?? throw new CategoriaNaoEncontradaException(categoriaId);

        if (categoria.ParentId != null || categoria.Tipo != TipoCategoria.Despesa)
            throw new CategoriaNaoEhPrimeiroNivelException(categoriaId);

        return categoria;
    }

    // Mes alem do horizonte do planejamento nao tem plano: relatorio segue sem limite (decisao 2026-10-14).
    private async Task<LimiteRelatorioResponse?> ObterLimiteRelatorioAsync(int ano, int mes, Guid categoriaId)
    {
        try
        {
            var consulta = await _planejamentoMensalService.ObterAsync(ano, mes);
            var sublimite = consulta?.Plano.Sublimites.FirstOrDefault(item => item.CategoriaId == categoriaId);

            return sublimite == null
                ? null
                : new LimiteRelatorioResponse(sublimite.Valor, sublimite.GastoRealizado, sublimite.Estourado);
        }
        catch (PlanoMensalForaDoHorizonteException)
        {
            return null;
        }
    }

    private static LinhaGastoPorCategoriaResponse MapearLinhaResumo(LinhaGastoCategoria linha) =>
        new(linha.CategoriaId, linha.Nome, linha.Icone, linha.Valor, linha.Percentual);

    private static IReadOnlyList<LancamentoRelatorioResponse> MapearLancamentos(
        IEnumerable<Lancamento> lancamentos,
        Guid? categoriaId,
        IReadOnlyDictionary<Guid, Categoria> categoriasPorId) =>
        lancamentos
            .Select(lancamento => MapearLancamento(lancamento, categoriaId, categoriasPorId))
            .ToList();

    private static LancamentoRelatorioResponse MapearLancamento(
        Lancamento lancamento,
        Guid? categoriaId,
        IReadOnlyDictionary<Guid, Categoria> categoriasPorId) =>
        new(
            lancamento.Id,
            lancamento.Descricao,
            lancamento.Data,
            lancamento.ContaId,
            lancamento.Conta?.Nome ?? string.Empty,
            lancamento.Conta?.Tipo?.ToStorageValue(),
            ObterNomeSubcategoria(lancamento, categoriaId, categoriasPorId),
            lancamento.Valor);

    // Subcategoria so quando o lancamento esta numa categoria diferente da raiz consultada.
    private static string? ObterNomeSubcategoria(
        Lancamento lancamento,
        Guid? categoriaId,
        IReadOnlyDictionary<Guid, Categoria> categoriasPorId)
    {
        if (lancamento.CategoriaId == null || lancamento.CategoriaId == categoriaId)
            return null;

        return categoriasPorId.TryGetValue(lancamento.CategoriaId.Value, out var subcategoria)
            ? subcategoria.Nome
            : null;
    }
}
