namespace MyFinances.Dtos;

public class CriarContaRequest
{
    public required string Nome { get; set; }
    public string? Tipo { get; set; } // BANCO | CARTAO | INVESTIMENTO
    public decimal? SaldoManual { get; set; }
    public int? DiaFechamento { get; set; } // obrigatorio para CARTAO
    public int? DiaVencimento { get; set; } // obrigatorio para CARTAO
}
