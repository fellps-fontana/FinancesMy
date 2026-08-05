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
            Oculto = lancamento.Oculto
        };
    }
}
