using MyFinances.Domain;

namespace MyFinances.Repositories;

public interface ICompraParceladaRepository
{
    Task Adicionar(CompraParcelada compraParcelada);
    Task<CompraParcelada?> ObterPorId(Guid id);
    Task Salvar();
}
