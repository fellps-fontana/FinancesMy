using MyFinances.Domain;
using MyFinances.Models;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests;

public class ClassificacaoLancamentoServiceTests
{
    private Conta CriarContaMinima()
    {
        return new Conta
        {
            Id = Guid.NewGuid(),
            Nome = "Conta Teste",
            Origem = OrigemConstants.Manual,
            Tipo = TipoContaConstants.Banco,
            Ativa = true
        };
    }

    // Caso 1: Tipo=DEBIT sem TransferenciaId e FaturaId -> Saida
    [Fact]
    public void Classificar_DebitSemTransferenciaOuFatura_RetornaSaida()
    {
        var conta = CriarContaMinima();
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Valor = 100m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Status = LancamentoStatusConstants.Pago,
            TransferenciaId = null,
            FaturaId = null,
            Conta = conta
        };

        var resultado = ClassificacaoLancamentoService.Classificar(lancamento);

        Assert.Equal(ClassificacaoLancamento.Saida, resultado);
    }

    // Caso 2: Tipo=CREDIT sem TransferenciaId e FaturaId -> Entrada
    [Fact]
    public void Classificar_CreditSemTransferenciaOuFatura_RetornaEntrada()
    {
        var conta = CriarContaMinima();
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Valor = 150m,
            Tipo = TipoLancamentoConstants.Credit,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Status = LancamentoStatusConstants.Pago,
            TransferenciaId = null,
            FaturaId = null,
            Conta = conta
        };

        var resultado = ClassificacaoLancamentoService.Classificar(lancamento);

        Assert.Equal(ClassificacaoLancamento.Entrada, resultado);
    }

    // Caso 3: TransferenciaId preenchido com Tipo=DEBIT -> Transferencia
    // Prova que TransferenciaId tem precedencia sobre Tipo (regra: precedencia TransferenciaId > FaturaId > Tipo)
    [Fact]
    public void Classificar_DebitComTransferenciaId_RetornaTransferencia()
    {
        var conta = CriarContaMinima();
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Valor = 200m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Status = LancamentoStatusConstants.Pago,
            TransferenciaId = Guid.NewGuid(),
            FaturaId = null,
            Conta = conta
        };

        var resultado = ClassificacaoLancamentoService.Classificar(lancamento);

        Assert.Equal(ClassificacaoLancamento.Transferencia, resultado);
    }

    // Caso 4: TransferenciaId preenchido com Tipo=CREDIT -> Transferencia
    // Prova que TransferenciaId ignora o sinal e sempre retorna Transferencia
    [Fact]
    public void Classificar_CreditComTransferenciaId_RetornaTransferencia()
    {
        var conta = CriarContaMinima();
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Valor = 300m,
            Tipo = TipoLancamentoConstants.Credit,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Status = LancamentoStatusConstants.Pago,
            TransferenciaId = Guid.NewGuid(),
            FaturaId = null,
            Conta = conta
        };

        var resultado = ClassificacaoLancamentoService.Classificar(lancamento);

        Assert.Equal(ClassificacaoLancamento.Transferencia, resultado);
    }

    // Caso 5: FaturaId preenchido -> CompetenciaCartao
    // Prova que FaturaId tem precedencia sobre Tipo (mas nao sobre TransferenciaId)
    [Fact]
    public void Classificar_ComFaturaId_RetornaCompetenciaCartao()
    {
        var conta = CriarContaMinima();
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Valor = 250m,
            Tipo = TipoLancamentoConstants.Debit,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Status = LancamentoStatusConstants.Pago,
            TransferenciaId = null,
            FaturaId = Guid.NewGuid(),
            Conta = conta
        };

        var resultado = ClassificacaoLancamentoService.Classificar(lancamento);

        Assert.Equal(ClassificacaoLancamento.CompetenciaCartao, resultado);
    }

    // Caso 6 (EXTRA): Tipo=CREDIT com Valor negativo -> Entrada
    // Prova que a funcao NAO olha para o sinal bruto de Valor
    // Regra critica: "usar SEMPRE o campo tipo (DEBIT | CREDIT) combinado com o account_type para classificar entrada/saida. Nunca somar valor cru."
    [Fact]
    public void Classificar_CreditComValorNegativo_RetornaEntradaIgnorandoSinal()
    {
        var conta = CriarContaMinima();
        var lancamento = new Lancamento
        {
            Id = Guid.NewGuid(),
            ContaId = conta.Id,
            Valor = -500m, // Valor negativo propositalmente
            Tipo = TipoLancamentoConstants.Credit,
            Data = DateOnly.FromDateTime(DateTime.Now),
            Status = LancamentoStatusConstants.Pago,
            TransferenciaId = null,
            FaturaId = null,
            Conta = conta
        };

        var resultado = ClassificacaoLancamentoService.Classificar(lancamento);

        // Espera Entrada porque Tipo=CREDIT, nao porque Valor > 0
        Assert.Equal(ClassificacaoLancamento.Entrada, resultado);
    }
}
