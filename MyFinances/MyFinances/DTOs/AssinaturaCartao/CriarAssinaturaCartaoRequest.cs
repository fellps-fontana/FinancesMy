namespace MyFinances.DTOs.AssinaturaCartao;

public class CriarAssinaturaCartaoRequest
{
    public Guid ContaId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int DiaReferencia { get; set; }
    public Guid? CategoriaId { get; set; }
}
