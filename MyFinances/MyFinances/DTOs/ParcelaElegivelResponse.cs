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

    // Exige Lancamento.Fatura e Lancamento.CompraParcelada carregados.
    public static ParcelaElegivelResponse FromLancamento(Lancamento lancamento)
    {
        return new()
        {
            LancamentoId = lancamento.Id,
            ParcelaNumero = lancamento.ParcelaNumero!.Value,
            TotalParcelas = lancamento.CompraParcelada!.QuantidadeParcelas,
            Valor = lancamento.Valor,
            Data = lancamento.Data,
            FaturaId = lancamento.FaturaId!.Value,
            FaturaDataVencimento = lancamento.Fatura!.DataVencimento
        };
    }
}
