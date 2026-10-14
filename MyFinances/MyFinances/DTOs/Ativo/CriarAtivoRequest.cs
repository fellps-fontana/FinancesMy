using MyFinances.Domain;

namespace MyFinances.DTOs.Ativo;

public class CriarAtivoRequest
{
    public required string Nome { get; set; }

    public TipoAtivo Tipo { get; set; }

    // Obrigatorio em ativo novo (item 8.5). Guid.Empty = ausente -> 400.
    public Guid ContaId { get; set; }

    public decimal Quantidade { get; set; }

    public decimal PrecoUnitario { get; set; }

    public DateOnly DataCompra { get; set; }
}
