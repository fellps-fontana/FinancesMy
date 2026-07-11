namespace MyFinances.Exceptions;

public class CategoriaNaoEncontradaException : Exception
{
    public Guid CategoriaId { get; }

    public CategoriaNaoEncontradaException(Guid categoriaId)
        : base($"Categoria com ID {categoriaId} nao encontrada.")
    {
        CategoriaId = categoriaId;
    }
}
