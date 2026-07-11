using MyFinances.Domain;

namespace MyFinances.DTOs.Categoria;

public class CriarCategoriaRequest
{
    public string Nome { get; set; } = string.Empty;

    public TipoCategoria Tipo { get; set; }

    public Guid? ParentId { get; set; }
}
