namespace MyFinances.DTOs.ContaReceber;

public class EditarContaReceberRequest
{
    public string Descricao { get; set; } = string.Empty;

    // Ignorada em RECEBIVEL; obrigatoria em EMPRESTIMO.
    public string? Pessoa { get; set; }

    // null limpa a data prevista.
    public DateOnly? DataPrevista { get; set; }

    // null = nao alterar. So aceito em RECEBIVEL Pendente (ou igual ao atual, no-op).
    public decimal? ValorTotal { get; set; }
}
