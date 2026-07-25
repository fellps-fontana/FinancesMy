namespace MyFinances.DTOs;

public class EstornarCompraParceladaRequest
{
    public required string Motivo { get; set; }

    // Data do(s) lancamento(s) de estorno gerado(s) nas faturas ja pagas
    // (mesmo papel do campo Data em CriarEstornoRequest). Parcela apenas
    // cancelada (fatura ainda nao paga) nao usa este campo -- e removida,
    // nao gera lancamento novo.
    public required DateOnly Data { get; set; }
}
