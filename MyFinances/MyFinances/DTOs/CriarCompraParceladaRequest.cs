namespace MyFinances.DTOs;

public class CriarCompraParceladaRequest
{
    public required string Descricao { get; set; }

    public required decimal ValorTotal { get; set; }

    public required int QuantidadeParcelas { get; set; }

    public Guid? CategoriaId { get; set; }

    public required DateOnly DataCompra { get; set; }
}
