using MyFinances.Domain;
using RecebivelRecorrenteDomain = MyFinances.Domain.RecebivelRecorrente;

namespace MyFinances.DTOs.RecebivelRecorrente;

public class RecebivelRecorrenteResponse
{
    public Guid Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string Periodicidade { get; set; } = string.Empty;
    public int? DiaVencimento { get; set; }
    public int? MesReferencia { get; set; }
    public string? DiaDaSemana { get; set; }
    public Guid? CategoriaId { get; set; }
    public bool Ativa { get; set; }

    public static RecebivelRecorrenteResponse FromRecebivelRecorrente(RecebivelRecorrenteDomain molde)
    {
        return new()
        {
            Id = molde.Id,
            Descricao = molde.Descricao,
            Valor = molde.Valor,
            Periodicidade = molde.Periodicidade.ToStorageValue(),
            DiaVencimento = molde.DiaVencimento,
            MesReferencia = molde.MesReferencia,
            DiaDaSemana = molde.DiaDaSemana?.ToStorageValue(),
            CategoriaId = molde.CategoriaId,
            Ativa = molde.Ativa
        };
    }
}
