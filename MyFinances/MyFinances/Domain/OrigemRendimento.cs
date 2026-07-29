namespace MyFinances.Domain;

public enum OrigemRendimento
{
    Manual,
    Automatico
}

public static class OrigemRendimentoExtensions
{
    public static string ToStorageValue(this OrigemRendimento origem) => origem switch
    {
        OrigemRendimento.Manual => "MANUAL",
        OrigemRendimento.Automatico => "AUTOMATICO",
        _ => throw new ArgumentOutOfRangeException(nameof(origem))
    };

    public static OrigemRendimento FromStorageValue(string value) => value switch
    {
        "MANUAL" => OrigemRendimento.Manual,
        "AUTOMATICO" => OrigemRendimento.Automatico,
        _ => throw new ArgumentException($"Valor desconhecido para OrigemRendimento: {value}", nameof(value))
    };
}
