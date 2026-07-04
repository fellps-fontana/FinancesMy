namespace MyFinances.Models;

public class Conta
{
    public Guid Id { get; set; }
    public required string Nome { get; set; }
    public required string Origem { get; set; } // OPEN_FINANCE | MANUAL
    public string? Tipo { get; set; } // BANCO | CARTAO | INVESTIMENTO
    public string? PierreAccountId { get; set; }
    public decimal? SaldoManual { get; set; }
    public int? DiaFechamento { get; set; } // só CARTAO
    public int? DiaVencimento { get; set; } // só CARTAO
    public bool Ativa { get; set; } = true;

    // Relacionamentos
    public ICollection<Lancamento> Lancamentos { get; set; } = new List<Lancamento>();
    public ICollection<Transferencia> TransferenciasOrigem { get; set; } = new List<Transferencia>();
    public ICollection<Transferencia> TransferenciasDestino { get; set; } = new List<Transferencia>();
    public ICollection<Fatura> Faturas { get; set; } = new List<Fatura>();
}
