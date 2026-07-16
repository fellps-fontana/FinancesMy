namespace MyFinances.DTOs.Ativo;

public class AtivosResumoResponse
{
    public decimal TotalInvestido { get; set; }

    public decimal TotalAtual { get; set; }

    public IEnumerable<ResumoPorTipo> PorTipo { get; set; } = [];
}

public class ResumoPorTipo
{
    public string Tipo { get; set; } = string.Empty;

    public decimal ValorAtual { get; set; }

    public decimal PercentualDaCarteira { get; set; }
}
