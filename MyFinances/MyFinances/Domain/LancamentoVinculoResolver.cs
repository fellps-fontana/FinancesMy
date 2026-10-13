namespace MyFinances.Domain;

// Regra-de-negocio item 18. Fonte unica da pergunta "este lancamento e
// avulso? se nao, quem e o dono?".
public static class LancamentoVinculoResolver
{
    // Avulso = Manual == true E todos os vinculos nulos. Invariante por construcao:
    // EhAvulso(l) <=> ResolverOrigem(l) == Avulso.
    public static bool EhAvulso(Lancamento lancamento)
    {
        return ResolverOrigem(lancamento) == OrigemVinculoLancamento.Avulso;
    }

    // Precedencia (primeiro que casar vence). A regra 18 so lista os vinculos;
    // a ordem aqui e escolha de implementacao, nao decisao de dominio.
    public static OrigemVinculoLancamento ResolverOrigem(Lancamento lancamento)
    {
        ArgumentNullException.ThrowIfNull(lancamento);

        if (!lancamento.Manual)
        {
            return OrigemVinculoLancamento.NaoManual;
        }

        if (lancamento.TransferenciaId != null)
        {
            return OrigemVinculoLancamento.Transferencia;
        }

        if (lancamento.CompraParceladaId != null)
        {
            return OrigemVinculoLancamento.CompraParcelada;
        }

        if (lancamento.ContaFixaId != null)
        {
            return OrigemVinculoLancamento.ContaFixa;
        }

        if (lancamento.FaturaId != null || lancamento.AssinaturaCartaoId != null)
        {
            return OrigemVinculoLancamento.Cartao;
        }

        if (lancamento.ContaReceberId != null)
        {
            return OrigemVinculoLancamento.ContaReceber;
        }

        if (lancamento.ConciliadoCom != null)
        {
            return OrigemVinculoLancamento.Conciliado;
        }

        return OrigemVinculoLancamento.Avulso;
    }
}
