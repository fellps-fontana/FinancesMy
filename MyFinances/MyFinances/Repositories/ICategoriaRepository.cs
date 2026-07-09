using MyFinances.Models;

namespace MyFinances.Repositories;

public interface ICategoriaRepository
{
    Task Adicionar(Categoria categoria);
    Task<Categoria?> ObterPorId(Guid id);
    Task<IEnumerable<Categoria>> ListarTodas();
    Task<IEnumerable<Categoria>> ListarPorTipo(TipoCategoria tipo);
    Task Atualizar(Categoria categoria);
    Task Salvar();
}
