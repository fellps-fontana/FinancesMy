namespace MyFinances.Domain;

public class Ativo
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public TipoAtivo Tipo { get; set; }

    public string Instituicao { get; set; } = string.Empty;

    public decimal ValorInvestido { get; set; }

    public decimal ValorAtual { get; set; }

    public DateOnly DataCompra { get; set; }

    public bool Ativa { get; set; } = true;

    public DateTime CriadoEm { get; set; }

    public DateTime? AtualizadoEm { get; set; }
}
