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
}
