namespace MyFinances.DTOs;

public class CriarTransferenciaRequest
{
    public required Guid ContaOrigemId { get; set; }
    public required Guid ContaDestinoId { get; set; }
    public required decimal Valor { get; set; }
    public DateOnly? Data { get; set; }
    public string? Descricao { get; set; }
}
