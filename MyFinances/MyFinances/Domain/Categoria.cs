namespace MyFinances.Domain;

public class Categoria
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public TipoCategoria Tipo { get; set; }

    public Guid? ParentId { get; set; }

    public Categoria? Parent { get; set; }

    public ICollection<Categoria> Subcategorias { get; set; } = new List<Categoria>();

    public bool Arquivada { get; set; } = false;

    public ICollection<Lancamento> Lancamentos { get; set; } = new List<Lancamento>();
}
