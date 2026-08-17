namespace MyFinances.Domain;

public enum TipoRendimento
{
    Dividendo,
    Valorizacao
}

public static class TipoRendimentoExtensions
{
    public static string ToStorageValue(this TipoRendimento tipo) => tipo switch
    {
        TipoRendimento.Dividendo => "DIVIDENDO",
        TipoRendimento.Valorizacao => "VALORIZACAO",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo))
    };

    public static TipoRendimento FromStorageValue(string value) => value switch
    {
        "DIVIDENDO" => TipoRendimento.Dividendo,
        "VALORIZACAO" => TipoRendimento.Valorizacao,
        _ => throw new ArgumentException($"Valor desconhecido para TipoRendimento: {value}", nameof(value))
    };
}
