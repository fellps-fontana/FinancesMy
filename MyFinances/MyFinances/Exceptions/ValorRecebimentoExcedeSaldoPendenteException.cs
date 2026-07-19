namespace MyFinances.Exceptions;

public class ValorRecebimentoExcedeSaldoPendenteException : Exception
{
    public decimal ValorRecebimento { get; }
    public decimal SaldoPendente { get; }

    public ValorRecebimentoExcedeSaldoPendenteException(decimal valorRecebimento, decimal saldoPendente)
        : base($"Valor do recebimento ({valorRecebimento}) excede o saldo pendente ({saldoPendente}).")
    {
        ValorRecebimento = valorRecebimento;
        SaldoPendente = saldoPendente;
    }
}
