namespace MyFinances.Models;

public class Conta
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public OrigemConta Origem { get; set; }

    public TipoConta? Tipo { get; set; }

    public string? PierreAccountId { get; set; }

    public decimal? SaldoManual { get; set; }

    public int? DiaFechamento { get; set; }

    public int? DiaVencimento { get; set; }

    public bool Ativa { get; set; } = true;
}
