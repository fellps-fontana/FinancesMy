using MyFinances.Domain;
using MyFinances.DTOs;
using Xunit;

namespace MyFinances.Tests.DTOs;

public class LancamentoResponseDtoTests
{
    // Teste 1: ClassificacaoLancamento.Entrada serializa para "ENTRADA"
    [Fact]
    public void ToStorageValue_Entrada_RetornaEntrada()
    {
        // Arrange
        var classificacao = ClassificacaoLancamento.Entrada;

        // Act
        var resultado = classificacao.ToStorageValue();

        // Assert
        Assert.Equal("ENTRADA", resultado);
    }

    // Teste 2: ClassificacaoLancamento.Saida serializa para "SAIDA"
    [Fact]
    public void ToStorageValue_Saida_RetornaSaida()
    {
        // Arrange
        var classificacao = ClassificacaoLancamento.Saida;

        // Act
        var resultado = classificacao.ToStorageValue();

        // Assert
        Assert.Equal("SAIDA", resultado);
    }

    // Teste 3: ClassificacaoLancamento.Transferencia serializa para "TRANSFERENCIA"
    [Fact]
    public void ToStorageValue_Transferencia_RetornaTransferencia()
    {
        // Arrange
        var classificacao = ClassificacaoLancamento.Transferencia;

        // Act
        var resultado = classificacao.ToStorageValue();

        // Assert
        Assert.Equal("TRANSFERENCIA", resultado);
    }

    // Teste 4: ClassificacaoLancamento.CompetenciaCartao serializa para "COMPETENCIA_CARTAO"
    [Fact]
    public void ToStorageValue_CompetenciaCartao_RetornaCompetenciaCartao()
    {
        // Arrange
        var classificacao = ClassificacaoLancamento.CompetenciaCartao;

        // Act
        var resultado = classificacao.ToStorageValue();

        // Assert
        Assert.Equal("COMPETENCIA_CARTAO", resultado);
    }

    // Teste 5: FromLancamento popula Classificacao corretamente para um lancamento
    // com TransferenciaId (caso de pagamento de fatura de cartao)
    // Prova que a regra critica de nao somar transferencia no resumo do mes funciona:
    // o campo Classificacao deve refletir corretamente que e uma transferencia.
    [Fact]
    public void FromLancamento_ComTransferenciaId_PopulaClassificacaoComoTransferencia()
    {
        // Arrange
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Valor = 500m,
            Tipo = TipoLancamento.Debit,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Status = StatusLancamento.Pago,
            TransferenciaId = Guid.NewGuid(),
            FaturaId = null,
            Descricao = "Pagamento de fatura",
            Manual = true,
            Oculto = false,
            CategoriaId = null
        };

        // Act
        var dto = LancamentoResponseDto.FromLancamento(lancamento);

        // Assert
        Assert.Equal("TRANSFERENCIA", dto.Classificacao);
    }

    // Teste 6: FromLancamento popula Classificacao corretamente para um lancamento
    // com FaturaId (compra no cartao em regime de competencia)
    [Fact]
    public void FromLancamento_ComFaturaId_PopulaClassificacaoComoCompetenciaCartao()
    {
        // Arrange
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Valor = 150m,
            Tipo = TipoLancamento.Debit,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Status = StatusLancamento.Pago,
            TransferenciaId = null,
            FaturaId = Guid.NewGuid(),
            Descricao = "Compra no cartao",
            Manual = true,
            Oculto = false,
            CategoriaId = null
        };

        // Act
        var dto = LancamentoResponseDto.FromLancamento(lancamento);

        // Assert
        Assert.Equal("COMPETENCIA_CARTAO", dto.Classificacao);
    }

    // Teste 7: FromLancamento popula Classificacao como SAIDA para um debit simples
    // (sem transferencia ou fatura)
    [Fact]
    public void FromLancamento_DebitSemTransferenciaOuFatura_PopulaClassificacaoComoSaida()
    {
        // Arrange
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Valor = 250m,
            Tipo = TipoLancamento.Debit,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Status = StatusLancamento.Pago,
            TransferenciaId = null,
            FaturaId = null,
            Descricao = "Gasto simples",
            Manual = true,
            Oculto = false,
            CategoriaId = null
        };

        // Act
        var dto = LancamentoResponseDto.FromLancamento(lancamento);

        // Assert
        Assert.Equal("SAIDA", dto.Classificacao);
    }

    // Teste 8: FromLancamento popula Classificacao como ENTRADA para um credit simples
    // (sem transferencia ou fatura)
    [Fact]
    public void FromLancamento_CreditSemTransferenciaOuFatura_PopulaClassificacaoComoEntrada()
    {
        // Arrange
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = Guid.NewGuid(),
            Valor = 1000m,
            Tipo = TipoLancamento.Credit,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Status = StatusLancamento.Pago,
            TransferenciaId = null,
            FaturaId = null,
            Descricao = "Salario recebido",
            Manual = true,
            Oculto = false,
            CategoriaId = null
        };

        // Act
        var dto = LancamentoResponseDto.FromLancamento(lancamento);

        // Assert
        Assert.Equal("ENTRADA", dto.Classificacao);
    }

    // Teste 9: FromLancamento copia todos os outros campos alem de Classificacao
    [Fact]
    public void FromLancamento_CopiaCorretamenteTodosOsCampos()
    {
        // Arrange
        var lancamentoId = Guid.NewGuid();
        var contaId = Guid.NewGuid();
        var categoriaId = Guid.NewGuid();
        var data = DateOnly.FromDateTime(DateTime.Now);

        var lancamento = new Lancamento
        {
            Id = lancamentoId,
            ContaId = contaId,
            CategoriaId = categoriaId,
            Valor = 500m,
            Tipo = TipoLancamento.Debit,
            Data = data,
            Status = StatusLancamento.Pendente,
            TransferenciaId = null,
            FaturaId = null,
            Descricao = "Teste completo",
            Manual = false,
            Oculto = false
        };

        // Act
        var dto = LancamentoResponseDto.FromLancamento(lancamento);

        // Assert
        Assert.Equal(lancamentoId, dto.Id);
        Assert.Equal(contaId, dto.ContaId);
        Assert.Equal(categoriaId, dto.CategoriaId);
        Assert.Equal(500m, dto.Valor);
        Assert.Equal("DEBIT", dto.Tipo);
        Assert.Equal(data, dto.Data);
        Assert.Equal("PENDENTE", dto.Status);
        Assert.Equal("Teste completo", dto.Descricao);
        Assert.False(dto.Manual);
        Assert.False(dto.Oculto);
        Assert.Equal("SAIDA", dto.Classificacao);
    }
}
