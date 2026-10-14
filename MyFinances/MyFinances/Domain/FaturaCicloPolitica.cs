namespace MyFinances.Domain;

public enum DestinoLancamentoFatura
{
    CriarNova,
    Usar,
    ReabrirEUsar,
    RejeitarPaga
}

public record MudancaStatusFatura(Fatura Fatura, StatusFatura NovoStatus);

// Regra pura do ciclo/status da fatura (item 12, "Ciclo e status da fatura").
// Sem I/O e sem relogio: "hoje" e "fechamentoCicloAtual" (fechamento do ciclo corrente calculado de
// dia_fechamento da conta, via FaturaCicloService.CalcularDatasCiclo) entram como parametro.
// Nao muta nenhuma fatura.
public static class FaturaCicloPolitica
{
    public const string ErroCicloPagoEncerrado = "Nao e possivel lancar em fatura ja paga de ciclo encerrado";

    // Status que o calendario determina para UMA fatura nao paga. Fechamento inclusivo:
    // dataFechamento == hoje ainda e ciclo corrente.
    public static StatusFatura ClassificarPorCalendario(
        DateOnly dataFechamento,
        DateOnly hoje,
        DateOnly fechamentoCicloAtual)
    {
        if (dataFechamento < hoje)
        {
            return StatusFatura.Fechada;
        }

        return dataFechamento <= fechamentoCicloAtual
            ? StatusFatura.Aberta
            : StatusFatura.Futura;
    }

    // Fatura ATUAL: de menor DataFechamento com hoje <= DataFechamento <= fechamentoCicloAtual,
    // qualquer status (PAGA conta como atual). Null se nao existir.
    public static Fatura? SelecionarAtual(
        IEnumerable<Fatura> faturasDaConta,
        DateOnly hoje,
        DateOnly fechamentoCicloAtual)
    {
        return faturasDaConta
            .Where(f => EstaNaJanelaDoCicloAtual(f.DataFechamento, hoje, fechamentoCicloAtual))
            .OrderBy(f => f.DataFechamento)
            .FirstOrDefault();
    }

    // Mudancas para o status gravado refletir o calendario. Paga nunca entra. So a atual nao paga
    // vira Aberta; demais nao pagas na janela viram Futura. Devolve so o que muda (idempotente).
    public static IReadOnlyList<MudancaStatusFatura> Reconciliar(
        IEnumerable<Fatura> faturasDaConta,
        DateOnly hoje,
        DateOnly fechamentoCicloAtual)
    {
        var faturas = faturasDaConta.ToList();
        var atualNaoPagaId = SelecionarAtualNaoPagaId(faturas, hoje, fechamentoCicloAtual);

        var mudancas = new List<MudancaStatusFatura>();
        foreach (var fatura in faturas.Where(f => f.Status != StatusFatura.Paga))
        {
            var novoStatus = DeterminarStatusReconciliado(fatura, hoje, fechamentoCicloAtual, atualNaoPagaId);
            if (novoStatus != fatura.Status)
            {
                mudancas.Add(new MudancaStatusFatura(fatura, novoStatus));
            }
        }

        return mudancas;
    }

    // faturaDoCiclo null -> CriarNova. Nao paga -> Usar (retroativa em Fechada tambem entra).
    // Paga com fechamento ja passado -> RejeitarPaga. Paga com fechamento ainda nao passado -> ReabrirEUsar.
    public static DestinoLancamentoFatura DecidirDestino(Fatura? faturaDoCiclo, DateOnly hoje)
    {
        if (faturaDoCiclo == null)
        {
            return DestinoLancamentoFatura.CriarNova;
        }

        if (faturaDoCiclo.Status != StatusFatura.Paga)
        {
            return DestinoLancamentoFatura.Usar;
        }

        return faturaDoCiclo.DataFechamento < hoje
            ? DestinoLancamentoFatura.RejeitarPaga
            : DestinoLancamentoFatura.ReabrirEUsar;
    }

    private static bool EstaNaJanelaDoCicloAtual(DateOnly dataFechamento, DateOnly hoje, DateOnly fechamentoCicloAtual)
    {
        return dataFechamento >= hoje && dataFechamento <= fechamentoCicloAtual;
    }

    // Id da atual quando ela existe e nao esta paga; null caso contrario (ninguem vira Aberta).
    private static Guid? SelecionarAtualNaoPagaId(List<Fatura> faturas, DateOnly hoje, DateOnly fechamentoCicloAtual)
    {
        var atual = SelecionarAtual(faturas, hoje, fechamentoCicloAtual);
        return atual is { Status: not StatusFatura.Paga } ? atual.Id : null;
    }

    private static StatusFatura DeterminarStatusReconciliado(
        Fatura fatura,
        DateOnly hoje,
        DateOnly fechamentoCicloAtual,
        Guid? atualNaoPagaId)
    {
        var statusCalendario = ClassificarPorCalendario(fatura.DataFechamento, hoje, fechamentoCicloAtual);

        if (statusCalendario != StatusFatura.Aberta)
        {
            return statusCalendario;
        }

        return fatura.Id == atualNaoPagaId ? StatusFatura.Aberta : StatusFatura.Futura;
    }
}
