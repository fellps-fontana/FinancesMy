namespace MyFinances.Models;

public class Categoria
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public TipoCategoria Tipo { get; set; }

    public Guid? ParentId { get; set; }

    public Categoria? Parent { get; set; }

    public ICollection<Categoria> Subcategorias { get; set; } = new List<Categoria>();

    public bool Arquivada { get; set; } = false;
}
