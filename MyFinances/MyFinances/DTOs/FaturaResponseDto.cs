namespace MyFinances.DTOs;

public class FaturaResponseDto
{
    public Guid Id { get; set; }
    public Guid ContaId { get; set; }
    public DateOnly DataFechamento { get; set; }
    public DateOnly DataVencimento { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal ValorTotal { get; set; }
    public decimal ValorPago { get; set; }
    public decimal ValorPendente { get; set; }
}
