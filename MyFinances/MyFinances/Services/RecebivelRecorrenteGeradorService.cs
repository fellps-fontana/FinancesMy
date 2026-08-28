using MyFinances.Domain;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class RecebivelRecorrenteGeradorService : IRecebivelRecorrenteGeradorService
{
    // regra-de-negocio.md item 15: janela de materializacao = primeiro dia do mes
    // corrente ate max(hoje + 90 dias, proxima ocorrencia do molde).
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

        var (inicio, fim) = CalcularJanela(molde, dataReferencia);
        var ocorrencias = RecebivelRecorrenteOcorrenciaFactory.CalcularOcorrenciasNoIntervalo(molde, inicio, fim);

        var contasGeradas = 0;
        foreach (var dataOcorrencia in ocorrencias)
        {
            if (await _recebivelRecorrenteRepository.ExisteOcorrenciaGerada(recebivelRecorrenteId, dataOcorrencia))
            {
                continue;
            }

            var contaReceber = RecebivelRecorrenteOcorrenciaFactory.CriarOcorrenciaPendente(
                molde, dataOcorrencia, dataReferencia);
            await _contaReceberRepository.Adicionar(contaReceber);
            contasGeradas++;
        }

        await _contaReceberRepository.Salvar();
        return contasGeradas;
    }

    public async Task MaterializarTodosAtivosAsync(DateOnly dataReferencia)
    {
        var moldes = await _recebivelRecorrenteRepository.ListarAtivos();
        foreach (var molde in moldes)
        {
            await MaterializarOcorrenciasAsync(molde.Id, dataReferencia);
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

        var (inicio, fim) = CalcularJanela(molde, dataReferencia);
        var conjuntoNovo = new HashSet<DateOnly>(
            RecebivelRecorrenteOcorrenciaFactory.CalcularOcorrenciasNoIntervalo(molde, inicio, fim));

        await RemoverPendentesForaDoConjunto(molde, conjuntoNovo);
        await _contaReceberRepository.Salvar();

        return await MaterializarOcorrenciasAsync(recebivelRecorrenteId, dataReferencia);
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

    private static (DateOnly Inicio, DateOnly Fim) CalcularJanela(RecebivelRecorrente molde, DateOnly dataReferencia)
    {
        var inicio = new DateOnly(dataReferencia.Year, dataReferencia.Month, 1);
        var fimLookahead = dataReferencia.AddDays(LookaheadDias);
        var proximaOcorrencia = RecebivelRecorrenteOcorrenciaFactory.PrimeiraOcorrenciaAPartirDe(molde, inicio);
        var fim = proximaOcorrencia > fimLookahead ? proximaOcorrencia : fimLookahead;
        return (inicio, fim);
    }
}
