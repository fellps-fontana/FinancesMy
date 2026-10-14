namespace MyFinances.Exceptions;

// Subtipo informado nao reconhecido (item 10.1/10.2). -> 400.
public class SubtipoContaInvalidoException : Exception
{
    public string Subtipo { get; }

    public SubtipoContaInvalidoException(string subtipo)
        : base($"Subtipo '{subtipo}' nao e valido.")
    {
        Subtipo = subtipo;
    }
}
