using MyFinances.Domain;
using MyFinances.Exceptions;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class PlanejamentoMensalService : IPlanejamentoMensalService
{
    private const int MesMinimo = 1;
    private const int MesMaximo = 12;
    private const int AnoMinimo = 1;
    // Limite tecnico do DateOnly (usado pela PlanoMensalInicialFactory).
    private const int AnoMaximo = 9999;
    // Horizonte: mes corrente + 12 inclusive. Alem disso nao ha consulta nem gravacao.
    private const int MesesFuturosPermitidos = 12;
    private const int MesesPorAno = 12;

    private readonly IPlanoMensalRepository _planoMensalRepository;
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly IRecebivelRecorrenteRepository _recebivelRecorrenteRepository;
    private readonly TimeProvider _timeProvider;

    public PlanejamentoMensalService(
        IPlanoMensalRepository planoMensalRepository,
        ICategoriaRepository categoriaRepository,
        ILancamentoRepository lancamentoRepository,
        IRecebivelRecorrenteRepository recebivelRecorrenteRepository,
        TimeProvider timeProvider)
    {
        _planoMensalRepository = planoMensalRepository;
        _categoriaRepository = categoriaRepository;
        _lancamentoRepository = lancamentoRepository;
        _recebivelRecorrenteRepository = recebivelRecorrenteRepository;
        _timeProvider = timeProvider;
    }

    public async Task<PlanoMensalConsulta?> ObterAsync(int ano, int mes)
    {
        ValidarAnoMes(ano, mes);

        var hoje = HojeLocal();
        ValidarHorizonte(ano, mes, hoje);
        var somenteLeitura = EhMesPassado(ano, mes, hoje);
        var plano = await _planoMensalRepository.ObterPorAnoMes(ano, mes);

        if (plano is not null)
        {
            return await CalcularConsultaAsync(plano, somenteLeitura);
        }

        if (somenteLeitura)
        {
            return null;
        }

        var gerado = await GerarPlanoSobDemandaAsync(ano, mes);
        return await CalcularConsultaAsync(gerado, somenteLeitura);
    }

    public async Task<PlanoMensalConsulta> SalvarAsync(
        int ano,
        int mes,
        decimal rendaPrevista,
        decimal percentualInvestimento,
        IReadOnlyList<SublimiteEntrada> sublimites)
    {
        ValidarAnoMes(ano, mes);

        var hoje = HojeLocal();
        ValidarHorizonte(ano, mes, hoje);

        if (EhMesPassado(ano, mes, hoje))
        {
            throw new PlanoMensalPassadoSomenteLeituraException(ano, mes);
        }

        var categoriasPorId = await ObterCategoriasPorIdAsync();
        PlanoMensalValidator.Validar(rendaPrevista, percentualInvestimento, sublimites, categoriasPorId);

        var plano = await ObterOuCriarParaSalvarAsync(ano, mes, rendaPrevista, percentualInvestimento, sublimites);
        await AplicarAlteracoesAsync(plano, rendaPrevista, percentualInvestimento, sublimites);
        await _planoMensalRepository.Salvar();

        return await CalcularConsultaAsync(plano, somenteLeitura: false);
    }

    // Mes corrente ou futuro sem plano: renda pelos moldes ativos, % e sublimites do plano anterior.
    private async Task<PlanoMensal> GerarPlanoSobDemandaAsync(int ano, int mes)
    {
        var moldes = await _recebivelRecorrenteRepository.ListarAtivos();
        var rendaPrevista = PlanoMensalInicialFactory.CalcularRendaPrevistaInicial(moldes, ano, mes);
        var planoAnterior = await _planoMensalRepository.ObterMaisRecenteAnteriorA(ano, mes);
        var categoriasPorId = await ObterCategoriasPorIdAsync();

        var novo = PlanoMensalInicialFactory.Criar(ano, mes, rendaPrevista, planoAnterior, categoriasPorId);
        return await _planoMensalRepository.AdicionarOuObterExistente(novo);
    }

    // Plano sem gravacao no mes corrente ou futuro nasce com os valores enviados (sem copia do anterior).
    private async Task<PlanoMensal> ObterOuCriarParaSalvarAsync(
        int ano,
        int mes,
        decimal rendaPrevista,
        decimal percentualInvestimento,
        IReadOnlyList<SublimiteEntrada> sublimites)
    {
        var existente = await _planoMensalRepository.ObterPorAnoMes(ano, mes);
        if (existente is not null)
        {
            return existente;
        }

        var novo = MontarPlanoNovo(ano, mes, rendaPrevista, percentualInvestimento, sublimites);
        return await _planoMensalRepository.AdicionarOuObterExistente(novo);
    }

    private static PlanoMensal MontarPlanoNovo(
        int ano,
        int mes,
        decimal rendaPrevista,
        decimal percentualInvestimento,
        IReadOnlyList<SublimiteEntrada> sublimites)
    {
        var plano = new PlanoMensal
        {
            Id = Guid.NewGuid(),
            Ano = ano,
            Mes = mes,
            RendaPrevista = rendaPrevista,
            PercentualInvestimento = percentualInvestimento
        };

        plano.Sublimites = sublimites.Select(entrada => MontarSublimite(plano.Id, entrada)).ToList();
        return plano;
    }

    // Diff in-place: remove ausentes, atualiza existentes e adiciona novos. Idempotente sobre o mesmo conjunto.
    private async Task AplicarAlteracoesAsync(
        PlanoMensal plano,
        decimal rendaPrevista,
        decimal percentualInvestimento,
        IReadOnlyList<SublimiteEntrada> sublimites)
    {
        plano.RendaPrevista = rendaPrevista;
        plano.PercentualInvestimento = percentualInvestimento;

        await RemoverSublimitesAusentesAsync(plano, sublimites);
        await AtualizarOuAdicionarSublimitesAsync(plano, sublimites);
    }

    private async Task RemoverSublimitesAusentesAsync(PlanoMensal plano, IReadOnlyList<SublimiteEntrada> sublimites)
    {
        var ausentes = plano.Sublimites
            .Where(existente => !sublimites.Any(entrada => entrada.CategoriaId == existente.CategoriaId))
            .ToList();

        foreach (var sublimite in ausentes)
        {
            await _planoMensalRepository.RemoverSublimite(sublimite);
            plano.Sublimites.Remove(sublimite);
        }
    }

    // Sublimite novo vai por AdicionarSublimite (Add explicito). Sem isso, o EF toma a entidade
    // descoberta pela navegacao, com Id Guid ja preenchido, por existente e gera UPDATE.
    private async Task AtualizarOuAdicionarSublimitesAsync(PlanoMensal plano, IReadOnlyList<SublimiteEntrada> sublimites)
    {
        foreach (var entrada in sublimites)
        {
            var existente = plano.Sublimites.FirstOrDefault(s => s.CategoriaId == entrada.CategoriaId);

            if (existente is not null)
            {
                existente.Percentual = entrada.Percentual;
                continue;
            }

            var novo = MontarSublimite(plano.Id, entrada);
            plano.Sublimites.Add(novo);
            await _planoMensalRepository.AdicionarSublimite(novo);
        }
    }

    private static PlanoMensalSublimite MontarSublimite(Guid planoId, SublimiteEntrada entrada) => new()
    {
        Id = Guid.NewGuid(),
        PlanoMensalId = planoId,
        CategoriaId = entrada.CategoriaId,
        Percentual = entrada.Percentual
    };

    private async Task<PlanoMensalConsulta> CalcularConsultaAsync(PlanoMensal plano, bool somenteLeitura)
    {
        var debitos = await _lancamentoRepository.ListarDebitosParaRelatorioAsync(plano.Ano, plano.Mes);
        var categorias = await _categoriaRepository.ListarTodas();
        var calculado = PlanejamentoMensalCalculator.Calcular(plano, debitos, categorias);

        return new PlanoMensalConsulta(calculado, somenteLeitura);
    }

    private async Task<Dictionary<Guid, Categoria>> ObterCategoriasPorIdAsync()
    {
        var categorias = await _categoriaRepository.ListarTodas();
        return categorias.ToDictionary(categoria => categoria.Id);
    }

    private DateOnly HojeLocal() => DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);

    // Mes passado: ano/mes anterior ao corrente. Mes corrente e futuro seguem editaveis.
    private static bool EhMesPassado(int ano, int mes, DateOnly hoje) =>
        ano < hoje.Year || (ano == hoje.Year && mes < hoje.Month);

    private static void ValidarHorizonte(int ano, int mes, DateOnly hoje)
    {
        var mesesAFrente = IndiceDoMes(ano, mes) - IndiceDoMes(hoje.Year, hoje.Month);

        if (mesesAFrente > MesesFuturosPermitidos)
        {
            throw new PlanoMensalForaDoHorizonteException(ano, mes, MesesFuturosPermitidos);
        }
    }

    private static int IndiceDoMes(int ano, int mes) => ano * MesesPorAno + mes;

    private static void ValidarAnoMes(int ano, int mes)
    {
        if (mes < MesMinimo || mes > MesMaximo)
        {
            throw new ValorInvalidoException(nameof(mes), mes, MesMinimo, MesMaximo);
        }

        if (ano < AnoMinimo || ano > AnoMaximo)
        {
            throw new ValorInvalidoException(nameof(ano), ano, AnoMinimo, AnoMaximo);
        }
    }
}
