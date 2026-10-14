using MyFinances.Domain;

namespace MyFinances.Services;

// Motor unico de geracao de ocorrencias de ContaFixa (regra-de-negocio.md
// item 6 + item 12). Substitui ContaFixaService.GerarLancamentosPendentes
// como ponto central: escolhe o destino (Lancamento PENDENTE se
// Conta.Tipo=Banco; Compra via CompraCartaoService se Conta.Tipo=Cartao) a
// partir de contaFixa.Conta.Tipo.
public interface IRecorrenciaGeradorService
{
    // Gera (idempotente) a ocorrencia atual e a proxima de UMA ContaFixa,
    // a partir de dataReferencia. Chamado nos gatilhos criar/reativar.
    Task<int> GerarOcorrenciaAtualEProximaAsync(Guid contaFixaId, DateOnly dataReferencia);

    // Geracao sob demanda (item 9): garante que TODAS as ContaFixa ativas
    // (banco e cartao) tenham a ocorrencia de ano/mes gerada, quando
    // EhOcorrenciaValida for true e ainda nao existir. Chamado no topo do
    // calculo de projecao do mes.
    Task GarantirOcorrenciasAtivasDoMesAsync(int ano, int mes);

    // Geracao sob demanda (item 12): garante a ocorrencia de UMA ContaFixa
    // para ano/mes especifico. Chamado pelos endpoints de fatura.
    Task<bool> GarantirOcorrenciaDoMesAsync(Guid contaFixaId, int ano, int mes);

    // Limpeza de periodicidade (item 6): recalcula o conjunto correto sob
    // periodicidadeNova/mesReferenciaNovo e exclui (hard delete) as
    // ocorrencias geradas que nao batem mais -- respeitando fato consumado
    // (Lancamento.Status=Pago para banco; Fatura.Status=Paga para cartao,
    // nunca tocados). Chamado por ContaFixaService.EditarAsync.
    Task<int> LimparOcorrenciasForaDaPeriodicidadeAsync(
        Guid contaFixaId,
        PeriodicidadeContaFixa periodicidadeNova,
        int? mesReferenciaNovo);

    // Desativar/Apagar (item 6): exclui (hard delete) TODAS as ocorrencias
    // nao consumadas da ContaFixa, sem recalcular periodicidade.
    // Banco (Fatura == null): Lancamento.Status = Pendente.
    // Cartao (Fatura != null): Fatura.Status != Paga.
    // Mesmo criterio de "nao consumada" da limpeza de periodicidade (predicado compartilhado).
    // Retorna a quantidade removida; ContaFixa inexistente -> 0. Salva.
    Task<int> RemoverOcorrenciasNaoConsumadasAsync(Guid contaFixaId);

    // Apagar (item 6): nas ocorrencias consumadas (banco: Status = Pago;
    // cartao: Fatura.Status = Paga) seta ContaFixaId = null. Valor, data,
    // categoria, descricao, status e fatura permanecem intactos.
    // Retorna a quantidade desvinculada; ContaFixa inexistente -> 0. Salva.
    // Roda DEPOIS de RemoverOcorrenciasNaoConsumadasAsync e ANTES de remover o molde.
    Task<int> DesvincularOcorrenciasConsumadasAsync(Guid contaFixaId);
}
