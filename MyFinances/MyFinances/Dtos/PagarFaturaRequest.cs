namespace MyFinances.Dtos;

public class PagarFaturaRequest
{
    public Guid ContaOrigemId { get; set; }
    public DateOnly Data { get; set; }
}
