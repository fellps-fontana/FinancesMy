namespace MyFinances.Domain;

public class ContaFixa
{
    public Guid Id { get; set; }

    public Guid ContaId { get; set; }

    public Guid? CategoriaId { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    public int DiaVencimento { get; set; }

    public PeriodicidadeContaFixa Periodicidade { get; set; } = PeriodicidadeContaFixa.Mensal;

    // regra-de-negocio.md item 6: so relevante quando Periodicidade == Anual
    // (mes em que a ocorrencia anual cai); null quando Mensal.
    public int? MesReferencia { get; set; }

    public bool Ativa { get; set; } = true;

    // Relacionamentos
    public Conta? Conta { get; set; }

    public Categoria? Categoria { get; set; }

    public ICollection<Lancamento> Lancamentos { get; set; } = new List<Lancamento>();
}
