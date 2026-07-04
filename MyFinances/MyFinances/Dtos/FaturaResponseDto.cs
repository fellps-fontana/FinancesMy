namespace MyFinances.Dtos;

public class FaturaResponseDto
{
    public Guid Id { get; set; }
    public Guid ContaId { get; set; }
    public DateOnly DataFechamento { get; set; }
    public DateOnly DataVencimento { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? TransferenciaId { get; set; }
}
