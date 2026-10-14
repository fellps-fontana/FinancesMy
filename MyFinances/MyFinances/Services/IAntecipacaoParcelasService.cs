using MyFinances.Domain;

namespace MyFinances.Services;

public interface IAntecipacaoParcelasService
{
    // Leitura sem efeito colateral: usa a fatura ABERTA atual da conta e devolve
    // a compra (para o total de parcelas) e as parcelas elegiveis (maior ParcelaNumero primeiro).
    // Erros: compra inexistente/de outra conta; conta nao-Cartao; sem fatura aberta.
    Task<(bool Sucesso, CompraParcelada? Compra, IReadOnlyList<Lancamento>? Parcelas, string? Erro)> ObterElegiveisAsync(
        Guid contaId,
        Guid compraParceladaId);

    // Move as `quantidade` parcelas selecionadas para a fatura ABERTA atual:
    // FaturaId -> fatura aberta, Data -> dataAntecipacao, Antecipada -> true. Valor,
    // ParcelaNumero, CompraParceladaId e Categoria intactos. Nada criado nem removido,
    // nenhuma fatura fechada ou criada. Sem pagamento, transferencia ou desconto.
    // Transacao unica. Repetivel.
    Task<(bool Sucesso, Fatura? FaturaDestino, IReadOnlyList<Lancamento>? Parcelas, string? Erro)> AnteciparAsync(
        Guid contaId,
        Guid compraParceladaId,
        int quantidade,
        DateOnly dataAntecipacao);
}
