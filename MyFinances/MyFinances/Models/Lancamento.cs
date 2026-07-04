namespace MyFinances.Models;

public class Lancamento
{
    public Guid Id { get; set; }
    public string? PierreTxnId { get; set; } // null se manual; chave de dedup
    public Guid ContaId { get; set; }
    public Guid? CategoriaId { get; set; }
    public string? Descricao { get; set; }
    public required decimal Valor { get; set; }
    public required string Tipo { get; set; } // DEBIT | CREDIT
    public required DateOnly Data { get; set; }
    public required string Status { get; set; } // PENDENTE | SUGERIDO | PAGO
    public bool Manual { get; set; } = false;
    public bool Oculto { get; set; } = false;
    public Guid? ContaFixaId { get; set; } // null se avulso; FK nullable sem entidade por enquanto
    public Guid? ConciliadoCom { get; set; } // txn OF vinculada na conciliacao
    public Guid? TransferenciaId { get; set; }
    public Guid? FaturaId { get; set; }

    // Relacionamentos
    public required Conta Conta { get; set; }
    public Categoria? Categoria { get; set; }
    public Lancamento? LancamentoConciliado { get; set; } // conciliado_com = self-reference
    public Transferencia? Transferencia { get; set; }
    public Fatura? Fatura { get; set; }
    public ICollection<Lancamento> LancamentosConciliados { get; set; } = new List<Lancamento>();
}
