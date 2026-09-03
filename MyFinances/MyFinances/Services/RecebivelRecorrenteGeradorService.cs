using MyFinances.Domain;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class RecebivelRecorrenteGeradorService : IRecebivelRecorrenteGeradorService
{
    // regra-de-negocio.md item 15: lookahead padrao da janela de materializacao.
    private const int LookaheadDias = 90;

    private readonly IRecebivelRecorrenteRepository _recebivelRecorrenteRepository;
    private readonly IContaReceberRepository _contaReceberRepository;

    public RecebivelRecorrenteGeradorService(
        IRecebivelRecorrenteRepository recebivelRecorrenteRepository,
        IContaReceberRepository contaReceberRepository)
    {
        _recebivelRecorrenteRepository = recebivelRecorrenteRepository;
        _contaReceberRepository = contaReceberRepository;
    }

    public async Task<int> MaterializarOcorrenciasAsync(Guid recebivelRecorrenteId, DateOnly dataReferencia)
    {
        var molde = await _recebivelRecorrenteRepository.ObterPorId(recebivelRecorrenteId);
        if (molde == null || !molde.Ativa)
        {
            return 0;
        }

        var (inicio, fim) = JanelaComProximaOcorrencia(molde, dataReferencia);
        return await MaterializarNoIntervaloAsync(molde, inicio, fim, dataReferencia);
    }

    public async Task MaterializarTodosAtivosAsync(DateOnly dataReferencia)
    {
        var moldes = await _recebivelRecorrenteRepository.ListarAtivos();
        foreach (var molde in moldes)
        {
            await MaterializarOcorrenciasAsync(molde.Id, dataReferencia);
        }
    }

    public async Task MaterializarTodosAtivosNaJanelaPadraoAsync(DateOnly dataReferencia)
    {
        // regra-de-negocio.md item 15 ("Rede de seguranca na projecao"): a
        // materializacao disparada pela projecao do mes usa SO a janela padrao
        // [primeiro dia do mes, hoje + 90 dias] -- nao estende ate a proxima
        // ocorrencia como o job/gatilhos fazem (nao varre ano/mes arbitrario).
        var inicio = PrimeiroDiaDoMes(dataReferencia);
        var fim = dataReferencia.AddDays(LookaheadDias);

        var moldes = await _recebivelRecorrenteRepository.ListarAtivos();
        foreach (var molde in moldes)
        {
            await MaterializarNoIntervaloAsync(molde, inicio, fim, dataReferencia);
        }
    }

    public async Task<int> RemoverOcorrenciasPendentesAsync(Guid recebivelRecorrenteId)
    {
        var molde = await _recebivelRecorrenteRepository.ObterPorId(recebivelRecorrenteId);
        if (molde == null)
        {
            return 0;
        }

        var contasRemovidas = await RemoverPendentesForaDoConjunto(molde, conjunto: null);
        await _contaReceberRepository.Salvar();
        return contasRemovidas;
    }

    public async Task<int> RegenerarOcorrenciasAsync(Guid recebivelRecorrenteId, DateOnly dataReferencia)
    {
        var molde = await _recebivelRecorrenteRepository.ObterPorId(recebivelRecorrenteId);
        if (molde == null)
        {
            return 0;
        }

        var (inicio, fim) = JanelaComProximaOcorrencia(molde, dataReferencia);
        var conjuntoNovo = new HashSet<DateOnly>(
            RecebivelRecorrenteOcorrenciaFactory.CalcularOcorrenciasNoIntervalo(molde, inicio, fim));

        await RemoverPendentesForaDoConjunto(molde, conjuntoNovo);
        await _contaReceberRepository.Salvar();

        return await MaterializarOcorrenciasAsync(recebivelRecorrenteId, dataReferencia);
    }

    // Materializa (idempotente) as ocorrencias do molde no intervalo dado.
    private async Task<int> MaterializarNoIntervaloAsync(
        RecebivelRecorrente molde, DateOnly inicio, DateOnly fim, DateOnly dataGeracao)
    {
        var ocorrencias = RecebivelRecorrenteOcorrenciaFactory.CalcularOcorrenciasNoIntervalo(molde, inicio, fim);

        var contasGeradas = 0;
        foreach (var dataOcorrencia in ocorrencias)
        {
            if (await _recebivelRecorrenteRepository.ExisteOcorrenciaGerada(molde.Id, dataOcorrencia))
            {
                continue;
            }

            var contaReceber = RecebivelRecorrenteOcorrenciaFactory.CriarOcorrenciaPendente(
                molde, dataOcorrencia, dataGeracao);
            await _contaReceberRepository.Adicionar(contaReceber);
            contasGeradas++;
        }

        await _contaReceberRepository.Salvar();
        return contasGeradas;
    }

    // Hard delete das ocorrencias PENDENTE do molde. Se `conjunto` for informado,
    // remove so as que estao fora dele (regeneracao); se null, remove todas
    // (desativacao). PARCIAL/RECEBIDO nunca sao tocadas.
    private async Task<int> RemoverPendentesForaDoConjunto(RecebivelRecorrente molde, HashSet<DateOnly>? conjunto)
    {
        var pendentes = molde.Ocorrencias
            .Where(c => c.Status == StatusContaReceber.Pendente)
            .ToList();

        var removidas = 0;
        foreach (var ocorrencia in pendentes)
        {
            var dentroDoConjunto = conjunto != null
                && ocorrencia.DataPrevista.HasValue
                && conjunto.Contains(ocorrencia.DataPrevista.Value);

            if (dentroDoConjunto)
            {
                continue;
            }

            await _contaReceberRepository.Remover(ocorrencia);
            removidas++;
        }

        return removidas;
    }

    // Janela do job e dos gatilhos criar/reativar/regenerar: vai ate no minimo a
    // proxima ocorrencia a partir de hoje -- garante que um molde ANUAL distante
    // materialize a proxima mesmo alem do lookahead de 90 dias (item 15).
    private static (DateOnly Inicio, DateOnly Fim) JanelaComProximaOcorrencia(
        RecebivelRecorrente molde, DateOnly dataReferencia)
    {
        var inicio = PrimeiroDiaDoMes(dataReferencia);
        var fimLookahead = dataReferencia.AddDays(LookaheadDias);
        var proximaOcorrencia = RecebivelRecorrenteOcorrenciaFactory.PrimeiraOcorrenciaAPartirDe(molde, dataReferencia);
        var fim = proximaOcorrencia > fimLookahead ? proximaOcorrencia : fimLookahead;
        return (inicio, fim);
    }

    private static DateOnly PrimeiroDiaDoMes(DateOnly data) => new(data.Year, data.Month, 1);
}
