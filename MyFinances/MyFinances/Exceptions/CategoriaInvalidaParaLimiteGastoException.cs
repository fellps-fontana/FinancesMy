namespace MyFinances.Exceptions;

public class CategoriaInvalidaParaLimiteGastoException : Exception
{
    public Guid CategoriaId { get; }

    public CategoriaInvalidaParaLimiteGastoException(Guid categoriaId)
        : base($"A categoria com ID {categoriaId} nao e valida para limite de gasto. Apenas categorias de tipo Despesa nao arquivada podem ter limite.")
    {
        CategoriaId = categoriaId;
    }
}
