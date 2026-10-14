using MyFinances.Domain;

namespace MyFinances.Repositories;

public interface IFaturaRepository
{
    Task Adicionar(Fatura fatura);
    Task<Fatura?> ObterPorId(Guid id);
    Task<IEnumerable<Fatura>> ListarPorConta(Guid contaId);
    Task<Fatura?> ObterFaturaAbertaPorConta(Guid contaId);

    // Fatura do ciclo (conta + data_fechamento exata). Entidade rastreada.
    Task<Fatura?> ObterPorContaEDataFechamento(Guid contaId, DateOnly dataFechamento);

    // Faturas da conta relevantes para reconciliar (status Aberta/Futura/Paga ou
    // DataFechamento >= hoje), ordem DataFechamento asc. Rastreadas.
    Task<IReadOnlyList<Fatura>> ListarParaReconciliacao(Guid contaId, DateOnly hoje);
    Task<IEnumerable<Fatura>> ListarFaturasCartaoPorVencimentoNoMes(int ano, int mes);
    Task Atualizar(Fatura fatura);
    Task Salvar();
}
