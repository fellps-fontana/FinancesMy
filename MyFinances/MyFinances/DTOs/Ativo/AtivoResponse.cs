using MyFinances.Domain;
using AtivoDomain = MyFinances.Domain.Ativo;

namespace MyFinances.DTOs.Ativo;

public class AtivoResponse
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public TipoAtivo Tipo { get; set; }

    public string Instituicao { get; set; } = string.Empty;

    public decimal ValorInvestido { get; set; }

    public decimal ValorAtual { get; set; }

    public decimal EvolucaoPercentual { get; set; }

    public DateOnly DataCompra { get; set; }

    public bool Ativa { get; set; }

    // EvolucaoPercentual chega ja calculada do Service - regra de negocio
    // (item 8.1 de regra-de-negocio.md) nao mora em DTO.
    public static AtivoResponse FromAtivo(AtivoDomain ativo, decimal evolucaoPercentual)
    {
        return new AtivoResponse
        {
            Id = ativo.Id,
            Nome = ativo.Nome,
            Tipo = ativo.Tipo,
            Instituicao = ativo.Instituicao,
            ValorInvestido = ativo.ValorInvestido,
            ValorAtual = ativo.ValorAtual,
            EvolucaoPercentual = evolucaoPercentual,
            DataCompra = ativo.DataCompra,
            Ativa = ativo.Ativa
        };
    }
}
