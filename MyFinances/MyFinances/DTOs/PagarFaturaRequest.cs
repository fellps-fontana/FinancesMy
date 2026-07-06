namespace MyFinances.DTOs;

public class PagarFaturaRequest
{
    public Guid ContaOrigemId { get; set; }
    public DateOnly Data { get; set; }
    public decimal Valor { get; set; }
}
