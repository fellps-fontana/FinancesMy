namespace MyFinances.Exceptions;

public class CotacaoExternaIndisponibilException : Exception
{
    public CotacaoExternaIndisponibilException(string message) : base(message)
    {
    }

    public CotacaoExternaIndisponibilException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
