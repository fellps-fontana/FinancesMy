using MyFinances.Domain;

namespace MyFinances.Services;

public interface IRecebivelRecorrenteService
{
    Task<RecebivelRecorrente> CriarAsync(
        string descricao, decimal valor, string periodicidade,
        int? diaVencimento, int? mesReferencia, string? diaDaSemana, Guid? categoriaId);

    Task<RecebivelRecorrente> EditarAsync(
        Guid id, decimal valor, string periodicidade,
        int? diaVencimento, int? mesReferencia, string? diaDaSemana, Guid? categoriaId);

    Task DesativarAsync(Guid id);

    Task ReativarAsync(Guid id);

    Task<RecebivelRecorrente> ObterPorId(Guid id);

    Task<IEnumerable<RecebivelRecorrente>> Listar(bool? ativaFiltro = null);
}
