using MyFinances.Domain;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class FaturaProjecaoService : IFaturaProjecaoService
{
    private readonly IFaturaRepository _faturaRepository;

    public FaturaProjecaoService(IFaturaRepository faturaRepository)
    {
        _faturaRepository = faturaRepository;
    }

    public async Task<FaturaProjecaoMes> CalcularProjecaoCartaoDoMes(int ano, int mes)
    {
        var faturas = await _faturaRepository.ListarFaturasCartaoPorVencimentoNoMes(ano, mes);

        var totalPago = 0m;
        var totalNaoPago = 0m;

        foreach (var fatura in faturas)
        {
            var saldo = FaturaSaldoCalculator.Calcular(fatura);

            totalPago += saldo.ValorPago;
            totalNaoPago += saldo.ValorPendente;
        }

        return new FaturaProjecaoMes(totalPago, totalNaoPago);
    }
}
