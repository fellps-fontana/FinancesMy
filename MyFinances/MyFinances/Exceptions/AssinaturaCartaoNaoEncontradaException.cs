namespace MyFinances.Exceptions;

public class AssinaturaCartaoNaoEncontradaException : Exception
{
    public Guid AssinaturaCartaoId { get; }

    public AssinaturaCartaoNaoEncontradaException(Guid id)
        : base($"Assinatura de cartao com ID '{id}' nao foi encontrada.")
    {
        AssinaturaCartaoId = id;
    }
}
