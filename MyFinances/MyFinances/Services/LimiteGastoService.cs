using MyFinances.Domain;
using MyFinances.Exceptions;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class LimiteGastoService : ILimiteGastoService
{
    private readonly ILimiteGastoRepository _limiteGastoRepository;
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly ILancamentoRepository _lancamentoRepository;

    public LimiteGastoService(
        ILimiteGastoRepository limiteGastoRepository,
        ICategoriaRepository categoriaRepository,
        ILancamentoRepository lancamentoRepository)
    {
        _limiteGastoRepository = limiteGastoRepository;
        _categoriaRepository = categoriaRepository;
        _lancamentoRepository = lancamentoRepository;
    }

    public async Task<LimiteGasto> Definir(Guid categoriaId, decimal valorLimite)
    {
        ValidarValor(valorLimite);

        var categoria = await _categoriaRepository.ObterPorId(categoriaId);
        if (categoria == null)
            throw new CategoriaNaoEncontradaException(categoriaId);

        ValidarCategoria(categoria, categoriaId);

        var limiteExistente = await _limiteGastoRepository.ObterPorCategoriaId(categoriaId);

        if (limiteExistente != null)
        {
            limiteExistente.ValorLimite = valorLimite;
            await _limiteGastoRepository.Salvar();
            return limiteExistente;
        }

        var novoLimite = new LimiteGasto
        {
            Id = Guid.NewGuid(),
            CategoriaId = categoriaId,
            ValorLimite = valorLimite,
            Periodo = PeriodoLimiteGasto.Mensal,
            Categoria = categoria
        };

        await _limiteGastoRepository.Adicionar(novoLimite);
        await _limiteGastoRepository.Salvar();

        return novoLimite;
    }

    public async Task Remover(Guid categoriaId)
    {
        var limite = await _limiteGastoRepository.ObterPorCategoriaId(categoriaId);
        if (limite == null)
            throw new LimiteGastoNaoEncontradoException(categoriaId);

        await _limiteGastoRepository.Remover(limite);
        await _limiteGastoRepository.Salvar();
    }

    public async Task<IEnumerable<LimiteGasto>> Listar()
    {
        return await _limiteGastoRepository.Listar();
    }

    public async Task<LimiteGastoStatus> ObterGastoVsLimite(Guid categoriaId, int ano, int mes)
    {
        var limite = await _limiteGastoRepository.ObterPorCategoriaId(categoriaId);
        if (limite == null)
            throw new LimiteGastoNaoEncontradoException(categoriaId);

        var categoria = await _categoriaRepository.ObterPorId(categoriaId);
        if (categoria == null)
            throw new CategoriaNaoEncontradaException(categoriaId);

        var categoriaIds = MontarListaDeCategorias(categoria);

        var lancamentos = await _lancamentoRepository.ListarPorCategoriasEPeriodo(categoriaIds, ano, mes);

        return LimiteGastoCalculator.Calcular(limite, lancamentos);
    }

    public async Task<IEnumerable<(LimiteGasto LimiteGasto, LimiteGastoStatus Status)>> ObterGastoVsLimiteTodasCategorias(int ano, int mes)
    {
        var limites = await _limiteGastoRepository.Listar();

        var resultados = new List<(LimiteGasto, LimiteGastoStatus)>();

        foreach (var limite in limites)
        {
            var status = await ObterGastoVsLimite(limite.CategoriaId, ano, mes);
            resultados.Add((limite, status));
        }

        return resultados;
    }

    private static void ValidarValor(decimal valorLimite)
    {
        if (valorLimite <= 0)
            throw new ValorInvalidoException("valorLimite", valorLimite);
    }

    private static void ValidarCategoria(Categoria categoria, Guid categoriaId)
    {
        if (categoria.Tipo != TipoCategoria.Despesa || categoria.Arquivada)
            throw new CategoriaInvalidaParaLimiteGastoException(categoriaId);
    }

    private static IEnumerable<Guid> MontarListaDeCategorias(Categoria categoria)
    {
        var ids = new List<Guid> { categoria.Id };

        foreach (var subcategoria in categoria.Subcategorias)
        {
            ids.Add(subcategoria.Id);
        }

        return ids;
    }
}
