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
        return await SomarLancamentosDoMes(ano, mes, TipoLancamento.Credit, StatusLancamento.Pago);
    }

    public async Task<decimal> CalcularTotalPagoNoMes(int ano, int mes)
    {
        return await SomarLancamentosDoMes(ano, mes, TipoLancamento.Debit, StatusLancamento.Pago);
    }

    public async Task<decimal> CalcularTotalAPagarNoMes(int ano, int mes)
    {
        return await SomarLancamentosDoMes(ano, mes, TipoLancamento.Debit, StatusLancamento.Pendente);
    }

    private async Task<decimal> SomarLancamentosDoMes(int ano, int mes, TipoLancamento tipo, StatusLancamento status)
    {
        var lancamentos = await _lancamentoRepository.ListarParaFluxoCaixaDoMes(ano, mes);

        return lancamentos
            .Where(l => l.Tipo == tipo && l.Status == status)
            .Where(l => ClassificacaoLancamentoService.Classificar(l) != ClassificacaoLancamento.Transferencia)
            .Sum(l => l.Valor);
    }
}
