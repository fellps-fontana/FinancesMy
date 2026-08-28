namespace MyFinances.Services;

// Motor unico de materializacao de ocorrencias de RecebivelRecorrente
// (regra-de-negocio.md item 15). Idempotencia mora aqui. Chamado so por
// RecebivelRecorrenteService (gatilhos criar/reativar/editar/desativar), pelo
// RecebivelRecorrenteMaterializacaoJob e pela rede de seguranca da projecao
// (ProjecaoMesService) -- nunca direto pelo controller.
public interface IRecebivelRecorrenteGeradorService
{
    // Materializa (idempotente) as ocorrencias do molde com data em
    // [primeiro dia do mes de dataReferencia, dataReferencia + LookaheadDias].
    // Retorna quantas conta_receber foram criadas.
    Task<int> MaterializarOcorrenciasAsync(Guid recebivelRecorrenteId, DateOnly dataReferencia);

    // Percorre todos os moldes ativos e materializa. Entrypoint do job e da
    // rede de seguranca da projecao do mes.
    Task MaterializarTodosAtivosAsync(DateOnly dataReferencia);

    // Hard delete das ocorrencias conta_receber do molde ainda com status PENDENTE.
    // Preserva PARCIAL/RECEBIDO (fato consumado). Usado no desativar.
    Task<int> RemoverOcorrenciasPendentesAsync(Guid recebivelRecorrenteId);

    // Recalcula o conjunto sob a config ATUAL do molde: hard delete das PENDENTE
    // fora do conjunto, depois materializa as que faltam. Usado no EditarAsync
    // quando periodicidade / dia_vencimento / mes_referencia / dia_da_semana mudam.
    Task<int> RegenerarOcorrenciasAsync(Guid recebivelRecorrenteId, DateOnly dataReferencia);
}
