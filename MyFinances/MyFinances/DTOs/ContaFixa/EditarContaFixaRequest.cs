namespace MyFinances.DTOs.ContaFixa;

public class EditarContaFixaRequest
{
    public decimal Valor { get; set; }

    public int DiaVencimento { get; set; }

    public Guid? CategoriaId { get; set; }
}
