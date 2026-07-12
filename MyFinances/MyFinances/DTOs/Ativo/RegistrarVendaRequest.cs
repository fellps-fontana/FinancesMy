namespace MyFinances.DTOs.Ativo;

public class RegistrarVendaRequest
{
    public decimal Quantidade { get; set; }

    public decimal PrecoUnitario { get; set; }

    public DateOnly Data { get; set; }

    public string? Observacao { get; set; }
}
