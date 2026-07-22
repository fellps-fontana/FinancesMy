using MyFinances.Domain;
using ContaFixaDomain = MyFinances.Domain.ContaFixa;

namespace MyFinances.DTOs.ContaFixa;

public class ContaFixaResponse
{
    public Guid Id { get; set; }

    public Guid ContaId { get; set; }

    public Guid? CategoriaId { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    public int DiaVencimento { get; set; }

    public bool Ativa { get; set; }

    public static ContaFixaResponse FromContaFixa(ContaFixaDomain contaFixa)
    {
        return new()
        {
            Id = contaFixa.Id,
            ContaId = contaFixa.ContaId,
            CategoriaId = contaFixa.CategoriaId,
            Descricao = contaFixa.Descricao,
            Valor = contaFixa.Valor,
            DiaVencimento = contaFixa.DiaVencimento,
            Ativa = contaFixa.Ativa
        };
    }
}
