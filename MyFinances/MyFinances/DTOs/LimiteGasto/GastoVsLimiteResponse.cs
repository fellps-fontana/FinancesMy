using MyFinances.Domain;
using LimiteGastoDomain = MyFinances.Domain.LimiteGasto;

namespace MyFinances.DTOs.LimiteGasto;

public class GastoVsLimiteResponse
{
    public Guid CategoriaId { get; set; }

    public string CategoriaNome { get; set; } = string.Empty;

    public decimal ValorLimite { get; set; }

    public decimal GastoRealizado { get; set; }

    public decimal PercentualUtilizado { get; set; }

    public bool Estourado { get; set; }

    public static GastoVsLimiteResponse FromLimiteEStatus(LimiteGastoDomain limiteGasto, LimiteGastoStatus status)
    {
        return new()
        {
            CategoriaId = limiteGasto.CategoriaId,
            CategoriaNome = limiteGasto.Categoria?.Nome ?? string.Empty,
            ValorLimite = status.ValorLimite,
            GastoRealizado = status.GastoRealizado,
            PercentualUtilizado = status.PercentualUtilizado,
            Estourado = status.Estourado
        };
    }
}
