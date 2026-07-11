namespace MyFinances.Exceptions;

public class AtivoNaoEncontradoException : Exception
{
    public Guid AtivoId { get; }

    public AtivoNaoEncontradoException(Guid ativoId)
        : base($"Ativo com ID {ativoId} nao encontrado ou nao esta ativo.")
    {
        AtivoId = ativoId;
    }
}
