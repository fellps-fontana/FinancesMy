using MyFinances.Models;

namespace MyFinances.DTOs.Ativo;

public class AtivoResponse
{
    public Guid Id { get; set; }

    public string Ticker { get; set; } = string.Empty;

    public string? Nome { get; set; }

    public decimal Quantidade { get; set; }

    public decimal PrecoMedio { get; set; }

    public decimal PrecoAtual { get; set; }

    public bool Ativa { get; set; }

    public static AtivoResponse FromAtivo(Models.Ativo ativo)
    {
        return new()
        {
            Id = ativo.Id,
            Ticker = ativo.Ticker,
            Nome = ativo.Nome,
            Quantidade = ativo.Quantidade,
            PrecoMedio = ativo.PrecoMedio,
            PrecoAtual = ativo.PrecoAtual,
            Ativa = ativo.Ativa
        };
    }
}
