namespace MyFinances.DTOs.Ativo;

public class RegistrarAporteRequest
{
    public decimal Quantidade { get; set; }

    public decimal PrecoUnitario { get; set; }

    public DateOnly Data { get; set; }
}
