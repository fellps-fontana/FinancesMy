namespace MyFinances.DTOs.Relatorio;

public record RelatorioCategoriaDetalheResponse(
    Guid? CategoriaId,
    string Nome,
    string? Icone,
    decimal Total,
    decimal PercentualDoTotal,
    LimiteRelatorioResponse? Limite,
    IReadOnlyList<LancamentoRelatorioResponse> Lancamentos);

// Presente so se a categoria tem sublimite no plano do mes (item 20). ValorLimite = Valor do sublimite.
// GastoRealizado = gasto variavel do sublimite (item 14: sem conta fixa nem assinatura de cartao),
// por isso pode ser menor que o Total da linha (item 19). Estourado = GastoRealizado > ValorLimite
// (calculado no sublimite; sublimite R$ 0 com gasto > 0 e estourado).
public record LimiteRelatorioResponse(decimal ValorLimite, decimal GastoRealizado, bool Estourado);

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
