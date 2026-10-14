namespace MyFinances.Exceptions;

public class CategoriaNaoEhPrimeiroNivelException : Exception
{
    public Guid CategoriaId { get; }

    public CategoriaNaoEhPrimeiroNivelException(Guid categoriaId)
        : base($"Categoria {categoriaId} nao e uma categoria de despesa de primeiro nivel.")
    {
        CategoriaId = categoriaId;
    }
}
