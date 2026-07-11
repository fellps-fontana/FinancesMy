using MyFinances.DTOs.Categoria;
using MyFinances.Models;
using DeParaCategoriaModel = MyFinances.Models.DeParaCategoria;

namespace MyFinances.DTOs.DeParaCategoria;

public class DeParaCategoriaResponse
{
    public Guid Id { get; set; }

    public string CategoriaPierre { get; set; } = string.Empty;

    public Guid CategoriaId { get; set; }

    public CategoriaResponse? Categoria { get; set; }

    public static DeParaCategoriaResponse FromDeParaCategoria(DeParaCategoriaModel deParaCategoria)
    {
        return new()
        {
            Id = deParaCategoria.Id,
            CategoriaPierre = deParaCategoria.CategoriaPierre,
            CategoriaId = deParaCategoria.CategoriaId,
            Categoria = deParaCategoria.Categoria != null
                ? CategoriaResponse.FromCategoria(deParaCategoria.Categoria)
                : null
        };
    }
}
