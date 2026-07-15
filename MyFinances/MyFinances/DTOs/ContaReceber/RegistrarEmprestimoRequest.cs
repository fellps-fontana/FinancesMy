namespace MyFinances.DTOs.ContaReceber;

public class RegistrarEmprestimoRequest
{
    public string Descricao { get; set; } = string.Empty;

    public string Pessoa { get; set; } = string.Empty;

    public decimal ValorTotal { get; set; }

    public Guid ContaOrigemId { get; set; }

    public DateOnly DataRegistro { get; set; }

    public DateOnly? DataPrevista { get; set; }

    public Guid? CategoriaId { get; set; }
}
