namespace MyFinances.Domain;

public enum ClassificacaoLancamento
{
    Entrada,
    Saida,
    Transferencia,
    CompetenciaCartao
}

// Serializacao para o contrato de API (LancamentoResponseDto.Classificacao).
// So converte o enum ja calculado por ClassificacaoLancamentoService para
// string - nao reclassifica nada aqui. Mesmo padrao de
// TipoLancamentoExtensions (Domain/TipoLancamento.cs).
public static class ClassificacaoLancamentoExtensions
{
    public static string ToStorageValue(this ClassificacaoLancamento classificacao) => classificacao switch
    {
        ClassificacaoLancamento.Entrada => "ENTRADA",
        ClassificacaoLancamento.Saida => "SAIDA",
        ClassificacaoLancamento.Transferencia => "TRANSFERENCIA",
        ClassificacaoLancamento.CompetenciaCartao => "COMPETENCIA_CARTAO",
        _ => throw new ArgumentOutOfRangeException(nameof(classificacao))
    };
}
