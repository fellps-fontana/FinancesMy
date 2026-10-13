using MyFinances.Domain;

namespace MyFinances.DTOs;

public class LancamentoResponseDto
{
    public Guid Id { get; set; }

    public Guid ContaId { get; set; }

    public Guid? CategoriaId { get; set; }

    public string? Descricao { get; set; }

    public decimal Valor { get; set; }

    public string Tipo { get; set; } = string.Empty;

    // Classificacao (regra-de-negocio.md itens 2, 3, 12): ENTRADA | SAIDA |
    // TRANSFERENCIA | COMPETENCIA_CARTAO. Reaproveita
    // ClassificacaoLancamentoService (ja usado em ProjecaoMesService) -
    // nunca reclassifica aqui. O front usa este campo, nunca o sinal de
    // Valor nem Tipo isolado, pra decidir o que soma no resumo do mes /
    // filtro de chip.
    public string Classificacao { get; set; } = string.Empty;

    public DateOnly Data { get; set; }

    public string Status { get; set; } = string.Empty;

    public bool Manual { get; set; }

    public bool Oculto { get; set; }

    // Item 18: true quando LancamentoVinculoResolver.EhAvulso. A UI habilita
    // Editar/Excluir so quando true.
    public bool Avulso { get; set; }

    // Item 18: AVULSO | CONTA_FIXA | COMPRA_PARCELADA | CARTAO | CONTA_RECEBER |
    // TRANSFERENCIA | CONCILIADO | NAO_MANUAL. Diz a UI onde editar.
    public string OrigemVinculo { get; set; } = string.Empty;

    // Item 17: exibicao "Notebook 3/10". Nulos quando nao e parcela.
    public Guid? CompraParceladaId { get; set; }

    public int? ParcelaNumero { get; set; }

    // = CompraParcelada.QuantidadeParcelas; exige Include de CompraParcelada.
    public int? TotalParcelas { get; set; }

    public static LancamentoResponseDto FromLancamento(Lancamento lancamento)
    {
        return new()
        {
            Id = lancamento.Id,
            ContaId = lancamento.ContaId,
            CategoriaId = lancamento.CategoriaId,
            Descricao = lancamento.Descricao,
            Valor = lancamento.Valor,
            Tipo = lancamento.Tipo.ToStorageValue(),
            Classificacao = ClassificacaoLancamentoService.Classificar(lancamento).ToStorageValue(),
            Data = lancamento.Data,
            Status = lancamento.Status.ToStorageValue(),
            Manual = lancamento.Manual,
            Oculto = lancamento.Oculto,
            Avulso = LancamentoVinculoResolver.EhAvulso(lancamento),
            OrigemVinculo = LancamentoVinculoResolver.ResolverOrigem(lancamento).ToStorageValue(),
            CompraParceladaId = lancamento.CompraParceladaId,
            ParcelaNumero = lancamento.ParcelaNumero,
            TotalParcelas = lancamento.CompraParcelada?.QuantidadeParcelas
        };
    }
}
