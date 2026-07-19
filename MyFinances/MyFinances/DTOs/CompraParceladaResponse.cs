using MyFinances.Domain;

namespace MyFinances.DTOs;

public class CompraParceladaResponse
{
    public Guid Id { get; set; }

    public Guid ContaId { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public decimal ValorTotal { get; set; }

    public int QuantidadeParcelas { get; set; }

    public DateOnly DataCompra { get; set; }

    public List<CompraResponse> Parcelas { get; set; } = new();

    public static CompraParceladaResponse FromDomain(CompraParcelada compraParcelada, Guid contaId)
    {
        var parcelas = compraParcelada.Lancamentos
            .OrderBy(l => l.ParcelaNumero)
            .Select(CompraResponse.FromLancamento)
            .ToList();

        return new()
        {
            Id = compraParcelada.Id,
            ContaId = contaId,
            Descricao = compraParcelada.Descricao,
            ValorTotal = compraParcelada.ValorTotal,
            QuantidadeParcelas = compraParcelada.QuantidadeParcelas,
            DataCompra = compraParcelada.DataCompra,
            Parcelas = parcelas
        };
    }
}
