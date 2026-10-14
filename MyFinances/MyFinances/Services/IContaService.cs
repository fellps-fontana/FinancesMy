using MyFinances.DTOs.Conta;
using MyFinances.Domain;

namespace MyFinances.Services;

public interface IContaService
{
    Task<(bool Sucesso, Conta? Conta, string? Erro)> CriarContaAsync(CriarContaRequest request);
    Task<Conta> CriarContaInvestimento(string nome, decimal saldoInicial);
    Task<IEnumerable<Conta>> ListarContasPorTipo(TipoConta tipo);
    Task<IEnumerable<Conta>> ListarContasInvestimento();
    // Item 8.5: saldos das contas INVESTIMENTO ativas + valor_atual dos ativos ativos SEM conta.
    Task<decimal> CalcularTotalInvestido();
    // Saldo exibido (itens 8.5 e 10). Calculado = true quando a conta e INVESTIMENTO e tem
    // ativo ativo vinculado (soma do valor_atual); senao SaldoManual ?? 0m, Calculado = false.
    Task<(decimal Saldo, bool Calculado)> ObterSaldo(Conta conta);

    // Guard novo: conta com ativo ativo vinculado -> SaldoCalculadoNaoEditavelException (422).
    Task AtualizarSaldoManual(Guid contaId, decimal novoSaldo);
    // Guard novo: conta com ativo ativo vinculado -> ContaPossuiAtivoAtivoException (409).
    Task DesativarConta(Guid contaId);

    // Item 10.2. Edita Nome/Icone/Cor/Subtipo; DiaFechamento/DiaVencimento so em Cartao.
    // Tipo imutavel. Conta inativa e editavel (nao reativa). Subtipo so vale p/ Banco (outros -> null).
    // Dias so em Cartao (outros tipos: ignorados). Cartao exige os dois, 1..31.
    // ContaNaoEncontradaException (404), CampoObrigatorioException("Nome" | "DiaFechamento" | "DiaVencimento") (400),
    // SubtipoContaInvalidoException (400), ValorInvalidoException (400, dia fora de 1..31).
    Task<Conta> EditarConta(Guid id, EditarContaRequest request);
}
