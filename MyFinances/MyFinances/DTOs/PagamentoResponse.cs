using MyFinances.Domain;

namespace MyFinances.DTOs;

public class PagamentoResponse
{
    public Guid Id { get; set; }

    public DateOnly Data { get; set; }

    public decimal Valor { get; set; }

    public Guid ContaOrigemId { get; set; }

    public Guid ContaDestinoId { get; set; }

    public Guid? FaturaId { get; set; }

    public string? Descricao { get; set; }

    public static PagamentoResponse FromTransferencia(Transferencia transferencia)
    {
        return new()
        {
            Id = transferencia.Id,
            Data = transferencia.Data,
            Valor = transferencia.Valor,
            ContaOrigemId = transferencia.ContaOrigemId,
            ContaDestinoId = transferencia.ContaDestinoId,
            FaturaId = transferencia.FaturaId,
            Descricao = transferencia.Descricao
        };
    }
}
