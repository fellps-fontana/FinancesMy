using MyFinances.Domain;

namespace MyFinances.Services;

public interface ILimiteGastoService
{
    Task<(LimiteGasto LimiteGasto, bool Criado)> Definir(Guid categoriaId, decimal valorLimite);
    Task Remover(Guid categoriaId);
    Task<IEnumerable<LimiteGasto>> Listar();
    Task<(LimiteGasto LimiteGasto, LimiteGastoStatus Status)> ObterGastoVsLimite(Guid categoriaId, int ano, int mes);
    Task<IEnumerable<(LimiteGasto LimiteGasto, LimiteGastoStatus Status)>> ObterGastoVsLimiteTodasCategorias(int ano, int mes);
}
