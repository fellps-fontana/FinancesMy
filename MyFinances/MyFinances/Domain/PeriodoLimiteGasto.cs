namespace MyFinances.Domain;

public enum PeriodoLimiteGasto
{
    Mensal
}

public static class PeriodoLimiteGastoExtensions
{
    public static string ToStorageValue(this PeriodoLimiteGasto periodo) => periodo switch
    {
        PeriodoLimiteGasto.Mensal => "MENSAL",
        _ => throw new ArgumentOutOfRangeException(nameof(periodo))
    };

    public static PeriodoLimiteGasto FromStorageValue(string value) => value switch
    {
        "MENSAL" => PeriodoLimiteGasto.Mensal,
        _ => throw new ArgumentException($"Valor desconhecido para PeriodoLimiteGasto: {value}", nameof(value))
    };
}
