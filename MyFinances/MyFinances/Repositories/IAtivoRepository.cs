using MyFinances.Domain;

namespace MyFinances.Repositories;

public interface IAtivoRepository
{
    Task Adicionar(Ativo ativo);
    Task<Ativo?> ObterPorId(Guid id);
    Task<IEnumerable<Ativo>> ListarAtivas();
    Task Salvar();
}
