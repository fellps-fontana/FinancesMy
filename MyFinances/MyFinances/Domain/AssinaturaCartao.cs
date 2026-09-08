namespace MyFinances.Domain;

public class AssinaturaCartao
{
    public Guid Id { get; set; }

    public Guid ContaId { get; set; }

    public Guid? CategoriaId { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    public int DiaReferencia { get; set; }

    public bool Ativa { get; set; } = true;

    // Relacionamentos
    public Conta? Conta { get; set; }

    public Categoria? Categoria { get; set; }

    public ICollection<Lancamento> Compras { get; set; } = new List<Lancamento>();
}
