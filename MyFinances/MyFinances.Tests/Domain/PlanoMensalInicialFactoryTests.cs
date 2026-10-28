using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Domain;

public class PlanoMensalInicialFactoryTests
{
    #region CalcularRendaPrevistaInicial: MENSAL, ANUAL, SEMANAL e moldes inativos

    [Fact]
    public void CalcularRendaPrevistaInicial_MoldeMensalComDia31EmFevereiro_UmaOcorrenciaComValorDoMolde()
    {
        var moldes = new[] { MoldeMensal(valor: 1000m, diaVencimento: 31) };

        var renda = PlanoMensalInicialFactory.CalcularRendaPrevistaInicial(moldes, 2026, 2);

        Assert.Equal(1000m, renda);
    }

    [Fact]
    public void CalcularRendaPrevistaInicial_MoldeAnualNoMesReferencia_SomaValorDaOcorrencia()
    {
        var moldes = new[] { MoldeAnual(valor: 5000m, mesReferencia: 7, diaVencimento: 15) };

        var renda = PlanoMensalInicialFactory.CalcularRendaPrevistaInicial(moldes, 2026, 7);

        Assert.Equal(5000m, renda);
    }

    [Fact]
    public void CalcularRendaPrevistaInicial_MoldeAnualForaDoMesReferencia_RetornaZero()
    {
        var moldes = new[] { MoldeAnual(valor: 5000m, mesReferencia: 7, diaVencimento: 15) };

        var renda = PlanoMensalInicialFactory.CalcularRendaPrevistaInicial(moldes, 2026, 8);

        Assert.Equal(0m, renda);
    }

    [Fact]
    public void CalcularRendaPrevistaInicial_MoldeSemanalMesComCincoQuartas_SomaCincoOcorrencias()
    {
        // Setembro de 2026 comeca numa terca e tem 5 quartas (2, 9, 16, 23, 30).
        var moldes = new[] { MoldeSemanal(valor: 100m, DiaDaSemana.Quarta) };

        var renda = PlanoMensalInicialFactory.CalcularRendaPrevistaInicial(moldes, 2026, 9);

        Assert.Equal(500m, renda);
    }

    [Fact]
    public void CalcularRendaPrevistaInicial_MoldeSemanalMesComQuatroQuartas_SomaQuatroOcorrencias()
    {
        // Fevereiro de 2026 comeca num domingo e tem 4 quartas (4, 11, 18, 25).
        var moldes = new[] { MoldeSemanal(valor: 100m, DiaDaSemana.Quarta) };

        var renda = PlanoMensalInicialFactory.CalcularRendaPrevistaInicial(moldes, 2026, 2);

        Assert.Equal(400m, renda);
    }

    [Fact]
    public void CalcularRendaPrevistaInicial_MoldeInativo_NaoEntraNaRenda()
    {
        var molde = MoldeMensal(valor: 1000m, diaVencimento: 10);
        molde.Ativa = false;

        var renda = PlanoMensalInicialFactory.CalcularRendaPrevistaInicial(new[] { molde }, 2026, 8);

        Assert.Equal(0m, renda);
    }

    [Fact]
    public void CalcularRendaPrevistaInicial_VariosMoldesAtivosEUmInativo_SomaSomenteOsAtivos()
    {
        var mensalAtivo = MoldeMensal(valor: 1000m, diaVencimento: 10);
        var mensalInativo = MoldeMensal(valor: 3000m, diaVencimento: 10);
        mensalInativo.Ativa = false;
        var semanalAtivo = MoldeSemanal(valor: 100m, DiaDaSemana.Quarta);

        var renda = PlanoMensalInicialFactory.CalcularRendaPrevistaInicial(
            new[] { mensalAtivo, mensalInativo, semanalAtivo }, 2026, 2);

        Assert.Equal(1400m, renda);
    }

    [Fact]
    public void CalcularRendaPrevistaInicial_MesFuturo_UsaOcorrenciasDoMesInformado()
    {
        var moldes = new[] { MoldeMensal(valor: 1000m, diaVencimento: 10) };

        var renda = PlanoMensalInicialFactory.CalcularRendaPrevistaInicial(moldes, 2030, 6);

        Assert.Equal(1000m, renda);
    }

    [Fact]
    public void CalcularRendaPrevistaInicial_SemMoldes_RetornaZero()
    {
        var renda = PlanoMensalInicialFactory.CalcularRendaPrevistaInicial(
            Array.Empty<RecebivelRecorrente>(), 2026, 8);

        Assert.Equal(0m, renda);
    }

    #endregion

    #region Criar: plano sem anterior

    [Fact]
    public void Criar_SemPlanoAnterior_NascePlanoComRendaInformadaInvestimentoZeroESemSublimites()
    {
        var plano = PlanoMensalInicialFactory.Criar(
            2026, 9, 1500m, null, new Dictionary<Guid, Categoria>());

        Assert.Equal(2026, plano.Ano);
        Assert.Equal(9, plano.Mes);
        Assert.Equal(1500m, plano.RendaPrevista);
        Assert.Equal(0m, plano.PercentualInvestimento);
        Assert.Empty(plano.Sublimites);
    }

