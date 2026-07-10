namespace MyFinances.Exceptions;

public class DeParaCategoriaNaoEncontradoException : Exception
{
    public Guid DeParaCategoriaId { get; }

    public DeParaCategoriaNaoEncontradoException(Guid deParaCategoriaId)
        : base($"De-para de categoria com ID {deParaCategoriaId} nao encontrado.")
    {
        DeParaCategoriaId = deParaCategoriaId;
    }
}
