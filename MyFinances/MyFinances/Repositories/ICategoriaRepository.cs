using MyFinances.Domain;

namespace MyFinances.Repositories;

public interface ICategoriaRepository
{
    Task Adicionar(Categoria categoria);
    Task<Categoria?> ObterPorId(Guid id);
    Task<IEnumerable<Categoria>> Listar(TipoCategoria? tipo = null, bool? arquivada = null, Guid? parentId = null);

    // Todas as categorias, ativas e arquivadas, sem filtro (Subcategorias incluidas).
    Task<IEnumerable<Categoria>> ListarTodas();
    Task Salvar();
}
