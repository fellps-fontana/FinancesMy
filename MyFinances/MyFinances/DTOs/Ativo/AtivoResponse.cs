using MyFinances.Domain;
using AtivoDomain = MyFinances.Domain.Ativo;

namespace MyFinances.DTOs.Ativo;

public class AtivoResponse
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public TipoAtivo Tipo { get; set; }

    public string Instituicao { get; set; } = string.Empty;

    public decimal Quantidade { get; set; }

    public decimal ValorInvestido { get; set; }

    public decimal ValorAtual { get; set; }

    public decimal PrecoMedio { get; set; }

    public decimal EvolucaoPercentual { get; set; }

    public DateOnly DataCompra { get; set; }

    public bool Ativa { get; set; }

    // EvolucaoPercentual e PrecoMedio chegam ja calculados do controller
    // (regra-de-negocio.md item 8.1) - regra de negocio nao mora em DTO
    public static AtivoResponse FromAtivo(AtivoDomain ativo, decimal evolucaoPercentual, decimal precoMedio)
    {
        return new AtivoResponse
        {
            Id = ativo.Id,
            Nome = ativo.Nome,
            Tipo = ativo.Tipo,
            Instituicao = ativo.Instituicao,
            Quantidade = ativo.Quantidade,
            ValorInvestido = ativo.ValorInvestido,
            ValorAtual = ativo.ValorAtual,
            PrecoMedio = precoMedio,
            EvolucaoPercentual = evolucaoPercentual,
            DataCompra = ativo.DataCompra,
            Ativa = ativo.Ativa
        };
    }
}
