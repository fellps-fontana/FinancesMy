using MyFinances.Domain;

namespace MyFinances.Repositories;

public interface IDeParaCategoriaRepository
{
    Task Adicionar(DeParaCategoria deParaCategoria);
    Task<DeParaCategoria?> ObterPorId(Guid id);
    Task<DeParaCategoria?> ObterPorCategoriaPierre(string categoriaPierre);
    Task<IEnumerable<DeParaCategoria>> Listar(string? categoriaPierre = null);
    Task Remover(DeParaCategoria deParaCategoria);
    Task Salvar();
}
