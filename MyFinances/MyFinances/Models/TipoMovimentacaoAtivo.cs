namespace MyFinances.Models;

public enum TipoMovimentacaoAtivo
{
    Compra,
    Venda
}

public static class TipoMovimentacaoAtivoExtensions
{
    public static string ToStorageValue(this TipoMovimentacaoAtivo tipo) => tipo switch
    {
        TipoMovimentacaoAtivo.Compra => "COMPRA",
        TipoMovimentacaoAtivo.Venda => "VENDA",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo))
    };

    public static TipoMovimentacaoAtivo FromStorageValue(string value) => value switch
    {
        "COMPRA" => TipoMovimentacaoAtivo.Compra,
        "VENDA" => TipoMovimentacaoAtivo.Venda,
        _ => throw new ArgumentException($"Valor desconhecido para TipoMovimentacaoAtivo: {value}", nameof(value))
    };
}
