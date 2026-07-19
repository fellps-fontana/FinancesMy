namespace MyFinances.DTOs.ContaReceber;

public class RegistrarRecebimentoRequest
{
    public decimal Valor { get; set; }

    public DateOnly Data { get; set; }

    public Guid ContaDestinoId { get; set; }

    public Guid? CategoriaId { get; set; }
}
