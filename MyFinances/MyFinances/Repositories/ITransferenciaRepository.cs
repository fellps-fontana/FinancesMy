using MyFinances.Domain;

namespace MyFinances.Repositories;

public interface ITransferenciaRepository
{
    Task Adicionar(Transferencia transferencia);
    Task<Transferencia?> ObterPorId(Guid id);
    Task<IEnumerable<Transferencia>> ListarPorConta(Guid contaId);
    Task<IEnumerable<Transferencia>> ListarPorFatura(Guid faturaId);
    Task Atualizar(Transferencia transferencia);
    Task Salvar();
}
