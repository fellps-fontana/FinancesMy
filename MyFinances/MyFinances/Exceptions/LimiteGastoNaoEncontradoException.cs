namespace MyFinances.Exceptions;

public class LimiteGastoNaoEncontradoException : Exception
{
    public Guid CategoriaId { get; }

    public LimiteGastoNaoEncontradoException(Guid categoriaId)
        : base($"Limite de gasto nao encontrado para a categoria com ID {categoriaId}.")
    {
        CategoriaId = categoriaId;
    }
}
