namespace MyFinances.Domain;

public class DeParaCategoria
{
    public Guid Id { get; set; }

    public string CategoriaPierre { get; set; } = string.Empty;

    public Guid CategoriaId { get; set; }

    public Categoria Categoria { get; set; } = null!;
}
