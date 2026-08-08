namespace MyFinances.Domain;

public enum PeriodicidadeContaFixa
{
    Mensal,
    Anual
}

public static class PeriodicidadeContaFixaExtensions
{
    public static string ToStorageValue(this PeriodicidadeContaFixa periodicidade) => periodicidade switch
    {
        PeriodicidadeContaFixa.Mensal => "MENSAL",
        PeriodicidadeContaFixa.Anual => "ANUAL",
        _ => throw new ArgumentOutOfRangeException(nameof(periodicidade))
    };

    public static PeriodicidadeContaFixa FromStorageValue(string value) => value switch
    {
        "MENSAL" => PeriodicidadeContaFixa.Mensal,
        "ANUAL" => PeriodicidadeContaFixa.Anual,
        _ => throw new ArgumentException($"Valor desconhecido para PeriodicidadeContaFixa: {value}", nameof(value))
    };
}
