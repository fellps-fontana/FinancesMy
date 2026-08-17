namespace MyFinances.Domain;

public enum SubtipoConta
{
    Corrente,
    Poupanca,
    DinheiroFisico
}

public static class SubtipoContaExtensions
{
    public static string ToStorageValue(this SubtipoConta subtipo) => subtipo switch
    {
        SubtipoConta.Corrente => "CORRENTE",
        SubtipoConta.Poupanca => "POUPANCA",
        SubtipoConta.DinheiroFisico => "DINHEIRO_FISICO",
        _ => throw new ArgumentOutOfRangeException(nameof(subtipo))
    };

    public static SubtipoConta FromStorageValue(string value) => value switch
    {
        "CORRENTE" => SubtipoConta.Corrente,
        "POUPANCA" => SubtipoConta.Poupanca,
        "DINHEIRO_FISICO" => SubtipoConta.DinheiroFisico,
        _ => throw new ArgumentException($"Valor desconhecido para SubtipoConta: {value}", nameof(value))
    };
}
