using MyFinances.Models;

namespace MyFinances.Services;

public interface IAtivoService
{
    Task<Ativo> RegistrarCompra(Guid contaId, string ticker, decimal quantidade, decimal precoUnitario, DateOnly data, string? nome);
    Task<Ativo> RegistrarVenda(Guid contaId, Guid ativoId, decimal quantidade, decimal precoUnitario, DateOnly data, string? observacao);
    Task<IEnumerable<Ativo>> ListarAtivosPorConta(Guid contaId);
}
