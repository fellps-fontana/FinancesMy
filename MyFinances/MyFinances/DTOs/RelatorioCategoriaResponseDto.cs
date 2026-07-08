namespace MyFinances.DTOs;

public class RelatorioCategoriaResponseDto
{
    public class ItemPorCategoria
    {
        public Guid? CategoriaId { get; set; }
        public string? NomeCategoria { get; set; }
        public decimal Total { get; set; }
    }

    public IEnumerable<ItemPorCategoria> Itens { get; set; } = new List<ItemPorCategoria>();
    public int Mes { get; set; }
    public int Ano { get; set; }
}
