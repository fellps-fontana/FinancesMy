using MyFinances.Domain;
using ContaDomain = MyFinances.Domain.Conta;

namespace MyFinances.DTOs.Conta;

public class ContaResponse
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public TipoConta Tipo { get; set; }

    public SubtipoConta? Subtipo { get; set; }

    public string? Icone { get; set; }

    public string? Cor { get; set; }

    public OrigemConta Origem { get; set; }

    // Saldo exibido: calculado dos ativos vinculados quando SaldoCalculado (itens 8.5/10).
    public decimal Saldo { get; set; }

    // true = Saldo vem da soma dos ativos; a tela nao permite editar saldo manual.
    public bool SaldoCalculado { get; set; }

    public decimal? SaldoManual { get; set; }

    public bool Ativa { get; set; }

    public int? DiaFechamento { get; set; }

    public int? DiaVencimento { get; set; }

    public string? PierreAccountId { get; set; }

    // Sem saldo calculado (mantem chamadores atuais): Saldo = SaldoManual ?? 0.
    public static ContaResponse FromConta(ContaDomain conta)
    {
        return FromConta(conta, conta.SaldoManual ?? 0m, false);
    }

    // saldo/saldoCalculado vem de IContaService.ObterSaldo (regra nao mora em DTO).
    public static ContaResponse FromConta(ContaDomain conta, decimal saldo, bool saldoCalculado)
    {
        if (conta.Tipo == null)
        {
            throw new InvalidOperationException($"Conta com ID {conta.Id} possui Tipo nulo, o que e um estado invalido.");
        }

        return new()
        {
            Id = conta.Id,
            Nome = conta.Nome,
            Tipo = conta.Tipo.Value,
            Subtipo = conta.Subtipo,
            Icone = conta.Icone,
            Cor = conta.Cor,
            Origem = conta.Origem,
            Saldo = saldo,
            SaldoCalculado = saldoCalculado,
            SaldoManual = conta.SaldoManual,
            Ativa = conta.Ativa,
            DiaFechamento = conta.DiaFechamento,
            DiaVencimento = conta.DiaVencimento,
            PierreAccountId = conta.PierreAccountId
        };
    }
}
