using MyFinances.DTOs.Conta;
using MyFinances.Models;

namespace MyFinances.Services;

public interface IContaService
{
    Task<(bool Sucesso, Conta? Conta, string? Erro)> CriarContaAsync(CriarContaRequest request);
    Task<Conta> CriarContaInvestimento(string nome, decimal saldoInicial);
    Task<IEnumerable<Conta>> ListarContasPorTipo(TipoConta tipo);
    Task<IEnumerable<Conta>> ListarContasInvestimento();
    Task<decimal> CalcularTotalInvestido();
    Task<Dictionary<Guid, decimal>> ObterSaldosContasInvestimento();
    Task<Dictionary<Guid, (decimal saldo, bool estaEmModoCarteira)>> ObterSaldosComModoContasInvestimento();
    Task AtualizarSaldoManual(Guid contaId, decimal novoSaldo);
    Task DesativarConta(Guid contaId);
}
