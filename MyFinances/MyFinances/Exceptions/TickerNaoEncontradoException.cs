namespace MyFinances.Exceptions;

public class TickerNaoEncontradoException : Exception
{
    public TickerNaoEncontradoException(string message) : base(message)
    {
    }

    public TickerNaoEncontradoException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
