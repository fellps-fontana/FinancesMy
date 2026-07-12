using MyFinances.Domain;
using CategoriaDomain = MyFinances.Domain.Categoria;

namespace MyFinances.DTOs.Categoria;

public class CategoriaResponse
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public TipoCategoria Tipo { get; set; }

    public Guid? ParentId { get; set; }

    public ICollection<CategoriaResponse> Subcategorias { get; set; } = new List<CategoriaResponse>();

    public bool Arquivada { get; set; }

    public static CategoriaResponse FromCategoria(CategoriaDomain categoria)
    {
        return new()
        {
            Id = categoria.Id,
            Nome = categoria.Nome,
            Tipo = categoria.Tipo,
            ParentId = categoria.ParentId,
            Subcategorias = categoria.Subcategorias
                .Select(FromCategoria)
                .ToList(),
            Arquivada = categoria.Arquivada
        };
    }
}
