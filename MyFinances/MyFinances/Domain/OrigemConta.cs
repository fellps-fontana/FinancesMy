namespace MyFinances.Domain;

public enum OrigemConta
{
    Manual,
    OpenFinance
}

public static class OrigemContaExtensions
{
    public static string ToStorageValue(this OrigemConta origem) => origem switch
    {
        OrigemConta.Manual => "MANUAL",
        OrigemConta.OpenFinance => "OPEN_FINANCE",
        _ => throw new ArgumentOutOfRangeException(nameof(origem))
    };

    public static OrigemConta FromStorageValue(string value) => value switch
    {
        "MANUAL" => OrigemConta.Manual,
        "OPEN_FINANCE" => OrigemConta.OpenFinance,
        _ => throw new ArgumentException($"Valor desconhecido para OrigemConta: {value}", nameof(value))
    };
}
