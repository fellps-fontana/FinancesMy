using MyFinances.Models;

namespace MyFinances.Services;

public interface IContaService
{
    Task<Conta> CriarContaInvestimento(string nome, decimal saldoInicial);
    Task<IEnumerable<Conta>> ListarContasInvestimento();
    Task<decimal> CalcularTotalInvestido();
    Task<Dictionary<Guid, decimal>> ObterSaldosContasInvestimento();
    Task<Dictionary<Guid, (decimal saldo, bool estaEmModoCarteira)>> ObterSaldosComModoContasInvestimento();
    Task AtualizarSaldoManual(Guid contaId, decimal novoSaldo);
    Task DesativarConta(Guid contaId);
}
