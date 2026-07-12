namespace MyFinances.Domain;

public enum TipoCategoria
{
    Despesa,
    Receita
}

public static class TipoCategoriaExtensions
{
    public static string ToStorageValue(this TipoCategoria tipo) => tipo switch
    {
        TipoCategoria.Despesa => "DESPESA",
        TipoCategoria.Receita => "RECEITA",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo))
    };

    public static TipoCategoria FromStorageValue(string value) => value switch
    {
        "DESPESA" => TipoCategoria.Despesa,
        "RECEITA" => TipoCategoria.Receita,
        _ => throw new ArgumentException($"Valor desconhecido para TipoCategoria: {value}", nameof(value))
    };
}
