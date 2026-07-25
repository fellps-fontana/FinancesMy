namespace MyFinances.DTOs.LimiteGasto;

public class DefinirLimiteGastoRequest
{
    public Guid CategoriaId { get; set; }

    public decimal ValorLimite { get; set; }
}
