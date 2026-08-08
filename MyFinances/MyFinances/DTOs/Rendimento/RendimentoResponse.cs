using MyFinances.Domain;
using RendimentoDomain = MyFinances.Domain.Rendimento;

namespace MyFinances.DTOs.Rendimento;

public class RendimentoResponse
{
    public Guid Id { get; set; }

    public Guid AtivoId { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public string Origem { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    public DateOnly Data { get; set; }

    public static RendimentoResponse FromRendimento(RendimentoDomain rendimento)
    {
        return new RendimentoResponse
        {
            Id = rendimento.Id,
            AtivoId = rendimento.AtivoId,
            Tipo = rendimento.Tipo.ToStorageValue(),
            Origem = rendimento.Origem.ToStorageValue(),
            Valor = rendimento.Valor,
            Data = rendimento.Data
        };
    }
}
