using System.ComponentModel.DataAnnotations;

namespace MyFinances.DTOs.PlanejamentoMensal;

// Os tres campos sao obrigatorios: ausente ou null => 400 de model state.
// Lista vazia de sublimites remove todos. Atributos no parametro do construtor (record posicional).
public record SalvarPlanoMensalRequest(
    [Required] decimal? RendaPrevista,
    [Required] decimal? PercentualInvestimento,
    [Required] IReadOnlyList<SublimiteRequest> Sublimites);

// Elemento null dentro da lista nao passa pela validacao de model state: o controller recusa.
public record SublimiteRequest(
    [Required] Guid? CategoriaId,
    [Required] decimal? Percentual);
