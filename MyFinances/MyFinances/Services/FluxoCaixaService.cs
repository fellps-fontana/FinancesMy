using MyFinances.DTOs;
using MyFinances.Domain;
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

    public async Task<decimal> CalcularTotalRecebidoNoMes(int ano, int mes)
    {
        var lancamentos = await _lancamentoRepository.ListarParaFluxoCaixaDoMes(ano, mes);

        return lancamentos
            .Where(l => l.Tipo == TipoLancamento.Credit && l.Status == StatusLancamento.Pago)
            .Where(l => ClassificacaoLancamentoService.Classificar(l) != ClassificacaoLancamento.Transferencia)
            .Sum(l => l.Valor);
    }

    public async Task<decimal> CalcularTotalPagoNoMes(int ano, int mes)
    {
        var lancamentos = await _lancamentoRepository.ListarParaFluxoCaixaDoMes(ano, mes);

        return lancamentos
            .Where(l => l.Tipo == TipoLancamento.Debit && l.Status == StatusLancamento.Pago)
            .Where(l => ClassificacaoLancamentoService.Classificar(l) != ClassificacaoLancamento.Transferencia)
            .Sum(l => l.Valor);
    }

    public async Task<decimal> CalcularTotalAPagarNoMes(int ano, int mes)
    {
        var lancamentos = await _lancamentoRepository.ListarParaFluxoCaixaDoMes(ano, mes);

        return lancamentos
            .Where(l => l.Tipo == TipoLancamento.Debit && l.Status == StatusLancamento.Pendente)
            .Where(l => ClassificacaoLancamentoService.Classificar(l) != ClassificacaoLancamento.Transferencia)
            .Sum(l => l.Valor);
    }
}
