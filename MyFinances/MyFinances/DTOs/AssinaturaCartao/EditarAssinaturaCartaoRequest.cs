namespace MyFinances.DTOs.AssinaturaCartao;

public class EditarAssinaturaCartaoRequest
{
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int DiaReferencia { get; set; }
    public Guid? CategoriaId { get; set; }
}
