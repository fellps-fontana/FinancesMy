using MyFinances.DTOs;

namespace MyFinances.Services;

// Item 17 da regra-de-negocio: despesa parcelada em conta banco.
public interface ILancamentoParceladoService
{
    // Validacao (primeira falha nao grava nada): quantidade >= 2; descricao; valor > 0;
    // status PENDENTE|PAGO; conta existe, ativa, manual, nao Cartao; categoria (se
    // informada) existe e e DESPESA. Valores via ParcelamentoCalculator (resto na
    // ultima), datas via ParcelamentoContaBancoDatas. Parcela 1 com o status do
    // formulario, demais PENDENTE; fatura_id null. Transacao unica.
    Task<(bool Sucesso, LancamentoParceladoResponse? Resposta, string? Erro)> CriarAsync(
        Guid contaId,
        CriarLancamentoParceladoRequest request);

    // Exclui (hard delete) so parcelas PENDENTE. Compra inexistente ou de outra conta ->
    // ErroCompraNaoEncontrada; compra de cartao (FaturaId) recusada; sem PENDENTE -> erro.
    // A linha compra_parcelada nunca e removida (historico).
    // Excluidas = PENDENTE removidas; Mantidas = demais (PAGO).
    Task<(bool Sucesso, int Excluidas, int Mantidas, string? Erro)> CancelarAsync(
        Guid contaId,
        Guid compraParceladaId);
}
