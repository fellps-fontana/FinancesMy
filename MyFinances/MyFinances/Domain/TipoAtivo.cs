namespace MyFinances.Domain;

public enum TipoAtivo
{
    RendaFixa,
    RendaVariavel
}

public static class TipoAtivoExtensions
{
    public static string ToStorageValue(this TipoAtivo tipo) => tipo switch
    {
        TipoAtivo.RendaFixa => "RENDA_FIXA",
        TipoAtivo.RendaVariavel => "RENDA_VARIAVEL",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo))
    };

    public static TipoAtivo FromStorageValue(string value) => value switch
    {
        "RENDA_FIXA" => TipoAtivo.RendaFixa,
        "RENDA_VARIAVEL" => TipoAtivo.RendaVariavel,
        _ => throw new ArgumentException($"Valor desconhecido para TipoAtivo: {value}", nameof(value))
    };
}
