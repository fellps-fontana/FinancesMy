namespace MyFinances.Exceptions;

public class SomaPercentuaisExcedeCemException : Exception
{
    // null = categorias de primeiro nivel; preenchido = subcategorias desse pai.
    public Guid? CategoriaPaiId { get; }
    public decimal Soma { get; }

    public SomaPercentuaisExcedeCemException(Guid? categoriaPaiId, decimal soma)
        : base(categoriaPaiId == null
            ? $"A soma dos percentuais das categorias de primeiro nivel excede 100. Soma: {soma}."
            : $"A soma dos percentuais das subcategorias da categoria {categoriaPaiId} excede 100. Soma: {soma}.")
    {
        CategoriaPaiId = categoriaPaiId;
        Soma = soma;
    }
}
