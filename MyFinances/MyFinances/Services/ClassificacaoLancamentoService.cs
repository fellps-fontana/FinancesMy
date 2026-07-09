using MyFinances.Domain;
using MyFinances.Models;

namespace MyFinances.Services;

public static class ClassificacaoLancamentoService
{
    public static ClassificacaoLancamento Classificar(Lancamento lancamento)
    {
        if (lancamento.TransferenciaId != null)
        {
            return ClassificacaoLancamento.Transferencia;
        }

        if (lancamento.FaturaId != null)
        {
            return ClassificacaoLancamento.CompetenciaCartao;
        }

        return lancamento.Tipo == TipoLancamentoConstants.Debit
            ? ClassificacaoLancamento.Saida
            : ClassificacaoLancamento.Entrada;
    }
}
