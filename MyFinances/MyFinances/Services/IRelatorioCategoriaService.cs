using MyFinances.DTOs.Relatorio;

namespace MyFinances.Services;

public interface IRelatorioCategoriaService
{
    // Lanca ValorInvalidoException se mes fora de 1..12 ou ano < 1.
    Task<RelatorioGastoPorCategoriaResponse> ObterResumoAsync(int ano, int mes);

    // categoriaId == null => detalhe de "Sem categoria".
    // Lanca CategoriaNaoEncontradaException (404); CategoriaNaoEhPrimeiroNivelException (400)
    // se for subcategoria ou nao for Despesa; ValorInvalidoException (400) para ano/mes invalidos.
    Task<RelatorioCategoriaDetalheResponse> ObterDetalheAsync(int ano, int mes, Guid? categoriaId);
}
