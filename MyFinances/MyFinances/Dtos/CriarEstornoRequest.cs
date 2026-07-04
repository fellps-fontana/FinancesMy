namespace MyFinances.Dtos;

public class CriarEstornoRequest
{
    public Guid? CategoriaId { get; set; }
    public required string Descricao { get; set; }
    public required decimal Valor { get; set; } // Cliente manda positivo, service converte para negativo
    public required DateOnly Data { get; set; }
}
