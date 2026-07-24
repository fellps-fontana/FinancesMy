using MyFinances.Domain;

namespace MyFinances.DTOs;

public class TransferenciaResponse
{
    public Guid Id { get; set; }

    public DateOnly Data { get; set; }

    public decimal Valor { get; set; }

    public Guid ContaOrigemId { get; set; }

    public Guid? ContaDestinoId { get; set; }

    public string? Descricao { get; set; }

    public static TransferenciaResponse FromTransferencia(Transferencia transferencia)
    {
        return new()
        {
            Id = transferencia.Id,
            Data = transferencia.Data,
            Valor = transferencia.Valor,
            ContaOrigemId = transferencia.ContaOrigemId,
            ContaDestinoId = transferencia.ContaDestinoId,
            Descricao = transferencia.Descricao
        };
    }
}
