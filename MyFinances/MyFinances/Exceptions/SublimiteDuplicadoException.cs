namespace MyFinances.Exceptions;

public class SublimiteDuplicadoException : Exception
{
    public Guid CategoriaId { get; }

    public SublimiteDuplicadoException(Guid categoriaId)
        : base($"A categoria {categoriaId} aparece mais de uma vez nos sublimites do plano.")
    {
        CategoriaId = categoriaId;
    }
}
