namespace MyFinances.Models;

public enum TipoConta
{
    Banco,
    Cartao,
    Investimento
}

public static class TipoContaExtensions
{
    public static string ToStorageValue(this TipoConta tipo) => tipo switch
    {
        TipoConta.Banco => "BANCO",
        TipoConta.Cartao => "CARTAO",
        TipoConta.Investimento => "INVESTIMENTO",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo))
    };

    public static TipoConta FromStorageValue(string value) => value switch
    {
        "BANCO" => TipoConta.Banco,
        "CARTAO" => TipoConta.Cartao,
        "INVESTIMENTO" => TipoConta.Investimento,
        _ => throw new ArgumentException($"Valor desconhecido para TipoConta: {value}", nameof(value))
    };
}
