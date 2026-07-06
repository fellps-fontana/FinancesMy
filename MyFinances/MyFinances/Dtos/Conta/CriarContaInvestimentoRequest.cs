namespace MyFinances.Dtos.Conta;

public class CriarContaInvestimentoRequest
{
    public string Nome { get; set; } = string.Empty;

    public decimal SaldoInicial { get; set; }
}
