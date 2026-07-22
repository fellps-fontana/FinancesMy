namespace MyFinances.DTOs.ContaFixa;

public class CriarContaFixaRequest
{
    public Guid ContaId { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    public int DiaVencimento { get; set; }

    public Guid? CategoriaId { get; set; }
}
