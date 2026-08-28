using MyFinances.Domain;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class RecebivelRecorrenteService : IRecebivelRecorrenteService
{
    private readonly IRecebivelRecorrenteRepository _recebivelRecorrenteRepository;
    private readonly IRecebivelRecorrenteGeradorService _geradorService;

    public RecebivelRecorrenteService(
        IRecebivelRecorrenteRepository recebivelRecorrenteRepository,
        IRecebivelRecorrenteGeradorService geradorService)
    {
        _recebivelRecorrenteRepository = recebivelRecorrenteRepository;
        _geradorService = geradorService;
    }

    public Task<RecebivelRecorrente> CriarAsync(
        string descricao, decimal valor, string periodicidade,
        int? diaVencimento, int? mesReferencia, string? diaDaSemana, Guid? categoriaId)
        => throw new NotImplementedException();

    public Task<RecebivelRecorrente> EditarAsync(
        Guid id, decimal valor, string periodicidade,
        int? diaVencimento, int? mesReferencia, string? diaDaSemana, Guid? categoriaId)
        => throw new NotImplementedException();

    public Task DesativarAsync(Guid id) => throw new NotImplementedException();

    public Task ReativarAsync(Guid id) => throw new NotImplementedException();

    public Task<RecebivelRecorrente> ObterPorId(Guid id) => throw new NotImplementedException();

    public Task<IEnumerable<RecebivelRecorrente>> Listar(bool? ativaFiltro = null)
        => throw new NotImplementedException();
}
