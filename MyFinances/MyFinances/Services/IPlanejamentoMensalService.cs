using MyFinances.Domain;

namespace MyFinances.Services;

public interface IPlanejamentoMensalService
{
    // Le o plano do mes JA CALCULADO (item 20). Geracao sob demanda: mes corrente ou futuro sem plano
    // nasce na primeira consulta (renda pre-preenchida pelos moldes ativos; copia % de investimento e
    // sublimites do plano mais recente anterior, sem categoria arquivada). Mes passado sem plano =>
    // null (nao cria). SomenteLeitura = mes anterior ao corrente.
    // Lanca ValorInvalidoException (400) se mes fora de 1..12 ou ano fora de 1..9999.
    // Lanca PlanoMensalForaDoHorizonteException (400) se o mes passa de 12 meses a frente do corrente.
    Task<PlanoMensalConsulta?> ObterAsync(int ano, int mes);

    // Salva renda, % de investimento e o conjunto COMPLETO de sublimites (replace-all; diff in-place:
    // atualiza existentes, adiciona novos, remove ausentes). Ordem: valida ano/mes -> mes passado
    // (PlanoMensalPassadoSomenteLeituraException, 409) -> PlanoMensalValidator -> persiste -> devolve
    // o plano recalculado. Violacao => nada e salvo.
    // Lanca ValorInvalidoException (400; inclui renda acima do teto numeric(18,2)),
    // PlanoMensalForaDoHorizonteException (400, antes de qualquer gravacao), RendaPrevistaNegativaException (400),
    // CategoriaNaoEncontradaException (404), CategoriaInvalidaParaSublimiteException (422),
    // SublimiteDuplicadoException (422), SubcategoriaSemSublimiteNoPaiException (422),
    // SomaPercentuaisExcedeCemException (422).
    Task<PlanoMensalConsulta> SalvarAsync(
        int ano,
        int mes,
        decimal rendaPrevista,
        decimal percentualInvestimento,
        IReadOnlyList<SublimiteEntrada> sublimites);
}
