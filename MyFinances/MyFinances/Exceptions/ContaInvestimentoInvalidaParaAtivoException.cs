namespace MyFinances.Exceptions;

// Conta do ativo nao e INVESTIMENTO ou esta inativa (item 8.5). -> 400 no controller.
public class ContaInvestimentoInvalidaParaAtivoException : Exception
{
    public Guid ContaId { get; }

    public ContaInvestimentoInvalidaParaAtivoException(Guid contaId)
        : base($"Conta {contaId} nao e uma conta de investimento ativa; so ela pode receber ativos.")
    {
        ContaId = contaId;
    }
}
