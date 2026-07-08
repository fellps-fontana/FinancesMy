namespace MyFinances.Exceptions;

public class ContaNaoEncontradaException : Exception
{
    public Guid ContaId { get; }

    public ContaNaoEncontradaException(Guid contaId)
        : base($"Conta com ID {contaId} nao encontrada.")
    {
        ContaId = contaId;
    }
}
