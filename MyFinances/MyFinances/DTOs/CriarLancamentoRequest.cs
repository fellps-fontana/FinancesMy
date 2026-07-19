namespace MyFinances.DTOs;

public class CriarLancamentoRequest
{
    public required string Descricao { get; set; }

    public required decimal Valor { get; set; }

    public Guid? CategoriaId { get; set; }

    public required string Tipo { get; set; }

    public required DateOnly Data { get; set; }

    public required string Status { get; set; }
}
