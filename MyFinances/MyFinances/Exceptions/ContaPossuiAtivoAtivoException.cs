namespace MyFinances.Exceptions;

// Desativar conta com ativo ativo vinculado e recusado (item 8.5). -> 409.
public class ContaPossuiAtivoAtivoException : Exception
{
    public Guid ContaId { get; }

    public ContaPossuiAtivoAtivoException(Guid contaId)
        : base($"Conta {contaId} possui ativo ativo vinculado. Mova ou desative os ativos antes.")
    {
        ContaId = contaId;
    }
}
