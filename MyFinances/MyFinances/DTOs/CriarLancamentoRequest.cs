namespace MyFinances.DTOs;

public class CriarLancamentoRequest
{
    public required string Tipo { get; set; } // DEBIT | CREDIT
    public Guid? CategoriaId { get; set; }
    public required string Descricao { get; set; }
    public required decimal Valor { get; set; } // Sempre positivo, o tipo determina entrada/saida
    public required DateOnly Data { get; set; }
    public required string Status { get; set; } // PENDENTE | PAGO
}
