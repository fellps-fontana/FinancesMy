using MyFinances.DTOs;

namespace MyFinances.Services;

public interface IFluxoCaixaService
{
    Task<IEnumerable<LancamentoResponseDto>> ListarFluxoCaixa(Guid? contaId);
}
