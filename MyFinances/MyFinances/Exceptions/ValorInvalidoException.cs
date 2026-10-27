namespace MyFinances.Exceptions;

public class ValorInvalidoException : Exception
{
    public string NomeCampo { get; }
    public decimal Valor { get; }

    public ValorInvalidoException(string nomeCampo, decimal valor)
        : base($"{nomeCampo} deve ser maior que zero. Valor recebido: {valor}.")
    {
        NomeCampo = nomeCampo;
        Valor = valor;
    }

    public ValorInvalidoException(string nomeCampo, decimal valor, int minimo, int maximo)
        : base($"{nomeCampo} deve estar entre {minimo} e {maximo}. Valor recebido: {valor}.")
    {
        NomeCampo = nomeCampo;
        Valor = valor;
    }

    public ValorInvalidoException(string nomeCampo, decimal valor, decimal minimo, decimal maximo)
        : base($"{nomeCampo} deve estar entre {minimo} e {maximo}. Valor recebido: {valor}.")
    {
        NomeCampo = nomeCampo;
        Valor = valor;
    }
}
