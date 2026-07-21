using MyFinances.DTOs;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class FluxoCaixaService : IFluxoCaixaService
{
    private readonly ILancamentoRepository _lancamentoRepository;

    public FluxoCaixaService(ILancamentoRepository lancamentoRepository)
    {
        _lancamentoRepository = lancamentoRepository;
    }

    public async Task<IEnumerable<LancamentoResponseDto>> ListarFluxoCaixa(Guid? contaId)
    {
        var lancamentos = await _lancamentoRepository.ListarParaFluxoCaixa(contaId);
        return lancamentos.Select(LancamentoResponseDto.FromLancamento);
    }

    public Task<decimal> CalcularTotalRecebidoNoMes(int ano, int mes)
    {
        throw new NotImplementedException();
    }

    public Task<decimal> CalcularTotalPagoNoMes(int ano, int mes)
    {
        throw new NotImplementedException();
    }

    public Task<decimal> CalcularTotalAPagarNoMes(int ano, int mes)
    {
        throw new NotImplementedException();
    }
}
