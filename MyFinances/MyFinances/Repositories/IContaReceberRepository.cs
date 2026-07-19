using MyFinances.Domain;

namespace MyFinances.Repositories;

public interface IContaReceberRepository
{
    Task Adicionar(ContaReceber contaReceber);
    Task<ContaReceber?> ObterPorId(Guid id);
    Task<IEnumerable<ContaReceber>> Listar(StatusContaReceber? statusFiltro = null);
    Task<IEnumerable<ContaReceber>> ListarParaProjecaoDoMes(int ano, int mes);
    Task Atualizar(ContaReceber contaReceber);
    Task Salvar();
}
