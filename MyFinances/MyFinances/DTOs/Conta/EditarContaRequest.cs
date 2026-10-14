namespace MyFinances.DTOs.Conta;

// Item 10.2. Sem campo Tipo de proposito: o tipo da conta e imutavel.
public class EditarContaRequest
{
    public required string Nome { get; set; }

    public string? Subtipo { get; set; }

    public string? Icone { get; set; }

    public string? Cor { get; set; }
}
