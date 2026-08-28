using MyFinances.Repositories;

namespace MyFinances.Services;

public class RecebivelRecorrenteGeradorService : IRecebivelRecorrenteGeradorService
{
    // regra-de-negocio.md item 15: janela de materializacao = mes corrente ate hoje + 90 dias.
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

    public Task<int> MaterializarOcorrenciasAsync(Guid recebivelRecorrenteId, DateOnly dataReferencia)
        => throw new NotImplementedException();

    public Task MaterializarTodosAtivosAsync(DateOnly dataReferencia)
        => throw new NotImplementedException();

    public Task<int> RemoverOcorrenciasPendentesAsync(Guid recebivelRecorrenteId)
        => throw new NotImplementedException();

    public Task<int> RegenerarOcorrenciasAsync(Guid recebivelRecorrenteId, DateOnly dataReferencia)
        => throw new NotImplementedException();
}
