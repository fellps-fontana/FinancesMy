namespace MyFinances.Domain;

public static class TipoContaConstants
{
    public const string Banco = "BANCO";
    public const string Cartao = "CARTAO";
    public const string Investimento = "INVESTIMENTO";

    public static bool EValido(string? tipo)
    {
        return tipo switch
        {
            Banco or Cartao or Investimento => true,
            _ => false
        };
    }
}

public static class OrigemConstants
{
    public const string OpenFinance = "OPEN_FINANCE";
    public const string Manual = "MANUAL";
}
