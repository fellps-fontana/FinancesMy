using MyFinances.Domain;
using ContaReceberDomain = MyFinances.Domain.ContaReceber;

namespace MyFinances.DTOs.ContaReceber;

public class ContaReceberResponse
{
    public Guid Id { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;

    public string? Pessoa { get; set; }

    public decimal ValorTotal { get; set; }

    public decimal SaldoPendente { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateOnly DataRegistro { get; set; }

    public DateOnly? DataPrevista { get; set; }

    public static ContaReceberResponse FromContaReceber(ContaReceberDomain contaReceber)
    {
        var saldo = ContaReceberSaldoCalculator.Calcular(contaReceber);

        return new()
        {
            Id = contaReceber.Id,
            Tipo = contaReceber.Tipo.ToStorageValue(),
            Descricao = contaReceber.Descricao,
            Pessoa = contaReceber.Pessoa,
            ValorTotal = contaReceber.ValorTotal,
            SaldoPendente = saldo.SaldoPendente,
            Status = saldo.Status.ToStorageValue(),
            DataRegistro = contaReceber.DataRegistro,
            DataPrevista = contaReceber.DataPrevista
        };
    }
}
