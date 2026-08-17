namespace MyFinances.Domain;

public class Ativo
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public TipoAtivo Tipo { get; set; }

    public string Instituicao { get; set; } = string.Empty;

    // Total de unidades/cotas em carteira, soma de todos os aportes
    // (regra-de-negocio.md item 8.1). Incrementado a cada RegistrarAporte.
    public decimal Quantidade { get; set; }

    public decimal ValorInvestido { get; set; }

    public decimal ValorAtual { get; set; }

    public DateOnly DataCompra { get; set; }

    public bool Ativa { get; set; } = true;

    public DateTime CriadoEm { get; set; }

    public DateTime? AtualizadoEm { get; set; }

    // Navegacao
    public ICollection<Rendimento> Rendimentos { get; set; } = [];
}
