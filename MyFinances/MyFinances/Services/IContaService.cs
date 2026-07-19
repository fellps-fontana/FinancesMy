using MyFinances.DTOs.Conta;
using MyFinances.Domain;

namespace MyFinances.Services;

public interface IContaService
{
    Task<(bool Sucesso, Conta? Conta, string? Erro)> CriarContaAsync(CriarContaRequest request);
    Task<Conta> CriarContaInvestimento(string nome, decimal saldoInicial);
    Task<IEnumerable<Conta>> ListarContasPorTipo(TipoConta tipo);
    Task<IEnumerable<Conta>> ListarContasInvestimento();
    Task<decimal> CalcularTotalInvestido();
    Task AtualizarSaldoManual(Guid contaId, decimal novoSaldo);
    Task DesativarConta(Guid contaId);
}
