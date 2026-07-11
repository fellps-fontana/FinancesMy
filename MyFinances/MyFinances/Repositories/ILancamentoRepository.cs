using MyFinances.Models;

namespace MyFinances.Repositories;

public interface ILancamentoRepository
{
    Task Adicionar(Lancamento lancamento);
    Task<Lancamento?> ObterPorId(Guid id);
    Task<IEnumerable<Lancamento>> ListarPorConta(Guid contaId);
    Task<IEnumerable<Lancamento>> ListarPorFatura(Guid faturaId);
    Task Atualizar(Lancamento lancamento);
    Task Salvar();
}
