namespace MyFinances.DTOs.Conta;

// Item 10.2. Sem campo Tipo de proposito: o tipo da conta e imutavel.
public class EditarContaRequest
{
    public required string Nome { get; set; }

    public string? Subtipo { get; set; }

    public string? Icone { get; set; }

    public string? Cor { get; set; }

    // Item 10.2: so Cartao. Cartao exige os dois (1..31). Nos demais tipos sao ignorados.
    public int? DiaFechamento { get; set; }

    public int? DiaVencimento { get; set; }
}
