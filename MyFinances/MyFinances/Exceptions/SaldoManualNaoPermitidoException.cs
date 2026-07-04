using MyFinances.Models;

namespace MyFinances.Exceptions;

public class SaldoManualNaoPermitidoException : Exception
{
    public Guid ContaId { get; }
    public OrigemConta Origem { get; }

    public SaldoManualNaoPermitidoException(Guid contaId, OrigemConta origem)
        : base(
            $"Nao e permitido atualizar saldo manual de uma conta com origem {origem}. " +
            "Apenas contas de origem Manual podem ter saldo atualizado manualmente.")
    {
        ContaId = contaId;
        Origem = origem;
    }
}
