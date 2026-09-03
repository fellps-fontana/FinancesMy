namespace MyFinances.Exceptions;

public class RecebivelRecorrenteNaoEncontradoException : Exception
{
    public RecebivelRecorrenteNaoEncontradoException(Guid id)
        : base($"Recebivel recorrente {id} nao encontrado.")
    {
    }
}
