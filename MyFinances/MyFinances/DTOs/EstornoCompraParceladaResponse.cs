using MyFinances.Domain;

namespace MyFinances.DTOs;

public class EstornoCompraParceladaResponse
{
    public List<CompraResponse> ParcelasCanceladas { get; set; } = new();

    public List<EstornoResponse> EstornosRetroativos { get; set; } = new();

    public static EstornoCompraParceladaResponse FromDomain(
        IReadOnlyList<Lancamento> parcelasCanceladas,
        IReadOnlyList<Lancamento> estornosRetroativos)
    {
        return new()
        {
            ParcelasCanceladas = parcelasCanceladas.Select(CompraResponse.FromLancamento).ToList(),
            EstornosRetroativos = estornosRetroativos.Select(EstornoResponse.FromLancamento).ToList()
        };
    }
}
