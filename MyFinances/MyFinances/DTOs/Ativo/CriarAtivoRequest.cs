using MyFinances.Domain;

namespace MyFinances.DTOs.Ativo;

public class CriarAtivoRequest
{
    public required string Nome { get; set; }

    public TipoAtivo Tipo { get; set; }

    public required string Instituicao { get; set; }

    public decimal ValorInvestido { get; set; }

    public DateOnly DataCompra { get; set; }
}
