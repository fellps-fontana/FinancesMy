namespace MyFinances.Domain;

// Resultado de PlanejamentoMensalCalculator.Calcular (item 20). Nada aqui e gravado.
// Valores em decimal SEM arredondamento; arredondar para 2 casas so na exibicao.
public record PlanoMensalCalculado(
    int Ano,
    int Mes,
    decimal Renda,
    decimal Fixas,
    decimal PercentualInvestimento,
    decimal InvestimentoPlanejado,
    decimal Investimento,
    // Investimento < InvestimentoPlanejado (aviso "investimento reduzido de X para Y").
    bool InvestimentoReduzido,
    // Renda - Fixas < 0 (alerta "plano nao fecha").
    bool PlanoNaoFecha,
    decimal Sobra,
    decimal NaoAlocado,
    decimal GastoSemSublimite,
    // Ordem: categorias de primeiro nivel por Nome; cada subcategoria logo apos o seu pai, por Nome.
    IReadOnlyList<SublimiteCalculado> Sublimites,
    // Gasto variavel (item 14) de todas as categorias DESPESA (1o nivel = propria + subcategorias diretas;
    // subcategoria = propria), ordenado por CategoriaId.
    IReadOnlyList<GastoCategoriaCalculado>? GastosPorCategoria = null)
{
    public IReadOnlyList<GastoCategoriaCalculado> GastosPorCategoria { get; init; } = GastosPorCategoria ?? [];
}

// Valor: sublimite em R$ (sobra*pct/100 no primeiro nivel; valorPai*pct/100 na subcategoria;
// subcategoria cujo pai nao tem sublimite no plano => 0, estado que a validacao impede).
// GastoRealizado: gasto do sublimite (item 14; pai soma proprias + subcategorias diretas).
// FracaoUtilizada: GastoRealizado / Valor (1,0 = 100%); 0 quando Valor == 0.
// Estourado: GastoRealizado > Valor (Valor 0 com gasto > 0 => true).
public record SublimiteCalculado(
    Guid CategoriaId,
    Guid? ParentId,
    string Nome,
    string? Icone,
    bool CategoriaArquivada,
    decimal Percentual,
    decimal Valor,
    decimal GastoRealizado,
    decimal FracaoUtilizada,
    bool Estourado);

// Gasto variavel no mes (item 14) de uma categoria DESPESA (independente de ter sublimite salvo).
public record GastoCategoriaCalculado(
    Guid CategoriaId,
    decimal GastoRealizado);

// SomenteLeitura = mes anterior ao mes corrente (item 20, "Ciclo do mes").
public record PlanoMensalConsulta(PlanoMensalCalculado Plano, bool SomenteLeitura);
