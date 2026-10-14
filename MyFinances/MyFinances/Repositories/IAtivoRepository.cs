using MyFinances.Domain;

namespace MyFinances.Repositories;

public interface IAtivoRepository
{
    Task Adicionar(Ativo ativo);
    Task<Ativo?> ObterPorId(Guid id);
    Task<IEnumerable<Ativo>> ListarAtivas();
    // Somente ativos com Ativa = true e ContaId == contaId.
    Task<IEnumerable<Ativo>> ListarAtivosPorConta(Guid contaId);

    // true se ha pelo menos um ativo Ativa = true com ContaId == contaId.
    Task<bool> ExisteAtivoAtivoVinculado(Guid contaId);

    Task Salvar();
}
