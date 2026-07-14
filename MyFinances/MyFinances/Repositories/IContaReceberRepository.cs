using MyFinances.Domain;

namespace MyFinances.Repositories;

public interface IContaReceberRepository
{
    Task Adicionar(ContaReceber contaReceber);
    Task<ContaReceber?> ObterPorId(Guid id);
    Task<IEnumerable<ContaReceber>> Listar(StatusContaReceber? statusFiltro = null);
    Task Atualizar(ContaReceber contaReceber);
    Task Salvar();
}
