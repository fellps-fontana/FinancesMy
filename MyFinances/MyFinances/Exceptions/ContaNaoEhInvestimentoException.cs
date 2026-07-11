using MyFinances.Domain;

namespace MyFinances.Exceptions;

public class ContaNaoEhInvestimentoException : Exception
{
    public Guid ContaId { get; }
    public TipoConta TipoConta { get; }

    public ContaNaoEhInvestimentoException(Guid contaId, TipoConta tipoConta)
        : base($"Conta com ID {contaId} nao e do tipo Investimento. Tipo da conta: {tipoConta}.")
    {
        ContaId = contaId;
        TipoConta = tipoConta;
    }
}
