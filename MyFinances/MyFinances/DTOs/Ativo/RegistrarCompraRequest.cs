namespace MyFinances.DTOs.Ativo;

public class RegistrarCompraRequest
{
    public string Ticker { get; set; } = string.Empty;

    public decimal Quantidade { get; set; }

    public decimal PrecoUnitario { get; set; }

    public DateOnly Data { get; set; }

    public string? Nome { get; set; }
}
