namespace MyFinances.Models;

public class Lancamento
{
    public Guid Id { get; set; }

    public string? PierreTxnId { get; set; }

    public Guid ContaId { get; set; }

    public Guid? CategoriaId { get; set; }

    public string? Descricao { get; set; }

    public decimal Valor { get; set; }

    public TipoLancamento Tipo { get; set; }

    public DateOnly Data { get; set; }

    public StatusLancamento Status { get; set; }

    public bool Manual { get; set; } = false;

    public bool Oculto { get; set; } = false;

    public Guid? ContaFixaId { get; set; }

    public Guid? ConciliadoCom { get; set; }

    public Guid? TransferenciaId { get; set; }

    public Guid? FaturaId { get; set; }

    // Relacionamentos
    public Conta? Conta { get; set; }

    public Categoria? Categoria { get; set; }

    public Lancamento? LancamentoConciliado { get; set; }

    public Transferencia? Transferencia { get; set; }

    public Fatura? Fatura { get; set; }

    public ICollection<Lancamento> LancamentosConciliados { get; set; } = new List<Lancamento>();
}
