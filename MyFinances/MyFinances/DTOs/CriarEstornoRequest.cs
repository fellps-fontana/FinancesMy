namespace MyFinances.DTOs;

public class CriarEstornoRequest
{
    public required Guid CompraId { get; set; }

    public required string Motivo { get; set; }

    public required DateOnly Data { get; set; }
}
