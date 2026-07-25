using MyFinances.Domain;
using LimiteGastoDomain = MyFinances.Domain.LimiteGasto;

namespace MyFinances.DTOs.LimiteGasto;

public class LimiteGastoResponse
{
    public Guid Id { get; set; }

    public Guid CategoriaId { get; set; }

    public string CategoriaNome { get; set; } = string.Empty;

    public decimal ValorLimite { get; set; }

    public string Periodo { get; set; } = string.Empty;

    public static LimiteGastoResponse FromLimiteGasto(LimiteGastoDomain limiteGasto)
    {
        return new()
        {
            Id = limiteGasto.Id,
            CategoriaId = limiteGasto.CategoriaId,
            CategoriaNome = limiteGasto.Categoria?.Nome ?? string.Empty,
            ValorLimite = limiteGasto.ValorLimite,
            Periodo = limiteGasto.Periodo.ToStorageValue()
        };
    }
}
