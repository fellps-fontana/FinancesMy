namespace MyFinances.DTOs.RecebivelRecorrente;

public class EditarRecebivelRecorrenteRequest
{
    public decimal Valor { get; set; }
    public string Periodicidade { get; set; } = "MENSAL";
    public int? DiaVencimento { get; set; }
    public int? MesReferencia { get; set; }
    public string? DiaDaSemana { get; set; }
    public Guid? CategoriaId { get; set; }
}
