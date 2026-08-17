namespace MyFinances.Domain;

public enum TipoItemFluxoCaixa
{
    Lancamento,
    Transferencia
}

public static class TipoItemFluxoCaixaExtensions
{
    public static string ToStorageValue(this TipoItemFluxoCaixa tipo) => tipo switch
    {
        TipoItemFluxoCaixa.Lancamento => "LANCAMENTO",
        TipoItemFluxoCaixa.Transferencia => "TRANSFERENCIA",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo))
    };

    public static TipoItemFluxoCaixa FromStorageValue(string value) => value switch
    {
        "LANCAMENTO" => TipoItemFluxoCaixa.Lancamento,
        "TRANSFERENCIA" => TipoItemFluxoCaixa.Transferencia,
        _ => throw new ArgumentException($"Valor desconhecido para TipoItemFluxoCaixa: {value}", nameof(value))
    };
}
