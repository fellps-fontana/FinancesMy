namespace MyFinances.Exceptions;

public class CategoriaInvalidaParaSublimiteException : Exception
{
    public Guid CategoriaId { get; }

    public CategoriaInvalidaParaSublimiteException(Guid categoriaId)
        : base($"A categoria {categoriaId} nao pode ter sublimite. Apenas categorias de despesa nao arquivadas.")
    {
        CategoriaId = categoriaId;
    }
}
