using MyFinances.Domain;
using LancamentoDomain = MyFinances.Domain.Lancamento;

namespace MyFinances.DTOs.ContaReceber;

public class RecebimentoResponse
{
    public Guid Id { get; set; }

    public decimal Valor { get; set; }

    public DateOnly Data { get; set; }

    public Guid ContaId { get; set; }

    public Guid? CategoriaId { get; set; }

    public Guid? ContaReceberId { get; set; }

    public static RecebimentoResponse FromLancamento(LancamentoDomain lancamento)
    {
        return new()
        {
            Id = lancamento.Id,
            Valor = lancamento.Valor,
            Data = lancamento.Data,
            ContaId = lancamento.ContaId,
            CategoriaId = lancamento.CategoriaId,
            ContaReceberId = lancamento.ContaReceberId
        };
    }
}
