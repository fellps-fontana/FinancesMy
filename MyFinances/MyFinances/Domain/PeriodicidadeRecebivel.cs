namespace MyFinances.Domain;

public enum PeriodicidadeRecebivel
{
    Mensal,
    Anual,
    Semanal
}

public static class PeriodicidadeRecebivelExtensions
{
    public static string ToStorageValue(this PeriodicidadeRecebivel periodicidade) => periodicidade switch
    {
        PeriodicidadeRecebivel.Mensal => "MENSAL",
        PeriodicidadeRecebivel.Anual => "ANUAL",
        PeriodicidadeRecebivel.Semanal => "SEMANAL",
        _ => throw new ArgumentOutOfRangeException(nameof(periodicidade))
    };

    public static PeriodicidadeRecebivel FromStorageValue(string value) => value switch
    {
        "MENSAL" => PeriodicidadeRecebivel.Mensal,
        "ANUAL" => PeriodicidadeRecebivel.Anual,
        "SEMANAL" => PeriodicidadeRecebivel.Semanal,
        _ => throw new ArgumentException($"Valor desconhecido para PeriodicidadeRecebivel: {value}", nameof(value))
    };
}
