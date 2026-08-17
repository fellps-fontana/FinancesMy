using MyFinances.DTOs;
using MyFinances.Domain;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class FluxoCaixaService : IFluxoCaixaService
{
    private readonly ILancamentoRepository _lancamentoRepository;
    private readonly ITransferenciaRepository _transferenciaRepository;

    public FluxoCaixaService(
        ILancamentoRepository lancamentoRepository,
        ITransferenciaRepository transferenciaRepository)
    {
        _lancamentoRepository = lancamentoRepository;
        _transferenciaRepository = transferenciaRepository;
    }

    public async Task<IEnumerable<LancamentoResponseDto>> ListarFluxoCaixa(Guid? contaId)
    {
        var lancamentos = await _lancamentoRepository.ListarParaFluxoCaixa(contaId);
        return lancamentos.Select(LancamentoResponseDto.FromLancamento);
    }

    public async Task<IEnumerable<FluxoCaixaItemDto>> ListarFluxoCaixaTodasContas()
    {
        var lancamentos = await _lancamentoRepository.ListarParaFluxoCaixa(null);
        var transferencias = await _transferenciaRepository.ListarTodas();

        var itens = new List<FluxoCaixaItemDto>();

        foreach (var lancamento in lancamentos)
        {
            itens.Add(new FluxoCaixaItemDto
            {
                TipoItem = TipoItemFluxoCaixa.Lancamento.ToStorageValue(),
                Data = lancamento.Data,
                Lancamento = LancamentoResponseDto.FromLancamento(lancamento),
                Transferencia = null
            });
        }

        foreach (var transferencia in transferencias)
        {
            itens.Add(new FluxoCaixaItemDto
            {
                TipoItem = TipoItemFluxoCaixa.Transferencia.ToStorageValue(),
                Data = transferencia.Data,
                Lancamento = null,
                Transferencia = TransferenciaResponse.FromTransferencia(transferencia)
            });
        }

        return itens.OrderByDescending(i => i.Data);
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
