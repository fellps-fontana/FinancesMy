using MyFinances.Domain;

namespace MyFinances.Services;

public interface ICategoriaService
{
    Task<Categoria> Criar(string nome, TipoCategoria tipo, Guid? parentId = null);
    Task<IEnumerable<Categoria>> Listar(TipoCategoria? tipo = null, bool? arquivada = null, Guid? parentId = null);
    Task<Categoria> Editar(Guid id, string nome, Guid? parentId);
    Task Arquivar(Guid id);
}
