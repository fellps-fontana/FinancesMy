namespace MyFinances.Exceptions;

public class RendaPrevistaNegativaException : Exception
{
    public decimal Valor { get; }

    public RendaPrevistaNegativaException(decimal valor)
        : base($"rendaPrevista nao pode ser negativa. Valor recebido: {valor}.")
    {
        Valor = valor;
    }
}
