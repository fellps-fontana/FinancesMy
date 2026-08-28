using MyFinances.Domain;

namespace MyFinances.Repositories;

public interface IRecebivelRecorrenteRepository
{
    Task Adicionar(RecebivelRecorrente recebivelRecorrente);
    Task<RecebivelRecorrente?> ObterPorId(Guid id);
    Task<IEnumerable<RecebivelRecorrente>> Listar(bool? ativaFiltro = null);
    Task<IEnumerable<RecebivelRecorrente>> ListarAtivos();
    Task Atualizar(RecebivelRecorrente recebivelRecorrente);

    // Idempotencia (item 15): existe conta_receber com este molde + esta data exata?
    Task<bool> ExisteOcorrenciaGerada(Guid recebivelRecorrenteId, DateOnly dataOcorrencia);

    Task Salvar();
}