    [Fact]
    public void Criar_SemPlanoAnterior_PlanoTemIdNovo()
    {
        var plano = PlanoMensalInicialFactory.Criar(
            2026, 9, 1500m, null, new Dictionary<Guid, Categoria>());

        Assert.NotEqual(Guid.Empty, plano.Id);
    }

    #endregion

    #region Criar: copia do plano anterior

    [Fact]
    public void Criar_ComPlanoAnterior_CopiaPercentualDeInvestimento()
    {
        var anterior = PlanoAnterior(percentualInvestimento: 20m);

        var plano = PlanoMensalInicialFactory.Criar(
            2026, 9, 1500m, anterior, new Dictionary<Guid, Categoria>());

        Assert.Equal(20m, plano.PercentualInvestimento);
    }

    [Fact]
    public void Criar_ComPlanoAnterior_CopiaSublimitesComCategoriaEPercentual()
    {
        var alimentacaoId = Guid.NewGuid();
        var lazerId = Guid.NewGuid();
        var categorias = DicionarioDe(
            CriarCategoria(alimentacaoId),
            CriarCategoria(lazerId));
        var anterior = PlanoAnterior(20m,
            CriarSublimite(alimentacaoId, 30m),
            CriarSublimite(lazerId, 10m));

        var plano = PlanoMensalInicialFactory.Criar(2026, 9, 1500m, anterior, categorias);

        Assert.Equal(2, plano.Sublimites.Count);
        Assert.Contains(plano.Sublimites, s => s.CategoriaId == alimentacaoId && s.Percentual == 30m);
        Assert.Contains(plano.Sublimites, s => s.CategoriaId == lazerId && s.Percentual == 10m);
    }

    [Fact]
    public void Criar_ComPlanoAnterior_SublimitesCopiadosRecebemIdsNovos()
    {
        var alimentacaoId = Guid.NewGuid();
        var categorias = DicionarioDe(CriarCategoria(alimentacaoId));
        var sublimiteAnterior = CriarSublimite(alimentacaoId, 30m);
        var anterior = PlanoAnterior(20m, sublimiteAnterior);

        var plano = PlanoMensalInicialFactory.Criar(2026, 9, 1500m, anterior, categorias);

        Assert.NotEqual(anterior.Id, plano.Id);
        var copiado = Assert.Single(plano.Sublimites);
        Assert.NotEqual(Guid.Empty, copiado.Id);
        Assert.NotEqual(sublimiteAnterior.Id, copiado.Id);
    }

    [Fact]
    public void Criar_ComPlanoAnterior_RendaPrevistaInformadaEhUsadaNaoACopiadaDoAnterior()
    {
        var anterior = PlanoAnterior(20m);
        anterior.RendaPrevista = 9999m;

        var plano = PlanoMensalInicialFactory.Criar(
            2026, 9, 1500m, anterior, new Dictionary<Guid, Categoria>());

        Assert.Equal(1500m, plano.RendaPrevista);
    }

    [Fact]
    public void Criar_ComPlanoAnterior_SublimitesApontamParaOPlanoNovo()
    {
        // [REVISAR] A regra nao diz explicitamente o PlanoMensalId do sublimite copiado;
        // o teste assume a FK apontando para o plano novo (nunca para o anterior).
        var alimentacaoId = Guid.NewGuid();
        var categorias = DicionarioDe(CriarCategoria(alimentacaoId));
        var anterior = PlanoAnterior(20m, CriarSublimite(alimentacaoId, 30m));

        var plano = PlanoMensalInicialFactory.Criar(2026, 9, 1500m, anterior, categorias);

        var copiado = Assert.Single(plano.Sublimites);
        Assert.Equal(plano.Id, copiado.PlanoMensalId);
    }

    #endregion

    #region Criar: exclusoes da copia (categoria arquivada, ausente e hierarquia)

    [Fact]
    public void Criar_ComPlanoAnterior_CategoriaArquivada_NaoEhCopiada()
    {
        var arquivadaId = Guid.NewGuid();
        var ativaId = Guid.NewGuid();
        var categorias = DicionarioDe(
            CriarCategoria(arquivadaId, arquivada: true),
            CriarCategoria(ativaId));
        var anterior = PlanoAnterior(20m,
            CriarSublimite(arquivadaId, 30m),
            CriarSublimite(ativaId, 10m));

        var plano = PlanoMensalInicialFactory.Criar(2026, 9, 1500m, anterior, categorias);

        var copiado = Assert.Single(plano.Sublimites);
        Assert.Equal(ativaId, copiado.CategoriaId);
        Assert.Equal(10m, copiado.Percentual);
    }

