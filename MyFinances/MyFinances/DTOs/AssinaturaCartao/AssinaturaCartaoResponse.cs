using MyFinances.Domain;

namespace MyFinances.DTOs.AssinaturaCartao;

public class AssinaturaCartaoResponse
{
    public Guid Id { get; set; }
    public Guid ContaId { get; set; }
    public Guid? CategoriaId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int DiaReferencia { get; set; }
    public bool Ativa { get; set; }

    public static AssinaturaCartaoResponse FromDomain(Domain.AssinaturaCartao assinatura)
    {
        return new AssinaturaCartaoResponse
        {
            Id = assinatura.Id,
            ContaId = assinatura.ContaId,
            CategoriaId = assinatura.CategoriaId,
            Descricao = assinatura.Descricao,
            Valor = assinatura.Valor,
            DiaReferencia = assinatura.DiaReferencia,
            Ativa = assinatura.Ativa
        };
    }
}
