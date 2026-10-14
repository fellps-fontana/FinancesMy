namespace MyFinances.Exceptions;

// saldo_manual nao e editavel com ativo ativo vinculado (itens 8.5 e 10). -> 422.
public class SaldoCalculadoNaoEditavelException : Exception
{
    public Guid ContaId { get; }

    public SaldoCalculadoNaoEditavelException(Guid contaId)
        : base($"Saldo da conta {contaId} e calculado a partir dos ativos vinculados e nao pode ser editado.")
    {
        ContaId = contaId;
    }
}
