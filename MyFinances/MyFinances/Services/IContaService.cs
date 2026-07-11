using MyFinances.Domain;

namespace MyFinances.Services;

public interface IContaService
{
    Task<Conta> CriarContaInvestimento(string nome, decimal saldoInicial);
    Task<IEnumerable<Conta>> ListarContasInvestimento();
    Task<decimal> CalcularTotalInvestido();
    Task AtualizarSaldoManual(Guid contaId, decimal novoSaldo);
    Task DesativarConta(Guid contaId);
}
