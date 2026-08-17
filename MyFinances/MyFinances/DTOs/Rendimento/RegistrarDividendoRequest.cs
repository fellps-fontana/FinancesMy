namespace MyFinances.DTOs.Rendimento;

public class RegistrarDividendoRequest
{
    public decimal Valor { get; set; }

    public DateOnly Data { get; set; }
}
