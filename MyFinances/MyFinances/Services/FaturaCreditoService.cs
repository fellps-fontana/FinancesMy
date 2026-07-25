using MyFinances.Domain;
using MyFinances.Repositories;

namespace MyFinances.Services;

// Orquestra a leitura de faturas de uma conta e a cadeia de credito
// (Domain/FaturaCreditoCalculator) sobre elas. Unico ponto de consulta do
// saldo AJUSTADO de fatura -- consumido tanto por PagamentoFaturaService
// (validar pagamento/decidir StatusFatura.Paga) quanto pela exibicao
// (FaturaResponse/FaturasController), para nao duplicar o encadeamento em
// dois lugares.
public class FaturaCreditoService
{
    private readonly IFaturaRepository _faturaRepository;

    public FaturaCreditoService(IFaturaRepository faturaRepository)
    {
        _faturaRepository = faturaRepository;
    }

    // Cadeia completa da conta, em ordem cronologica. Uso interno de
    // ObterSaldoAjustadoAsync; tambem util para diagnostico/relatorio.
    public async Task<IReadOnlyList<FaturaSaldoAjustado>> CalcularCadeiaDaContaAsync(Guid contaId)
    {
        var faturas = await _faturaRepository.ListarPorConta(contaId);
        var faturasOrdenadas = faturas
            .OrderBy(f => f.DataVencimento)
            .ToList()
            .AsReadOnly();

        return FaturaCreditoCalculator.CalcularCadeia(faturasOrdenadas);
    }

    // Saldo ajustado de UMA fatura especifica dentro da cadeia da sua
    // conta. Devolve null se a fatura nao existir ou nao pertencer a
    // contaId informado. Otimizacao: calcula a cadeia so ate a fatura alvo
    // (faturas futuras nao influenciam o credito de uma fatura anterior).
    public async Task<FaturaSaldoAjustado?> ObterSaldoAjustadoAsync(Guid contaId, Guid faturaId)
    {
        var fatura = await _faturaRepository.ObterPorId(faturaId);
        if (fatura == null || fatura.ContaId != contaId)
        {
            return null;
        }

        var faturas = await _faturaRepository.ListarPorConta(contaId);
        var faturasAteFaturaAlvo = faturas
            .Where(f => f.DataVencimento <= fatura.DataVencimento)
            .OrderBy(f => f.DataVencimento)
            .ToList()
            .AsReadOnly();

        var cadeia = FaturaCreditoCalculator.CalcularCadeia(faturasAteFaturaAlvo);
        return cadeia.FirstOrDefault(f => f.FaturaId == faturaId);
    }
}
