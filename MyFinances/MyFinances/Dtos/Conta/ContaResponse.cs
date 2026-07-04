using MyFinances.Models;

namespace MyFinances.Dtos.Conta;

public class ContaResponse
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public TipoConta Tipo { get; set; }

    public OrigemConta Origem { get; set; }

    public decimal? SaldoManual { get; set; }

    public bool Ativa { get; set; }

    public static ContaResponse FromConta(Models.Conta conta)
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
            Origem = conta.Origem,
            SaldoManual = conta.SaldoManual,
            Ativa = conta.Ativa
        };
    }
}
