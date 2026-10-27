using MyFinances.Domain;

namespace MyFinances.DTOs.PlanejamentoMensal;

public record PlanoMensalResponse(
    int Ano,
    int Mes,
    bool SomenteLeitura,
    decimal RendaPrevista,
    decimal Fixas,
    decimal PercentualInvestimento,
    decimal InvestimentoPlanejado,
    decimal Investimento,
    bool InvestimentoReduzido,
    bool PlanoNaoFecha,
    decimal Sobra,
    decimal NaoAlocado,
    decimal GastoSemSublimite,
    IReadOnlyList<SublimiteResponse> Sublimites,
    IReadOnlyList<GastoCategoriaResponse> GastosPorCategoria)
{
    public static PlanoMensalResponse From(PlanoMensalConsulta consulta)
    {
        var plano = consulta.Plano;

        return new PlanoMensalResponse(
            plano.Ano,
            plano.Mes,
            consulta.SomenteLeitura,
            plano.Renda,
            plano.Fixas,
            plano.PercentualInvestimento,
            plano.InvestimentoPlanejado,
            plano.Investimento,
            plano.InvestimentoReduzido,
            plano.PlanoNaoFecha,
            plano.Sobra,
            plano.NaoAlocado,
            plano.GastoSemSublimite,
            plano.Sublimites.Select(SublimiteResponse.From).ToList(),
            plano.GastosPorCategoria.Select(GastoCategoriaResponse.From).ToList());
    }
}

public record SublimiteResponse(
    Guid CategoriaId,
    Guid? ParentId,
    string Nome,
    string? Icone,
    bool CategoriaArquivada,
    decimal Percentual,
    decimal Valor,
    decimal GastoRealizado,
    decimal FracaoUtilizada,
    bool Estourado)
{
    public static SublimiteResponse From(SublimiteCalculado sublimite) =>
        new(
            sublimite.CategoriaId,
            sublimite.ParentId,
            sublimite.Nome,
            sublimite.Icone,
            sublimite.CategoriaArquivada,
            sublimite.Percentual,
            sublimite.Valor,
            sublimite.GastoRealizado,
            sublimite.FracaoUtilizada,
            sublimite.Estourado);
}

public record GastoCategoriaResponse(
    Guid CategoriaId,
    decimal GastoRealizado)
{
    public static GastoCategoriaResponse From(GastoCategoriaCalculado gasto) =>
        new(gasto.CategoriaId, gasto.GastoRealizado);
}
