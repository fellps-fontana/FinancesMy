namespace MyFinances.Exceptions;

public class ContaReceberNaoEncontradaException : Exception
{
    public Guid ContaReceberId { get; }

    public ContaReceberNaoEncontradaException(Guid contaReceberId)
        : base($"Conta a receber com ID {contaReceberId} nao encontrada.")
    {
        ContaReceberId = contaReceberId;
    }
}
