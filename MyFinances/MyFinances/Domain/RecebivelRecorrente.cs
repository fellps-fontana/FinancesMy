namespace MyFinances.Domain;

public class RecebivelRecorrente
{
    public Guid Id { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    public PeriodicidadeRecebivel Periodicidade { get; set; } = PeriodicidadeRecebivel.Mensal;

    // regra-de-negocio.md item 15: obrigatorio para Mensal e Anual, null para Semanal.
    public int? DiaVencimento { get; set; }

    // regra-de-negocio.md item 15: obrigatorio para Anual, null caso contrario.
    public int? MesReferencia { get; set; }

    // regra-de-negocio.md item 15: obrigatorio para Semanal, null caso contrario.
    public DiaDaSemana? DiaDaSemana { get; set; }

    public Guid? CategoriaId { get; set; }

    public bool Ativa { get; set; } = true;

    // Relacionamentos
    public Categoria? Categoria { get; set; }

    // Ocorrencias materializadas (conta_receber com recebivel_recorrente_id = este molde).
    public ICollection<ContaReceber> Ocorrencias { get; set; } = new List<ContaReceber>();
}
