using MyFinances.Domain;

namespace MyFinances.Repositories;

public interface IAtivoRepository
{
    Task Adicionar(Ativo ativo);
    Task AdicionarMovimentacao(MovimentacaoAtivo movimentacao);
    Task<Ativo?> ObterPorId(Guid id);
    Task<IEnumerable<Ativo>> ListarPorConta(Guid contaId);
    Task<IEnumerable<Ativo>> ListarAtivosAtivosPorConta(Guid contaId);
    Task<Ativo?> ObterAtivoAtivoPorTicker(Guid contaId, string ticker);
    Task<Dictionary<Guid, decimal>> SomarValorAtivosPorConta(IEnumerable<Guid> contaIds);
    Task<Dictionary<Guid, bool>> VerificarContasComAtivos(IEnumerable<Guid> contaIds);
    Task Salvar();
}
