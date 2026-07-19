namespace MyFinances.DTOs.Categoria;

public class EditarCategoriaRequest
{
    public string Nome { get; set; } = string.Empty;

    public Guid? ParentId { get; set; }
}
