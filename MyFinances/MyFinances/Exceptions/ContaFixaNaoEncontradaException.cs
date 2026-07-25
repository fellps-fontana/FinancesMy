namespace MyFinances.Exceptions;

public class ContaFixaNaoEncontradaException : Exception
{
    public Guid ContaFixaId { get; }

    public ContaFixaNaoEncontradaException(Guid contaFixaId)
        : base($"Conta fixa com ID {contaFixaId} nao encontrada.")
    {
        ContaFixaId = contaFixaId;
    }
}
