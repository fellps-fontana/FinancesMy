namespace MyFinances.Exceptions;

public class QuantidadeVendaInvalidaException : Exception
{
    public Guid AtivoId { get; }
    public decimal QuantidadeVenda { get; }
    public decimal QuantidadeDisponivel { get; }

    public QuantidadeVendaInvalidaException(Guid ativoId, decimal quantidadeVenda, decimal quantidadeDisponivel)
        : base($"Quantidade de venda ({quantidadeVenda}) nao pode ser maior que a quantidade disponivel ({quantidadeDisponivel}) do ativo com ID {ativoId}.")
    {
        AtivoId = ativoId;
        QuantidadeVenda = quantidadeVenda;
        QuantidadeDisponivel = quantidadeDisponivel;
    }
}
