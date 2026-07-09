using MyFinances.Domain;
using MyFinances.Models;
using MyFinances.Repositories;

namespace MyFinances.Services;

public class SaldoCartaoService
{
    private readonly IContaRepository _contaRepository;
    private readonly IFaturaRepository _faturaRepository;

    public SaldoCartaoService(IContaRepository contaRepository, IFaturaRepository faturaRepository)
    {
        _contaRepository = contaRepository;
        _faturaRepository = faturaRepository;
    }

    public async Task<(bool Sucesso, decimal Saldo, string? Erro)> CalcularSaldoAsync(Guid contaId)
    {
        var conta = await _contaRepository.ObterPorId(contaId);
        if (conta == null)
        {
            return (false, 0, "Conta nao encontrada");
        }

        if (conta.Tipo != TipoConta.Cartao)
        {
            return (false, 0, "A operacao so pode ser realizada em contas de cartao de credito");
        }

        var faturas = await _faturaRepository.ListarPorConta(contaId);

        decimal saldoTotal = 0;
        foreach (var fatura in faturas)
        {
            var saldo = FaturaSaldoCalculator.Calcular(fatura);
            saldoTotal += saldo.ValorPendente;
        }

        return (true, saldoTotal, null);
    }
}
