namespace MyFinances.Domain;

public enum StatusLancamento
{
    Pendente,
    Sugerido,
    Pago
}

public static class StatusLancamentoExtensions
{
    public static string ToStorageValue(this StatusLancamento status) => status switch
    {
        StatusLancamento.Pendente => "PENDENTE",
        StatusLancamento.Sugerido => "SUGERIDO",
        StatusLancamento.Pago => "PAGO",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static StatusLancamento FromStorageValue(string value) => value switch
    {
        "PENDENTE" => StatusLancamento.Pendente,
        "SUGERIDO" => StatusLancamento.Sugerido,
        "PAGO" => StatusLancamento.Pago,
        _ => throw new ArgumentException($"Valor desconhecido para StatusLancamento: {value}", nameof(value))
    };
}
