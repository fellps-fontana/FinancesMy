using MyFinances.Models;

namespace MyFinances.Repositories;

public interface IContaRepository
{
    Task Adicionar(Conta conta);
    Task<Conta?> ObterPorId(Guid id);
    Task<IEnumerable<Conta>> ListarPorTipo(TipoConta tipo);
    Task Salvar();
}
