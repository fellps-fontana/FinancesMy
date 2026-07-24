using MyFinances.Domain;

namespace MyFinances.Repositories;

public interface ILimiteGastoRepository
{
    Task Adicionar(LimiteGasto limiteGasto);
    Task<LimiteGasto?> ObterPorCategoriaId(Guid categoriaId);
    Task<IEnumerable<LimiteGasto>> Listar();
    Task Remover(LimiteGasto limiteGasto);
    Task Salvar();
}
