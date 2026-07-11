namespace MyFinances.Models;

public enum TipoLancamento
{
    Debit,
    Credit
}

public static class TipoLancamentoExtensions
{
    public static string ToStorageValue(this TipoLancamento tipo) => tipo switch
    {
        TipoLancamento.Debit => "DEBIT",
        TipoLancamento.Credit => "CREDIT",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo))
    };

    public static TipoLancamento FromStorageValue(string value) => value switch
    {
        "DEBIT" => TipoLancamento.Debit,
        "CREDIT" => TipoLancamento.Credit,
        _ => throw new ArgumentException($"Valor desconhecido para TipoLancamento: {value}", nameof(value))
    };
}
