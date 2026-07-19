namespace MyFinances.DTOs.Conta;

public class CriarContaRequest
{
    public required string Nome { get; set; }

    public required string Tipo { get; set; }

    public decimal? SaldoManual { get; set; }

    public int? DiaFechamento { get; set; }

    public int? DiaVencimento { get; set; }
}
