namespace MyFinances.DTOs;

public class EditarCompraRequest
{
    public Guid? CategoriaId { get; set; }
    public required string Descricao { get; set; }
    public required decimal Valor { get; set; }
    public required DateOnly Data { get; set; }
}
