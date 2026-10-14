namespace MyFinances.Exceptions;

public class ContaReceberRecorrenteNaoEditavelException : Exception
{
    public ContaReceberRecorrenteNaoEditavelException(Guid contaReceberId)
        : base($"A conta a receber {contaReceberId} e ocorrencia de recebivel recorrente. Edite o molde.")
    {
    }
}
