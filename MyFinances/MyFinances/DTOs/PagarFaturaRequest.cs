namespace MyFinances.DTOs;

public class PagarFaturaRequest
{
    public required decimal Valor { get; set; }

    public required DateOnly Data { get; set; }

    public Guid ContaOrigemId { get; set; }
}
