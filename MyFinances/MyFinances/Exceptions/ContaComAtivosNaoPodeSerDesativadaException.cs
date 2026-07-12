namespace MyFinances.Exceptions;

public class ContaComAtivosNaoPodeSerDesativadaException : Exception
{
    public Guid ContaId { get; }

    public ContaComAtivosNaoPodeSerDesativadaException(Guid contaId)
        : base(
            $"Nao e permitido desativar a conta {contaId} enquanto ela possui ativos ativos. " +
            "Venda todos os ativos antes de desativar a conta.")
    {
        ContaId = contaId;
    }
}
