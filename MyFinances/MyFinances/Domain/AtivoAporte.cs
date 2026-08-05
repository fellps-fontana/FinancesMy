namespace MyFinances.Domain;

public class AtivoAporte
{
    public Guid Id { get; set; }

    public Guid AtivoId { get; set; }

    public DateOnly Data { get; set; }

    public decimal Quantidade { get; set; }

    public decimal PrecoUnitario { get; set; }

    // Calculado sob demanda, nunca coluna persistida (mesmo padrao de
    // EvolucaoPercentual no AtivoResponse - regra-de-negocio.md item 8.1).
    public decimal ValorTotal => Quantidade * PrecoUnitario;

    public DateTime CriadoEm { get; set; }
}
