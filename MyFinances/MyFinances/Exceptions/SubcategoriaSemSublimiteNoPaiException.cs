namespace MyFinances.Exceptions;

// Cobre dois casos: sublimite de subcategoria sem sublimite no pai, e remocao do sublimite do pai
// enquanto alguma subcategoria dele mantem sublimite (item 14).
public class SubcategoriaSemSublimiteNoPaiException : Exception
{
    public Guid SubcategoriaId { get; }
    public Guid PaiId { get; }

    public SubcategoriaSemSublimiteNoPaiException(Guid subcategoriaId, Guid paiId)
        : base($"A subcategoria {subcategoriaId} tem sublimite, mas a categoria-pai {paiId} nao. Remova o sublimite da subcategoria ou defina o do pai.")
    {
        SubcategoriaId = subcategoriaId;
        PaiId = paiId;
    }
}
