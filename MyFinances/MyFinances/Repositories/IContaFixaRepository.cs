using MyFinances.Domain;

namespace MyFinances.Repositories;

public interface IContaFixaRepository
{
    Task Adicionar(ContaFixa contaFixa);
    Task<ContaFixa?> ObterPorId(Guid id);
    Task<IEnumerable<ContaFixa>> Listar(bool? ativaFiltro = null);
    Task Atualizar(ContaFixa contaFixa);
    Task<bool> ExisteLancamentoGerado(Guid contaFixaId, int ano, int mes);
    Task Salvar();
}
