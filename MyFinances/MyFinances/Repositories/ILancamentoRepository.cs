using MyFinances.Domain;

namespace MyFinances.Repositories;

public interface ILancamentoRepository
{
    Task Adicionar(Lancamento lancamento);
    Task<Lancamento?> ObterPorId(Guid id);
    Task<IEnumerable<Lancamento>> ListarPorConta(Guid contaId);
    Task<IEnumerable<Lancamento>> ListarPorFatura(Guid faturaId);
    Task<IEnumerable<Lancamento>> ListarParaFluxoCaixa(Guid? contaId);
    Task<IEnumerable<Lancamento>> ListarParaFluxoCaixaDoMes(int ano, int mes);
    Task Atualizar(Lancamento lancamento);
    Task Remover(Lancamento lancamento);
    Task Salvar();
}
