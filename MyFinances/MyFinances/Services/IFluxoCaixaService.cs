using MyFinances.DTOs;

namespace MyFinances.Services;

public interface IFluxoCaixaService
{
    Task<IEnumerable<LancamentoResponseDto>> ListarFluxoCaixa(Guid? contaId);

    Task<IEnumerable<FluxoCaixaItemDto>> ListarFluxoCaixaTodasContas();

    Task<decimal> CalcularTotalRecebidoNoMes(int ano, int mes);

    Task<decimal> CalcularTotalPagoNoMes(int ano, int mes);

    Task<decimal> CalcularTotalAPagarNoMes(int ano, int mes);
}
