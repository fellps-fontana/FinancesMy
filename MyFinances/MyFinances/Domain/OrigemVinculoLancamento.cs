namespace MyFinances.Domain;

// Origem (dono) de um lancamento, derivada dos vinculos. Regra-de-negocio
// item 18. Avulso e a unica origem editavel/excluivel pelo endpoint de
// lancamento manual; todas as outras sao editadas na origem.
public enum OrigemVinculoLancamento
{
    Avulso,
    ContaFixa,
    CompraParcelada,
    Cartao,
    ContaReceber,
    Transferencia,
    Conciliado,
    NaoManual
}

public static class OrigemVinculoLancamentoExtensions
{
    public static string ToStorageValue(this OrigemVinculoLancamento origem) => origem switch
    {
        OrigemVinculoLancamento.Avulso => "AVULSO",
        OrigemVinculoLancamento.ContaFixa => "CONTA_FIXA",
        OrigemVinculoLancamento.CompraParcelada => "COMPRA_PARCELADA",
        OrigemVinculoLancamento.Cartao => "CARTAO",
        OrigemVinculoLancamento.ContaReceber => "CONTA_RECEBER",
        OrigemVinculoLancamento.Transferencia => "TRANSFERENCIA",
        OrigemVinculoLancamento.Conciliado => "CONCILIADO",
        OrigemVinculoLancamento.NaoManual => "NAO_MANUAL",
        _ => throw new ArgumentOutOfRangeException(nameof(origem))
    };
}
