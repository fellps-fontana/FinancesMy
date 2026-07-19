using MyFinances.Domain;

namespace MyFinances.Services;

internal static class TransferenciaLancamentoHelper
{
    public static (Lancamento Saida, Lancamento Entrada) CriarLancamentos(
        Transferencia transferencia,
        Guid contaOrigemId,
        Guid contaDestinoId)
    {
        var lancamentoSaida = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaOrigemId,
            Descricao = transferencia.Descricao,
            Valor = transferencia.Valor,
            Tipo = TipoLancamento.Debit,
            Data = transferencia.Data,
            Status = StatusLancamento.Pago,
            Manual = true,
            Oculto = false,
            TransferenciaId = transferencia.Id
        };

        var lancamentoEntrada = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = contaDestinoId,
            Descricao = transferencia.Descricao,
            Valor = transferencia.Valor,
            Tipo = TipoLancamento.Credit,
            Data = transferencia.Data,
            Status = StatusLancamento.Pago,
            Manual = true,
            Oculto = false,
            TransferenciaId = transferencia.Id
        };

        return (lancamentoSaida, lancamentoEntrada);
    }
}
