namespace MyFinances.DTOs.Relatorio;

public record RelatorioGastoPorCategoriaResponse(
    int Ano,
    int Mes,
    decimal TotalGasto,
    decimal TotalMesAnterior,
    decimal? VariacaoPercentual,
    IReadOnlyList<LinhaGastoPorCategoriaResponse> Linhas);

// CategoriaId == null => linha "Sem categoria".
public record LinhaGastoPorCategoriaResponse(
    Guid? CategoriaId,
    string Nome,
    string? Icone,
    decimal Valor,
    decimal Percentual);
