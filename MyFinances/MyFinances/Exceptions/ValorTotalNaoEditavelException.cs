namespace MyFinances.Exceptions;

public class ValorTotalNaoEditavelException : Exception
{
    public ValorTotalNaoEditavelException(string motivo)
        : base($"O valor_total nao pode ser editado: {motivo}")
    {
    }
}
