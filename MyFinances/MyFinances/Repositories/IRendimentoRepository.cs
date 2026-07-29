using MyFinances.Domain;

namespace MyFinances.Repositories;

public interface IRendimentoRepository
{
    Task Adicionar(Rendimento rendimento);
    Task<IEnumerable<Rendimento>> ListarPorAtivo(Guid ativoId);
    Task<IEnumerable<Rendimento>> ListarTodos();
    Task Salvar();
}
