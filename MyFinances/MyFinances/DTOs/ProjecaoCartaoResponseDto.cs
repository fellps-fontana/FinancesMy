namespace MyFinances.DTOs;

public class ProjecaoCartaoResponseDto
{
    public Guid ContaId { get; set; }
    public int Mes { get; set; }
    public int Ano { get; set; }
    public bool TemFatura { get; set; }
    public Guid? FaturaId { get; set; }
    public string? StatusPagamento { get; set; } // PAGO | NAO_PAGO, null se TemFatura=false
    public decimal Valor { get; set; } // 0 se TemFatura=false
}
