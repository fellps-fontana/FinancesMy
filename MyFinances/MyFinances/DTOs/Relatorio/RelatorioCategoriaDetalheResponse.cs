namespace MyFinances.DTOs.Relatorio;

public record RelatorioCategoriaDetalheResponse(
    Guid? CategoriaId,
    string Nome,
    string? Icone,
    decimal Total,
    decimal PercentualDoTotal,
    LimiteRelatorioResponse? Limite,
    IReadOnlyList<LancamentoRelatorioResponse> Lancamentos);

// Presente so se a categoria tem limite. GastoRealizado == Total da linha (itens 14 e 19).
public record LimiteRelatorioResponse(decimal ValorLimite, decimal GastoRealizado);

// ContaTipo: "BANCO" | "CARTAO" | "INVESTIMENTO". SubcategoriaNome != null so quando o
// lancamento esta numa subcategoria da categoria raiz.
public record LancamentoRelatorioResponse(
    Guid Id,
    string? Descricao,
    DateOnly Data,
    Guid ContaId,
    string ContaNome,
    string? ContaTipo,
    string? SubcategoriaNome,
    decimal Valor);
