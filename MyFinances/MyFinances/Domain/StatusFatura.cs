namespace MyFinances.Domain;

public enum StatusFatura
{
    Aberta,
    Fechada,
    Paga,
    Futura
}

public static class StatusFaturaExtensions
{
    public static string ToStorageValue(this StatusFatura status) => status switch
    {
        StatusFatura.Aberta => "ABERTA",
        StatusFatura.Fechada => "FECHADA",
        StatusFatura.Paga => "PAGA",
        StatusFatura.Futura => "FUTURA",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static StatusFatura FromStorageValue(string value) => value switch
    {
        "ABERTA" => StatusFatura.Aberta,
        "FECHADA" => StatusFatura.Fechada,
        "PAGA" => StatusFatura.Paga,
        "FUTURA" => StatusFatura.Futura,
        _ => throw new ArgumentException($"Valor desconhecido para StatusFatura: {value}", nameof(value))
    };
}
