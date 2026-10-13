using MyFinances.Domain;

namespace MyFinances.Repositories;

public interface ILancamentoRepository
{
    Task Adicionar(Lancamento lancamento);
    Task<Lancamento?> ObterPorId(Guid id);
    Task<IEnumerable<Lancamento>> ListarPorConta(Guid contaId);
    Task<IEnumerable<Lancamento>> ListarPorFatura(Guid faturaId);
    Task<IEnumerable<Lancamento>> ListarParaFluxoCaixa(Guid? contaId);
    Task<IEnumerable<Lancamento>> ListarPorCategoriasEPeriodo(IEnumerable<Guid> categoriaIds, int ano, int mes);
    Task<IEnumerable<Lancamento>> ListarParaFluxoCaixaDoMes(int ano, int mes);
    Task Atualizar(Lancamento lancamento);
    Task Remover(Lancamento lancamento);
    Task Salvar();

    // Item 19: Debit, !Oculto, TransferenciaId == null, Status in (Pendente, Pago), mes/ano
    // pela Data; Include Conta.
    Task<IEnumerable<Lancamento>> ListarDebitosParaRelatorioAsync(int ano, int mes);
}