    [Fact]
    public void Criar_ComPlanoAnterior_CategoriaAusenteDoDicionario_NaoEhCopiada()
    {
        var ausenteId = Guid.NewGuid();
        var ativaId = Guid.NewGuid();
        var categorias = DicionarioDe(CriarCategoria(ativaId));
        var anterior = PlanoAnterior(20m,
            CriarSublimite(ausenteId, 30m),
            CriarSublimite(ativaId, 10m));

        var plano = PlanoMensalInicialFactory.Criar(2026, 9, 1500m, anterior, categorias);

        var copiado = Assert.Single(plano.Sublimites);
        Assert.Equal(ativaId, copiado.CategoriaId);
    }

    [Fact]
    public void Criar_ComPlanoAnterior_SubcategoriaComPaiArquivado_NaoEhCopiada()
    {
        var paiId = Guid.NewGuid();
        var subId = Guid.NewGuid();
        var categorias = DicionarioDe(
            CriarCategoria(paiId, arquivada: true),
            CriarCategoria(subId, parentId: paiId));
        var anterior = PlanoAnterior(20m, CriarSublimite(subId, 15m));

        var plano = PlanoMensalInicialFactory.Criar(2026, 9, 1500m, anterior, categorias);

        Assert.Empty(plano.Sublimites);
    }

    [Fact]
    public void Criar_ComPlanoAnterior_SubcategoriaComPaiAusenteDoDicionario_NaoEhCopiada()
    {
        var paiAusenteId = Guid.NewGuid();
        var subId = Guid.NewGuid();
        var categorias = DicionarioDe(CriarCategoria(subId, parentId: paiAusenteId));
        var anterior = PlanoAnterior(20m, CriarSublimite(subId, 15m));

        var plano = PlanoMensalInicialFactory.Criar(2026, 9, 1500m, anterior, categorias);

        Assert.Empty(plano.Sublimites);
    }

    [Fact]
    public void Criar_ComPlanoAnterior_SubcategoriaComPaiCopiado_EhCopiadaComPercentualOriginal()
    {
        var paiId = Guid.NewGuid();
        var subId = Guid.NewGuid();
        var categorias = DicionarioDe(
            CriarCategoria(paiId),
            CriarCategoria(subId, parentId: paiId));
        var anterior = PlanoAnterior(20m,
            CriarSublimite(paiId, 40m),
            CriarSublimite(subId, 15m));

        var plano = PlanoMensalInicialFactory.Criar(2026, 9, 1500m, anterior, categorias);

        Assert.Equal(2, plano.Sublimites.Count);
        Assert.Contains(plano.Sublimites, s => s.CategoriaId == paiId && s.Percentual == 40m);
        Assert.Contains(plano.Sublimites, s => s.CategoriaId == subId && s.Percentual == 15m);
    }

    #endregion

    private static RecebivelRecorrente MoldeMensal(decimal valor, int diaVencimento) => new()
    {
        Id = Guid.NewGuid(),
        Descricao = "Receita mensal",
        Valor = valor,
        Periodicidade = PeriodicidadeRecebivel.Mensal,
        DiaVencimento = diaVencimento,
        Ativa = true
    };

    private static RecebivelRecorrente MoldeAnual(decimal valor, int mesReferencia, int diaVencimento) => new()
    {
        Id = Guid.NewGuid(),
        Descricao = "Receita anual",
        Valor = valor,
        Periodicidade = PeriodicidadeRecebivel.Anual,
        MesReferencia = mesReferencia,
        DiaVencimento = diaVencimento,
        Ativa = true
    };

    private static RecebivelRecorrente MoldeSemanal(decimal valor, DiaDaSemana diaDaSemana) => new()
    {
        Id = Guid.NewGuid(),
        Descricao = "Receita semanal",
        Valor = valor,
        Periodicidade = PeriodicidadeRecebivel.Semanal,
        DiaDaSemana = diaDaSemana,
        Ativa = true
    };

    private static Categoria CriarCategoria(Guid id, Guid? parentId = null, bool arquivada = false) => new()
    {
        Id = id,
        Nome = "Categoria de teste",
        Tipo = TipoCategoria.Despesa,
        ParentId = parentId,
        Arquivada = arquivada
    };

    private static Dictionary<Guid, Categoria> DicionarioDe(params Categoria[] categorias) =>
        categorias.ToDictionary(c => c.Id);

    private static PlanoMensalSublimite CriarSublimite(Guid categoriaId, decimal percentual) => new()
    {
        Id = Guid.NewGuid(),
        CategoriaId = categoriaId,
        Percentual = percentual
    };

    private static PlanoMensal PlanoAnterior(decimal percentualInvestimento, params PlanoMensalSublimite[] sublimites) => new()
    {
        Id = Guid.NewGuid(),
        Ano = 2026,
        Mes = 8,
        RendaPrevista = 9999m,
        PercentualInvestimento = percentualInvestimento,
        Sublimites = sublimites.ToList()
    };
}
