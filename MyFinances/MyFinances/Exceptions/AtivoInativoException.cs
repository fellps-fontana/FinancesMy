namespace MyFinances.Exceptions;

public class AtivoInativoException : Exception
{
    public Guid AtivoId { get; }

    public AtivoInativoException(Guid ativoId)
        : base($"Ativo com ID {ativoId} esta inativo.")
    {
        AtivoId = ativoId;
    }
}
