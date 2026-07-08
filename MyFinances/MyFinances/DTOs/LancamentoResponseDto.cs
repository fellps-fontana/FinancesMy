namespace MyFinances.DTOs;

public class LancamentoResponseDto
{
    public Guid Id { get; set; }
    public Guid ContaId { get; set; }
    public Guid? CategoriaId { get; set; }
    public string? Descricao { get; set; }
    public decimal Valor { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public DateOnly Data { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool Manual { get; set; }
}
