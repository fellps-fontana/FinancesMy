using MyFinances.Domain;

namespace MyFinances.Services;

public interface IDeParaCategoriaService
{
    Task<DeParaCategoria> Criar(string categoriaPierre, Guid categoriaId);
    Task<IEnumerable<DeParaCategoria>> Listar(string? categoriaPierre = null);
    Task<DeParaCategoria> Editar(Guid id, Guid novaCategoriaId);
    Task Excluir(Guid id);
}
