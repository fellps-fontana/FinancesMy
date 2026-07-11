using MyFinances.Models;

namespace MyFinances.Repositories;

public interface ICategoriaRepository
{
    Task Adicionar(Categoria categoria);
    Task<Categoria?> ObterPorId(Guid id);
    Task<IEnumerable<Categoria>> Listar(TipoCategoria? tipo = null, bool? arquivada = null, Guid? parentId = null);
    Task Salvar();
}
