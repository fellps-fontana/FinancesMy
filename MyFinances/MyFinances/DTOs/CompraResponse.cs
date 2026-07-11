using MyFinances.Domain;

namespace MyFinances.DTOs;

public class CompraResponse
{
    public Guid Id { get; set; }

    public Guid ContaId { get; set; }

    public Guid? CategoriaId { get; set; }

    public string? Descricao { get; set; }

    public decimal Valor { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public DateOnly Data { get; set; }

    public string Status { get; set; } = string.Empty;

    public Guid? FaturaId { get; set; }

    public static CompraResponse FromLancamento(Lancamento lancamento)
    {
        return new()
        {
            Id = lancamento.Id,
            ContaId = lancamento.ContaId,
            CategoriaId = lancamento.CategoriaId,
            Descricao = lancamento.Descricao,
            Valor = lancamento.Valor,
            Tipo = lancamento.Tipo.ToStorageValue(),
            Data = lancamento.Data,
            Status = lancamento.Status.ToStorageValue(),
            FaturaId = lancamento.FaturaId
        };
    }
}
