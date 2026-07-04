namespace MyFinances.Models;

public class Categoria
{
    public Guid Id { get; set; }
    public required string Nome { get; set; }
    public required string Tipo { get; set; } // DESPESA | RECEITA
    public Guid? ParentId { get; set; }
    public bool Arquivada { get; set; } = false;

    // Relacionamentos
    public Categoria? Parent { get; set; }
    public ICollection<Categoria> Subcategorias { get; set; } = new List<Categoria>();
    public ICollection<Lancamento> Lancamentos { get; set; } = new List<Lancamento>();
}
