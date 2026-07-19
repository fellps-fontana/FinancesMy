namespace MyFinances.DTOs.ContaReceber;

public class RegistrarRecebivelRequest
{
    public string Descricao { get; set; } = string.Empty;

    public decimal ValorTotal { get; set; }

    public DateOnly DataRegistro { get; set; }

    public DateOnly? DataPrevista { get; set; }

    public Guid? CategoriaId { get; set; }
}
