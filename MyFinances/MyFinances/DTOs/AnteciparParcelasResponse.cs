using MyFinances.Domain;

namespace MyFinances.DTOs;

public class AnteciparParcelasResponse
{
    public Guid CompraParceladaId { get; set; }

    public Guid FaturaDestinoId { get; set; }

    public int QuantidadeAntecipada { get; set; }

    public decimal ValorAntecipado { get; set; }

    public IReadOnlyList<CompraResponse> Parcelas { get; set; } = Array.Empty<CompraResponse>();

    public static AnteciparParcelasResponse FromDomain(
        Guid compraParceladaId,
        Fatura faturaDestino,
        IReadOnlyList<Lancamento> parcelasMovidas)
    {
        return new()
        {
            CompraParceladaId = compraParceladaId,
            FaturaDestinoId = faturaDestino.Id,
            QuantidadeAntecipada = parcelasMovidas.Count,
            ValorAntecipado = parcelasMovidas.Sum(l => l.Valor),
            Parcelas = parcelasMovidas.Select(CompraResponse.FromLancamento).ToList()
        };
    }
}
