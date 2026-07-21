using MyFinances.Domain;

namespace MyFinances.Services;

public interface ILimiteGastoService
{
    Task<LimiteGasto> Definir(Guid categoriaId, decimal valorLimite);
    Task Remover(Guid categoriaId);
    Task<IEnumerable<LimiteGasto>> Listar();
    Task<LimiteGastoStatus> ObterGastoVsLimite(Guid categoriaId, int ano, int mes);
    Task<IEnumerable<(LimiteGasto LimiteGasto, LimiteGastoStatus Status)>> ObterGastoVsLimiteTodasCategorias(int ano, int mes);
}
