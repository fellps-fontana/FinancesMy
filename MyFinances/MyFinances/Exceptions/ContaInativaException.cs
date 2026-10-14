namespace MyFinances.Exceptions;

// Editar conta inativa e recusado (item 10.2). -> 409.
public class ContaInativaException : Exception
{
    public Guid ContaId { get; }

    public ContaInativaException(Guid contaId)
        : base($"Conta {contaId} esta inativa e nao pode ser editada.")
    {
        ContaId = contaId;
    }
}
