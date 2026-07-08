namespace MyFinances.DTOs.Conta;

public class CriarContaInvestimentoRequest
{
    public string Nome { get; set; } = string.Empty;

    public decimal SaldoInicial { get; set; }
}
