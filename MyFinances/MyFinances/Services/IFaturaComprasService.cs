using MyFinances.Domain;

namespace MyFinances.Services;

public interface IFaturaComprasService
{
    Task<IReadOnlyList<Lancamento>?> ListarComprasAsync(Guid contaId, Guid faturaId);
}
