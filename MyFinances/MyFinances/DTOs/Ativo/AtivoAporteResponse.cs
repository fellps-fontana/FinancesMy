using MyFinances.Domain;
using AtivoAporteDomain = MyFinances.Domain.AtivoAporte;

namespace MyFinances.DTOs.Ativo;

public class AtivoAporteResponse
{
    public Guid Id { get; set; }

    public Guid AtivoId { get; set; }

    public DateOnly Data { get; set; }

    public decimal Quantidade { get; set; }

    public decimal PrecoUnitario { get; set; }

    // Calculado sob demanda - mesmo padrao de EvolucaoPercentual em AtivoResponse
    // (regra-de-negocio.md item 8.1)
    public decimal ValorTotal { get; set; }

    public DateTime CriadoEm { get; set; }

    public static AtivoAporteResponse FromAporte(AtivoAporteDomain aporte)
    {
        return new AtivoAporteResponse
        {
            Id = aporte.Id,
            AtivoId = aporte.AtivoId,
            Data = aporte.Data,
            Quantidade = aporte.Quantidade,
            PrecoUnitario = aporte.PrecoUnitario,
            ValorTotal = aporte.ValorTotal,
            CriadoEm = aporte.CriadoEm
        };
    }
}
