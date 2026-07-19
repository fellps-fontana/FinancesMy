namespace MyFinances.Domain;

public enum TipoContaReceber
{
    Recebivel,
    Emprestimo
}

public static class TipoContaReceberExtensions
{
    public static string ToStorageValue(this TipoContaReceber tipo) => tipo switch
    {
        TipoContaReceber.Recebivel => "RECEBIVEL",
        TipoContaReceber.Emprestimo => "EMPRESTIMO",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo))
    };

    public static TipoContaReceber FromStorageValue(string value) => value switch
    {
        "RECEBIVEL" => TipoContaReceber.Recebivel,
        "EMPRESTIMO" => TipoContaReceber.Emprestimo,
        _ => throw new ArgumentException($"Valor desconhecido para TipoContaReceber: {value}", nameof(value))
    };
}
