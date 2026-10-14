using MyFinances.Domain;

namespace MyFinances.DTOs;

public class ParcelaElegivelResponse
{
    public Guid LancamentoId { get; set; }

    public int ParcelaNumero { get; set; }

    public int TotalParcelas { get; set; }

    public decimal Valor { get; set; }

    public DateOnly Data { get; set; }

    public Guid FaturaId { get; set; }

    public DateOnly FaturaDataVencimento { get; set; }

    // totalParcelas vem da CompraParcelada. Exige Lancamento.Fatura carregado.
    public static ParcelaElegivelResponse FromLancamento(Lancamento lancamento, int totalParcelas)
    {
        return new()
        {
            LancamentoId = lancamento.Id,
            ParcelaNumero = lancamento.ParcelaNumero!.Value,
            TotalParcelas = totalParcelas,
            Valor = lancamento.Valor,
            Data = lancamento.Data,
            FaturaId = lancamento.FaturaId!.Value,
            FaturaDataVencimento = lancamento.Fatura!.DataVencimento
        };
    }
}
