namespace MyFinances.Domain;

public enum StatusContaReceber
{
    Pendente,
    Parcial,
    Recebido
}

public static class StatusContaReceberExtensions
{
    public static string ToStorageValue(this StatusContaReceber status) => status switch
    {
        StatusContaReceber.Pendente => "PENDENTE",
        StatusContaReceber.Parcial => "PARCIAL",
        StatusContaReceber.Recebido => "RECEBIDO",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static StatusContaReceber FromStorageValue(string value) => value switch
    {
        "PENDENTE" => StatusContaReceber.Pendente,
        "PARCIAL" => StatusContaReceber.Parcial,
        "RECEBIDO" => StatusContaReceber.Recebido,
        _ => throw new ArgumentException($"Valor desconhecido para StatusContaReceber: {value}", nameof(value))
    };
}
