using MyFinances.Domain;
using Xunit;

namespace MyFinances.Tests.Domain;

public class PlanejamentoMensalCalculatorTests
{
    private const int Ano = 2026;
    private const int Mes = 3;

    private static readonly DateOnly DentroDoMes = new(2026, 3, 15);
    private static readonly DateOnly PrimeiroDiaDoMes = new(2026, 3, 1);
    private static readonly DateOnly UltimoDiaDoMes = new(2026, 3, 31);
    private static readonly DateOnly DiaAnteriorAoMes = new(2026, 2, 28);
    private static readonly DateOnly OutroMes = new(2026, 4, 1);

    #region Regra: conta fixa e assinatura de cartao entram em fixas (EhFixa)

    [Fact]
    public void EhFixa_ContaFixaPendenteNoMesDoPlano_RetornaTrue()
    {
        var lancamento = NovoLancamento(1000m, contaFixaId: Guid.NewGuid(), status: StatusLancamento.Pendente);

        Assert.True(PlanejamentoMensalCalculator.EhFixa(lancamento, Ano, Mes));
    }

    [Fact]
    public void EhFixa_ContaFixaPagaNoMesDoPlano_RetornaTrue()
    {
        var lancamento = NovoLancamento(1000m, contaFixaId: Guid.NewGuid(), status: StatusLancamento.Pago);

        Assert.True(PlanejamentoMensalCalculator.EhFixa(lancamento, Ano, Mes));
    }

    [Fact]
    public void EhFixa_AssinaturaDeCartaoNoMesDoPlano_RetornaTrue()
    {
        var lancamento = NovoLancamento(200m, assinaturaCartaoId: Guid.NewGuid());

        Assert.True(PlanejamentoMensalCalculator.EhFixa(lancamento, Ano, Mes));
    }

    [Fact]
    public void EhFixa_LancamentoSemContaFixaNemAssinatura_RetornaFalse()
    {
        var lancamento = NovoLancamento(100m, categoriaId: Guid.NewGuid());

        Assert.False(PlanejamentoMensalCalculator.EhFixa(lancamento, Ano, Mes));
    }

    [Fact]
    public void EhFixa_ParcelaDeCompraParcelada_RetornaFalse()
    {
        // Parcela nunca e fixa: entra no sublimite da categoria no mes da propria data.
        var lancamento = NovoLancamento(90m, categoriaId: Guid.NewGuid(), compraParceladaId: Guid.NewGuid());

        Assert.False(PlanejamentoMensalCalculator.EhFixa(lancamento, Ano, Mes));
    }

    [Fact]
    public void EhFixa_ContaFixaComTipoCredit_RetornaFalse()
    {
        var lancamento = NovoLancamento(1000m, tipo: TipoLancamento.Credit, contaFixaId: Guid.NewGuid());

        Assert.False(PlanejamentoMensalCalculator.EhFixa(lancamento, Ano, Mes));
    }

    [Fact]
    public void EhFixa_ContaFixaOculta_RetornaFalse()
    {
        var lancamento = NovoLancamento(1000m, oculto: true, contaFixaId: Guid.NewGuid());

        Assert.False(PlanejamentoMensalCalculator.EhFixa(lancamento, Ano, Mes));
    }

    [Fact]
    public void EhFixa_ContaFixaComTransferencia_RetornaFalse()
    {
        var lancamento = NovoLancamento(1000m, contaFixaId: Guid.NewGuid(), transferenciaId: Guid.NewGuid());

        Assert.False(PlanejamentoMensalCalculator.EhFixa(lancamento, Ano, Mes));
    }

    [Fact]
    public void EhFixa_ContaFixaComStatusSugerido_RetornaFalse()
    {
        var lancamento = NovoLancamento(1000m, contaFixaId: Guid.NewGuid(), status: StatusLancamento.Sugerido);

        Assert.False(PlanejamentoMensalCalculator.EhFixa(lancamento, Ano, Mes));
    }

    [Fact]
    public void EhFixa_ContaFixaDeOutroMes_RetornaFalse()
    {
        var lancamento = NovoLancamento(1000m, data: OutroMes, contaFixaId: Guid.NewGuid());

        Assert.False(PlanejamentoMensalCalculator.EhFixa(lancamento, Ano, Mes));
    }

    #endregion

    #region Regra: gasto variavel e debito elegivel sem vinculo fixo (EhGastoVariavel)

    [Fact]
    public void EhGastoVariavel_DebitoSemVinculoFixoPendente_RetornaTrue()
    {
        var lancamento = NovoLancamento(100m, categoriaId: Guid.NewGuid(), status: StatusLancamento.Pendente);

        Assert.True(PlanejamentoMensalCalculator.EhGastoVariavel(lancamento, Ano, Mes));
    }

    [Fact]
    public void EhGastoVariavel_ParcelaDeCompraParcelada_RetornaTrue()
    {
        var lancamento = NovoLancamento(90m, categoriaId: Guid.NewGuid(), compraParceladaId: Guid.NewGuid());

        Assert.True(PlanejamentoMensalCalculator.EhGastoVariavel(lancamento, Ano, Mes));
    }

    [Fact]
    public void EhGastoVariavel_PrimeiroDiaDoMes_RetornaTrue()
    {
        var lancamento = NovoLancamento(100m, data: PrimeiroDiaDoMes, categoriaId: Guid.NewGuid());

        Assert.True(PlanejamentoMensalCalculator.EhGastoVariavel(lancamento, Ano, Mes));
    }

    [Fact]
    public void EhGastoVariavel_UltimoDiaDoMes_RetornaTrue()
    {
        var lancamento = NovoLancamento(100m, data: UltimoDiaDoMes, categoriaId: Guid.NewGuid());

        Assert.True(PlanejamentoMensalCalculator.EhGastoVariavel(lancamento, Ano, Mes));
    }

    [Fact]
    public void EhGastoVariavel_ContaFixa_RetornaFalse()
    {
        var lancamento = NovoLancamento(1000m, contaFixaId: Guid.NewGuid());

        Assert.False(PlanejamentoMensalCalculator.EhGastoVariavel(lancamento, Ano, Mes));
    }

    [Fact]
    public void EhGastoVariavel_AssinaturaDeCartao_RetornaFalse()
    {
        var lancamento = NovoLancamento(200m, assinaturaCartaoId: Guid.NewGuid());

        Assert.False(PlanejamentoMensalCalculator.EhGastoVariavel(lancamento, Ano, Mes));
    }

    [Fact]
    public void EhGastoVariavel_ComTipoCredit_RetornaFalse()
    {
        var lancamento = NovoLancamento(100m, tipo: TipoLancamento.Credit, categoriaId: Guid.NewGuid());

        Assert.False(PlanejamentoMensalCalculator.EhGastoVariavel(lancamento, Ano, Mes));
    }

    [Fact]
    public void EhGastoVariavel_Oculto_RetornaFalse()
    {
        var lancamento = NovoLancamento(100m, oculto: true, categoriaId: Guid.NewGuid());

        Assert.False(PlanejamentoMensalCalculator.EhGastoVariavel(lancamento, Ano, Mes));
    }

    [Fact]
    public void EhGastoVariavel_ComTransferencia_RetornaFalse()
    {
        var lancamento = NovoLancamento(100m, transferenciaId: Guid.NewGuid(), categoriaId: Guid.NewGuid());

        Assert.False(PlanejamentoMensalCalculator.EhGastoVariavel(lancamento, Ano, Mes));
    }

    [Fact]
    public void EhGastoVariavel_StatusSugerido_RetornaFalse()
    {
        var lancamento = NovoLancamento(100m, status: StatusLancamento.Sugerido, categoriaId: Guid.NewGuid());

        Assert.False(PlanejamentoMensalCalculator.EhGastoVariavel(lancamento, Ano, Mes));
    }

    [Fact]
    public void EhGastoVariavel_DiaAnteriorAoMes_RetornaFalse()
    {
        var lancamento = NovoLancamento(100m, data: DiaAnteriorAoMes, categoriaId: Guid.NewGuid());

        Assert.False(PlanejamentoMensalCalculator.EhGastoVariavel(lancamento, Ano, Mes));
    }

    [Fact]
    public void EhGastoVariavel_DataNoMesSeguinte_RetornaFalse()
    {
        var lancamento = NovoLancamento(100m, data: OutroMes, categoriaId: Guid.NewGuid());

        Assert.False(PlanejamentoMensalCalculator.EhGastoVariavel(lancamento, Ano, Mes));
    }

    #endregion

    #region Regra: fixas = soma(valor) dos lancamentos EhFixa

    [Fact]
    public void CalcularFixas_ContaFixaEAssinatura_SomaSoOsElegiveisDoMes()
    {
        var lancamentos = new[]
        {
            NovoLancamento(1000m, contaFixaId: Guid.NewGuid()),
            NovoLancamento(200m, assinaturaCartaoId: Guid.NewGuid()),
            NovoLancamento(500m, categoriaId: Guid.NewGuid()),
            NovoLancamento(900m, tipo: TipoLancamento.Credit, contaFixaId: Guid.NewGuid()),
            NovoLancamento(800m, oculto: true, contaFixaId: Guid.NewGuid()),
            NovoLancamento(700m, data: OutroMes, contaFixaId: Guid.NewGuid())
        };

        var fixas = PlanejamentoMensalCalculator.CalcularFixas(lancamentos, Ano, Mes);

        Assert.Equal(1200m, fixas);
    }

    [Fact]
    public void CalcularFixas_SemLancamentos_RetornaZero()
    {
        var fixas = PlanejamentoMensalCalculator.CalcularFixas(Array.Empty<Lancamento>(), Ano, Mes);

        Assert.Equal(0m, fixas);
    }

    #endregion

    #region Regra: investimento planejado = renda * percentual / 100

    [Fact]
    public void CalcularInvestimentoPlanejado_RendaEPercentual_RetornaRendaVezesPercentualSobre100()
    {
        var planejado = PlanejamentoMensalCalculator.CalcularInvestimentoPlanejado(5000m, 10m);

        Assert.Equal(500m, planejado);
    }

    [Fact]
    public void CalcularInvestimentoPlanejado_PercentualZero_RetornaZero()
    {
        var planejado = PlanejamentoMensalCalculator.CalcularInvestimentoPlanejado(5000m, 0m);

        Assert.Equal(0m, planejado);
    }

    [Fact]
    public void CalcularInvestimentoPlanejado_PercentualCem_RetornaRendaInteira()
    {
        var planejado = PlanejamentoMensalCalculator.CalcularInvestimentoPlanejado(1000m, 100m);

        Assert.Equal(1000m, planejado);
    }

    #endregion

    #region Regra: investimento = max(0, min(planejado, renda - fixas))

    [Fact]
    public void CalcularInvestimento_SobraSuficiente_RetornaPlanejado()
    {
        var investimento = PlanejamentoMensalCalculator.CalcularInvestimento(5000m, 1000m, 500m);

        Assert.Equal(500m, investimento);
    }

    [Fact]
    public void CalcularInvestimento_RendaMenosFixasIgualAoPlanejado_RetornaPlanejado()
    {
        var investimento = PlanejamentoMensalCalculator.CalcularInvestimento(1000m, 500m, 500m);

        Assert.Equal(500m, investimento);
    }

    [Fact]
    public void CalcularInvestimento_NaoCabe_CaiSoAteSobrarZero()
    {
        // renda - fixas = 200 < planejado 500: investimento cai para 200, nunca abaixo de zero
        var investimento = PlanejamentoMensalCalculator.CalcularInvestimento(1000m, 800m, 500m);

        Assert.Equal(200m, investimento);
    }

    [Fact]
    public void CalcularInvestimento_RendaMenosFixasNegativa_RetornaZero()
    {
        var investimento = PlanejamentoMensalCalculator.CalcularInvestimento(1000m, 1300m, 100m);

        Assert.Equal(0m, investimento);
    }

    [Fact]
    public void CalcularInvestimento_PlanejadoZero_RetornaZero()
    {
        var investimento = PlanejamentoMensalCalculator.CalcularInvestimento(1000m, 0m, 0m);

        Assert.Equal(0m, investimento);
    }

    #endregion

    #region Regra: sobra = max(0, renda - fixas - investimento), nunca negativa

    [Fact]
    public void CalcularSobra_RendaFixasEInvestimento_RetornaDiferenca()
    {
        var sobra = PlanejamentoMensalCalculator.CalcularSobra(5000m, 1000m, 500m);

        Assert.Equal(3500m, sobra);
    }

    [Fact]
    public void CalcularSobra_FixasMaisInvestimentoMaioresQueRenda_RetornaZeroSemNegativo()
    {
        var sobra = PlanejamentoMensalCalculator.CalcularSobra(1000m, 700m, 500m);

        Assert.Equal(0m, sobra);
    }

    #endregion

    #region Regra: sublimite 1o nivel = sobra * percentual / 100 e subcategoria = pai * percentual / 100

    [Fact]
    public void CalcularSublimitePrimeiroNivel_SobraVezesPercentual_RetornaValor()
    {
        var valor = PlanejamentoMensalCalculator.CalcularSublimitePrimeiroNivel(3500m, 25m);

        Assert.Equal(875m, valor);
    }

    [Fact]
    public void CalcularSublimitePrimeiroNivel_SobraZero_RetornaZero()
    {
        var valor = PlanejamentoMensalCalculator.CalcularSublimitePrimeiroNivel(0m, 50m);

        Assert.Equal(0m, valor);
    }

    [Fact]
    public void CalcularSublimiteSubcategoria_ValorDoPaiVezesPercentual_RetornaValor()
    {
        var valor = PlanejamentoMensalCalculator.CalcularSublimiteSubcategoria(875m, 20m);

        Assert.Equal(175m, valor);
    }

    [Fact]
    public void CalcularSublimiteSubcategoria_PaiComValorZero_RetornaZero()
    {
        var valor = PlanejamentoMensalCalculator.CalcularSublimiteSubcategoria(0m, 40m);

        Assert.Equal(0m, valor);
    }

    #endregion

    #region Regra: nao_alocado = sobra - soma(sublimites de primeiro nivel em R$)

    [Fact]
    public void CalcularNaoAlocado_SobraMenosSomaDosSublimites_RetornaDiferenca()
    {
        var naoAlocado = PlanejamentoMensalCalculator.CalcularNaoAlocado(3500m, new[] { 875m, 350m });

        Assert.Equal(2275m, naoAlocado);
    }

    [Fact]
    public void CalcularNaoAlocado_SemSublimites_RetornaSobraInteira()
    {
        var naoAlocado = PlanejamentoMensalCalculator.CalcularNaoAlocado(3500m, Array.Empty<decimal>());

        Assert.Equal(3500m, naoAlocado);
    }

    [Fact]
    public void CalcularNaoAlocado_SublimitesConsomemTodaSobra_RetornaZero()
    {
        var naoAlocado = PlanejamentoMensalCalculator.CalcularNaoAlocado(3500m, new[] { 2000m, 1500m });

        Assert.Equal(0m, naoAlocado);
    }

    #endregion

    #region Regra: gasto do sublimite (item 14) - primeiro nivel soma proprias + subcategorias diretas

    [Fact]
    public void CalcularGastoDoSublimite_SomaSoAsCategoriasInformadas()
    {
        var alimentacao = NovaCategoria("Alimentacao");
        var outra = NovaCategoria("Outra");
        var lancamentos = new[]
        {
            NovoLancamento(100m, categoriaId: alimentacao.Id),
            NovoLancamento(50m, categoriaId: alimentacao.Id),
            NovoLancamento(300m, categoriaId: outra.Id)
        };

        var gasto = PlanejamentoMensalCalculator.CalcularGastoDoSublimite(lancamentos, new[] { alimentacao.Id }, Ano, Mes);

        Assert.Equal(150m, gasto);
    }

    [Fact]
    public void CalcularGastoDoSublimite_PaiSomaSubcategoriasDiretas()
    {
        var pai = NovaCategoria("Moradia");
        var subcategoria = NovaCategoria("Aluguel", pai);
        var outra = NovaCategoria("Lazer");
        var lancamentos = new[]
        {
            NovoLancamento(100m, categoriaId: pai.Id),
            NovoLancamento(200m, categoriaId: subcategoria.Id),
            NovoLancamento(999m, categoriaId: outra.Id)
        };

        var gasto = PlanejamentoMensalCalculator.CalcularGastoDoSublimite(
            lancamentos,
            new[] { pai.Id, subcategoria.Id },
            Ano,
            Mes);

        Assert.Equal(300m, gasto);
    }

    [Fact]
    public void CalcularGastoDoSublimite_ExcluiContaFixaEAssinatura()
    {
        var categoria = NovaCategoria("Moradia");
        var lancamentos = new[]
        {
            NovoLancamento(100m, categoriaId: categoria.Id),
            NovoLancamento(200m, categoriaId: categoria.Id, contaFixaId: Guid.NewGuid()),
            NovoLancamento(300m, categoriaId: categoria.Id, assinaturaCartaoId: Guid.NewGuid())
        };

        var gasto = PlanejamentoMensalCalculator.CalcularGastoDoSublimite(lancamentos, new[] { categoria.Id }, Ano, Mes);

        Assert.Equal(100m, gasto);
    }

    [Fact]
    public void CalcularGastoDoSublimite_IncluiParcelaDoMes()
    {
        var categoria = NovaCategoria("Eletronicos");
        var lancamentos = new[]
        {
            NovoLancamento(100m, categoriaId: categoria.Id),
            NovoLancamento(90m, categoriaId: categoria.Id, compraParceladaId: Guid.NewGuid())
        };

        var gasto = PlanejamentoMensalCalculator.CalcularGastoDoSublimite(lancamentos, new[] { categoria.Id }, Ano, Mes);

        Assert.Equal(190m, gasto);
    }

    [Fact]
    public void CalcularGastoDoSublimite_ExcluiCredit()
    {
        var categoria = NovaCategoria("Mercado");
        var lancamentos = new[]
        {
            NovoLancamento(100m, categoriaId: categoria.Id),
            NovoLancamento(40m, tipo: TipoLancamento.Credit, categoriaId: categoria.Id)
        };

        var gasto = PlanejamentoMensalCalculator.CalcularGastoDoSublimite(lancamentos, new[] { categoria.Id }, Ano, Mes);

        Assert.Equal(100m, gasto);
    }

    [Fact]
    public void CalcularGastoDoSublimite_ExcluiOculto()
    {
        var categoria = NovaCategoria("Mercado");
        var lancamentos = new[]
        {
            NovoLancamento(100m, categoriaId: categoria.Id),
            NovoLancamento(60m, oculto: true, categoriaId: categoria.Id)
        };

        var gasto = PlanejamentoMensalCalculator.CalcularGastoDoSublimite(lancamentos, new[] { categoria.Id }, Ano, Mes);

        Assert.Equal(100m, gasto);
    }

    [Fact]
    public void CalcularGastoDoSublimite_ExcluiTransferencia()
    {
        var categoria = NovaCategoria("Mercado");
        var lancamentos = new[]
        {
            NovoLancamento(100m, categoriaId: categoria.Id),
            NovoLancamento(70m, transferenciaId: Guid.NewGuid(), categoriaId: categoria.Id)
        };

        var gasto = PlanejamentoMensalCalculator.CalcularGastoDoSublimite(lancamentos, new[] { categoria.Id }, Ano, Mes);

        Assert.Equal(100m, gasto);
    }

    [Fact]
    public void CalcularGastoDoSublimite_ExcluiStatusSugerido()
    {
        var categoria = NovaCategoria("Mercado");
        var lancamentos = new[]
        {
            NovoLancamento(100m, categoriaId: categoria.Id),
            NovoLancamento(80m, status: StatusLancamento.Sugerido, categoriaId: categoria.Id)
        };

        var gasto = PlanejamentoMensalCalculator.CalcularGastoDoSublimite(lancamentos, new[] { categoria.Id }, Ano, Mes);

        Assert.Equal(100m, gasto);
    }

    [Fact]
    public void CalcularGastoDoSublimite_ExcluiLancamentoDeOutroMes()
    {
        var categoria = NovaCategoria("Mercado");
        var lancamentos = new[]
        {
            NovoLancamento(100m, categoriaId: categoria.Id),
            NovoLancamento(500m, data: OutroMes, categoriaId: categoria.Id)
        };

        var gasto = PlanejamentoMensalCalculator.CalcularGastoDoSublimite(lancamentos, new[] { categoria.Id }, Ano, Mes);

        Assert.Equal(100m, gasto);
    }

    [Fact]
    public void CalcularGastoDoSublimite_SemLancamentos_RetornaZero()
    {
        var categoria = NovaCategoria("Mercado");

        var gasto = PlanejamentoMensalCalculator.CalcularGastoDoSublimite(
            Array.Empty<Lancamento>(),
            new[] { categoria.Id },
            Ano,
            Mes);

        Assert.Equal(0m, gasto);
    }

    [Fact]
    public void CalcularGastoDoSublimite_ListaDeCategoriasVazia_RetornaZero()
    {
        var categoria = NovaCategoria("Mercado");
        var lancamentos = new[] { NovoLancamento(100m, categoriaId: categoria.Id) };

        var gasto = PlanejamentoMensalCalculator.CalcularGastoDoSublimite(
            lancamentos,
            Array.Empty<Guid>(),
            Ano,
            Mes);

        Assert.Equal(0m, gasto);
    }

    #endregion

    #region Regra: CalcularGastosPorCategoria retorna gasto variavel de todas as categorias DESPESA (item 14 e item 20)

    [Fact]
    public void CalcularGastosPorCategoria_IncluiDespesaAtivaArquivadaPrimeiroNivelESubcategoriaEExcluiReceita()
    {
        var despesaAtiva = NovaCategoria("Alimentacao", tipo: TipoCategoria.Despesa, arquivada: false);
        var subcategoriaDespesa = NovaCategoria("Restaurantes", pai: despesaAtiva, tipo: TipoCategoria.Despesa, arquivada: false);
        var despesaArquivada = NovaCategoria("Antiga", tipo: TipoCategoria.Despesa, arquivada: true);
        var receita = NovaCategoria("Salario", tipo: TipoCategoria.Receita);
        var subcategoriaReceita = NovaCategoria("Bonus", pai: receita, tipo: TipoCategoria.Receita);
        var categorias = new[] { despesaAtiva, subcategoriaDespesa, despesaArquivada, receita, subcategoriaReceita };

        var resultado = PlanejamentoMensalCalculator.CalcularGastosPorCategoria(
            Array.Empty<Lancamento>(),
            categorias,
            Ano,
            Mes);

        var ids = resultado.Select(gasto => gasto.CategoriaId).ToList();
        Assert.Equal(3, ids.Count);
        Assert.Contains(despesaAtiva.Id, ids);
        Assert.Contains(subcategoriaDespesa.Id, ids);
        Assert.Contains(despesaArquivada.Id, ids);
        Assert.DoesNotContain(receita.Id, ids);
        Assert.DoesNotContain(subcategoriaReceita.Id, ids);
    }

    [Fact]
    public void CalcularGastosPorCategoria_PrimeiroNivelSomaPropriosESubcategoriasDiretasESubcategoriaSomaApenasProprios()
    {
        var alimentacao = NovaCategoria("Alimentacao");
        var restaurantes = NovaCategoria("Restaurantes", alimentacao);
        var mercado = NovaCategoria("Mercado", alimentacao);
        var lazer = NovaCategoria("Lazer");
        var categorias = new[] { alimentacao, restaurantes, mercado, lazer };
        var lancamentos = new[]
        {
            NovoLancamento(100m, categoriaId: alimentacao.Id),
            NovoLancamento(150m, categoriaId: restaurantes.Id),
            NovoLancamento(50m, categoriaId: restaurantes.Id, status: StatusLancamento.Pendente),
            NovoLancamento(200m, categoriaId: mercado.Id),
            NovoLancamento(80m, categoriaId: lazer.Id)
        };

        var resultado = PlanejamentoMensalCalculator.CalcularGastosPorCategoria(lancamentos, categorias, Ano, Mes);

        Assert.Equal(500m, GastoDe(resultado, alimentacao.Id).GastoRealizado);
        Assert.Equal(200m, GastoDe(resultado, restaurantes.Id).GastoRealizado);
        Assert.Equal(200m, GastoDe(resultado, mercado.Id).GastoRealizado);
        Assert.Equal(80m, GastoDe(resultado, lazer.Id).GastoRealizado);
    }

    [Fact]
    public void CalcularGastosPorCategoria_CategoriaDespesaSemGastoNoMes_ApareceComGastoRealizadoZero()
    {
        var comGasto = NovaCategoria("Alimentacao");
        var semGasto = NovaCategoria("Lazer");
        var subSemGasto = NovaCategoria("Bares", comGasto);
        var categorias = new[] { comGasto, semGasto, subSemGasto };
        var lancamentos = new[] { NovoLancamento(120m, categoriaId: comGasto.Id) };

        var resultado = PlanejamentoMensalCalculator.CalcularGastosPorCategoria(lancamentos, categorias, Ano, Mes);

        Assert.Equal(120m, GastoDe(resultado, comGasto.Id).GastoRealizado);
        Assert.Equal(0m, GastoDe(resultado, semGasto.Id).GastoRealizado);
        Assert.Equal(0m, GastoDe(resultado, subSemGasto.Id).GastoRealizado);
    }

    [Fact]
    public void CalcularGastosPorCategoria_IgnoraFixosOcultosTransferenciasCreditSugeridoEOutrosMeses()
    {
        var categoria = NovaCategoria("Moradia");
        var lancamentos = new[]
        {
            NovoLancamento(100m, categoriaId: categoria.Id),
            NovoLancamento(40m, categoriaId: categoria.Id, compraParceladaId: Guid.NewGuid()),
            NovoLancamento(200m, categoriaId: categoria.Id, contaFixaId: Guid.NewGuid()),
            NovoLancamento(300m, categoriaId: categoria.Id, assinaturaCartaoId: Guid.NewGuid()),
            NovoLancamento(400m, categoriaId: categoria.Id, oculto: true),
            NovoLancamento(500m, categoriaId: categoria.Id, transferenciaId: Guid.NewGuid()),
            NovoLancamento(600m, categoriaId: categoria.Id, tipo: TipoLancamento.Credit),
            NovoLancamento(700m, categoriaId: categoria.Id, status: StatusLancamento.Sugerido),
            NovoLancamento(800m, categoriaId: categoria.Id, data: DiaAnteriorAoMes),
            NovoLancamento(900m, categoriaId: categoria.Id, data: OutroMes)
        };

        var resultado = PlanejamentoMensalCalculator.CalcularGastosPorCategoria(
            lancamentos,
            new[] { categoria },
            Ano,
            Mes);

        Assert.Single(resultado);
        Assert.Equal(140m, GastoDe(resultado, categoria.Id).GastoRealizado);
    }

    [Fact]
    public void CalcularGastosPorCategoria_OrdenaDeterministicamentePorCategoriaId()
    {
        var idMenor = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var idMeio = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var idMaior = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        var catMaior = NovaCategoria("Alimentacao", id: idMaior);
        var catMeio = NovaCategoria("Restaurantes", pai: catMaior, id: idMeio);
        var catMenor = NovaCategoria("Zeladoria", id: idMenor);

        var resultado = PlanejamentoMensalCalculator.CalcularGastosPorCategoria(
            Array.Empty<Lancamento>(),
            new[] { catMaior, catMeio, catMenor },
            Ano,
            Mes);

        Assert.Equal(new[] { idMenor, idMeio, idMaior }, resultado.Select(gasto => gasto.CategoriaId));
    }

    #endregion

    #region Regra: gasto_sem_sublimite = categoria null OU nem ela nem o pai direto tem sublimite

    [Fact]
    public void CalcularGastoSemSublimite_CategoriaNula_EntraNoGasto()
    {
        var lancamentos = new[] { NovoLancamento(80m) };

        var gasto = PlanejamentoMensalCalculator.CalcularGastoSemSublimite(
            lancamentos,
            Array.Empty<Categoria>(),
            new HashSet<Guid>(),
            Ano,
            Mes);

        Assert.Equal(80m, gasto);
    }

    [Fact]
    public void CalcularGastoSemSublimite_CategoriaSemSublimiteENemPai_EntraNoGasto()
    {
        var categoria = NovaCategoria("Lazer");
        var lancamentos = new[] { NovoLancamento(100m, categoriaId: categoria.Id) };

        var gasto = PlanejamentoMensalCalculator.CalcularGastoSemSublimite(
            lancamentos,
            new[] { categoria },
            new HashSet<Guid>(),
            Ano,
            Mes);

        Assert.Equal(100m, gasto);
    }

    [Fact]
    public void CalcularGastoSemSublimite_CategoriaComSublimite_NaoEntraNoGasto()
    {
        var categoria = NovaCategoria("Mercado");
        var lancamentos = new[] { NovoLancamento(100m, categoriaId: categoria.Id) };

        var gasto = PlanejamentoMensalCalculator.CalcularGastoSemSublimite(
            lancamentos,
            new[] { categoria },
            new HashSet<Guid> { categoria.Id },
            Ano,
            Mes);

        Assert.Equal(0m, gasto);
    }

    [Fact]
    public void CalcularGastoSemSublimite_SubcategoriaSemSublimiteDeCategoriaComSublimite_NaoEntraNoGasto()
    {
        var pai = NovaCategoria("Moradia");
        var subcategoria = NovaCategoria("Aluguel", pai);
        var lancamentos = new[] { NovoLancamento(100m, categoriaId: subcategoria.Id) };

        var gasto = PlanejamentoMensalCalculator.CalcularGastoSemSublimite(
            lancamentos,
            new[] { pai, subcategoria },
            new HashSet<Guid> { pai.Id },
            Ano,
            Mes);

        Assert.Equal(0m, gasto);
    }

    [Fact]
    public void CalcularGastoSemSublimite_SubcategoriaSemSublimiteDePaiSemSublimite_EntraNoGasto()
    {
        var pai = NovaCategoria("Lazer");
        var subcategoria = NovaCategoria("Cinema", pai);
        var lancamentos = new[] { NovoLancamento(100m, categoriaId: subcategoria.Id) };

        var gasto = PlanejamentoMensalCalculator.CalcularGastoSemSublimite(
            lancamentos,
            new[] { pai, subcategoria },
            new HashSet<Guid>(),
            Ano,
            Mes);

        Assert.Equal(100m, gasto);
    }

    [Fact]
    public void CalcularGastoSemSublimite_SubcategoriaComSublimiteComPaiSemSublimite_NaoEntraNoGasto()
    {
        // Estado que a validacao impede; a regra literal exclui por "ela tem sublimite".
        var pai = NovaCategoria("Lazer");
        var subcategoria = NovaCategoria("Cinema", pai);
        var lancamentos = new[] { NovoLancamento(100m, categoriaId: subcategoria.Id) };

        var gasto = PlanejamentoMensalCalculator.CalcularGastoSemSublimite(
            lancamentos,
            new[] { pai, subcategoria },
            new HashSet<Guid> { subcategoria.Id },
            Ano,
            Mes);

        Assert.Equal(0m, gasto);
    }

    [Fact]
    public void CalcularGastoSemSublimite_ExcluiFixasEAssinatura()
    {
        var lancamentos = new[]
        {
            NovoLancamento(500m, contaFixaId: Guid.NewGuid()),
            NovoLancamento(400m, assinaturaCartaoId: Guid.NewGuid()),
            NovoLancamento(60m)
        };

        var gasto = PlanejamentoMensalCalculator.CalcularGastoSemSublimite(
            lancamentos,
            Array.Empty<Categoria>(),
            new HashSet<Guid>(),
            Ano,
            Mes);

        Assert.Equal(60m, gasto);
    }

    #endregion

    #region Regra: Calcular compoe os termos do item 20

    [Fact]
    public void Calcular_RendaFixasEInvestimento_CalculaValoresDoPlano()
    {
        var plano = NovoPlano(5000m, 10m);
        var lancamentos = new[]
        {
            NovoLancamento(1000m, contaFixaId: Guid.NewGuid()),
            NovoLancamento(200m)
        };

        var resultado = PlanejamentoMensalCalculator.Calcular(plano, lancamentos, Array.Empty<Categoria>());

        Assert.Equal(5000m, resultado.Renda);
        Assert.Equal(1000m, resultado.Fixas);
        Assert.Equal(10m, resultado.PercentualInvestimento);
        Assert.Equal(500m, resultado.InvestimentoPlanejado);
        Assert.Equal(500m, resultado.Investimento);
        Assert.Equal(3500m, resultado.Sobra);
        Assert.False(resultado.InvestimentoReduzido);
        Assert.False(resultado.PlanoNaoFecha);
    }

    [Fact]
    public void Calcular_GastoVariavelNaoReduzSobra()
    {
        var plano = NovoPlano(5000m, 10m);
        var lancamentos = new[]
        {
            NovoLancamento(1000m, contaFixaId: Guid.NewGuid()),
            NovoLancamento(200m)
        };

        var resultado = PlanejamentoMensalCalculator.Calcular(plano, lancamentos, Array.Empty<Categoria>());

        Assert.Equal(3500m, resultado.Sobra);
    }

    [Fact]
    public void Calcular_SublimitePrimeiroNivel_ValorEhSobraVezesPercentual()
    {
        var alimentacao = NovaCategoria("Alimentacao");
        var plano = NovoPlano(5000m, 10m, (alimentacao, 25m));
        var lancamentos = new[] { NovoLancamento(1000m, contaFixaId: Guid.NewGuid()) };

        var resultado = PlanejamentoMensalCalculator.Calcular(plano, lancamentos, new[] { alimentacao });

        Assert.Equal(875m, SublimiteDe(resultado, "Alimentacao").Valor);
    }

    [Fact]
    public void Calcular_NaoAlocado_EhSobraMenosSublimitesDePrimeiroNivel()
    {
        var alimentacao = NovaCategoria("Alimentacao");
        var moradia = NovaCategoria("Moradia");
        var plano = NovoPlano(5000m, 10m, (alimentacao, 25m), (moradia, 10m));
        var lancamentos = new[] { NovoLancamento(1000m, contaFixaId: Guid.NewGuid()) };

        var resultado = PlanejamentoMensalCalculator.Calcular(
            plano,
            lancamentos,
            new[] { alimentacao, moradia });

        Assert.Equal(2275m, resultado.NaoAlocado);
    }

    [Fact]
    public void Calcular_Subcategoria_ValorEhSublimitePaiVezesPercentual()
    {
        var alimentacao = NovaCategoria("Alimentacao");
        var bares = NovaCategoria("Bares", alimentacao);
        var plano = NovoPlano(5000m, 10m, (alimentacao, 25m), (bares, 20m));
        var lancamentos = new[] { NovoLancamento(1000m, contaFixaId: Guid.NewGuid()) };

        var resultado = PlanejamentoMensalCalculator.Calcular(
            plano,
            lancamentos,
            new[] { alimentacao, bares });

        Assert.Equal(175m, SublimiteDe(resultado, "Bares").Valor);
    }

    [Fact]
    public void Calcular_GastoDoPai_SomaProprioEDasSubcategoriasDiretas()
    {
        var alimentacao = NovaCategoria("Alimentacao");
        var bares = NovaCategoria("Bares", alimentacao);
        var plano = NovoPlano(5000m, 0m, (alimentacao, 25m), (bares, 20m));
        var lancamentos = new[]
        {
            NovoLancamento(100m, categoriaId: alimentacao.Id),
            NovoLancamento(200m, categoriaId: bares.Id),
            NovoLancamento(50m, categoriaId: bares.Id)
        };

        var resultado = PlanejamentoMensalCalculator.Calcular(plano, lancamentos, new[] { alimentacao, bares });

        Assert.Equal(350m, SublimiteDe(resultado, "Alimentacao").GastoRealizado);
        Assert.Equal(250m, SublimiteDe(resultado, "Bares").GastoRealizado);
    }

    [Fact]
    public void Calcular_GastoSemSublimite_SomaVariaveisSemSublimite()
    {
        var alimentacao = NovaCategoria("Alimentacao");
        var lazer = NovaCategoria("Lazer");
        var plano = NovoPlano(5000m, 0m, (alimentacao, 25m));
        var lancamentos = new[]
        {
            NovoLancamento(80m),
            NovoLancamento(40m, categoriaId: lazer.Id),
            NovoLancamento(999m, categoriaId: alimentacao.Id)
        };

        var resultado = PlanejamentoMensalCalculator.Calcular(plano, lancamentos, new[] { alimentacao, lazer });

        Assert.Equal(120m, resultado.GastoSemSublimite);
    }

    [Fact]
    public void Calcular_InvestimentoNaoCabe_ReduzEMarcaInvestimentoReduzido()
    {
        var plano = NovoPlano(1000m, 50m);
        var lancamentos = new[] { NovoLancamento(800m, contaFixaId: Guid.NewGuid()) };

        var resultado = PlanejamentoMensalCalculator.Calcular(plano, lancamentos, Array.Empty<Categoria>());

        Assert.Equal(500m, resultado.InvestimentoPlanejado);
        Assert.Equal(200m, resultado.Investimento);
        Assert.Equal(0m, resultado.Sobra);
        Assert.True(resultado.InvestimentoReduzido);
        Assert.False(resultado.PlanoNaoFecha);
    }

    [Fact]
    public void Calcular_InvestimentoIgualAoPlanejado_NaoMarcaInvestimentoReduzido()
    {
        var plano = NovoPlano(1000m, 10m);

        var resultado = PlanejamentoMensalCalculator.Calcular(plano, Array.Empty<Lancamento>(), Array.Empty<Categoria>());

        Assert.Equal(100m, resultado.Investimento);
        Assert.False(resultado.InvestimentoReduzido);
    }

    [Fact]
    public void Calcular_RendaIgualAsFixas_PlanoFecha()
    {
        var plano = NovoPlano(1000m, 0m);
        var lancamentos = new[] { NovoLancamento(1000m, contaFixaId: Guid.NewGuid()) };

        var resultado = PlanejamentoMensalCalculator.Calcular(plano, lancamentos, Array.Empty<Categoria>());

        Assert.False(resultado.PlanoNaoFecha);
        Assert.Equal(0m, resultado.Sobra);
    }

    [Fact]
    public void Calcular_RendaMenosFixasNegativa_ZeraInvestimentoSobraESublimitesEMarcaPlanoNaoFecha()
    {
        var alimentacao = NovaCategoria("Alimentacao");
        var plano = NovoPlano(1000m, 10m, (alimentacao, 30m));
        var lancamentos = new[] { NovoLancamento(1500m, contaFixaId: Guid.NewGuid()) };

        var resultado = PlanejamentoMensalCalculator.Calcular(plano, lancamentos, new[] { alimentacao });

        Assert.Equal(1500m, resultado.Fixas);
        Assert.Equal(0m, resultado.Investimento);
        Assert.Equal(0m, resultado.Sobra);
        Assert.Equal(0m, SublimiteDe(resultado, "Alimentacao").Valor);
        Assert.Equal(0m, resultado.NaoAlocado);
        Assert.True(resultado.PlanoNaoFecha);
        Assert.True(resultado.InvestimentoReduzido);
    }

    [Fact]
    public void Calcular_SublimiteZeroComGasto_EstouradoEFracaoZero()
    {
        var alimentacao = NovaCategoria("Alimentacao");
        var plano = NovoPlano(1000m, 10m, (alimentacao, 30m));
        var lancamentos = new[]
        {
            NovoLancamento(1500m, contaFixaId: Guid.NewGuid()),
            NovoLancamento(50m, categoriaId: alimentacao.Id)
        };

        var resultado = PlanejamentoMensalCalculator.Calcular(plano, lancamentos, new[] { alimentacao });
        var sublimite = SublimiteDe(resultado, "Alimentacao");

        Assert.Equal(0m, sublimite.Valor);
        Assert.Equal(50m, sublimite.GastoRealizado);
        Assert.Equal(0m, sublimite.FracaoUtilizada);
        Assert.True(sublimite.Estourado);
    }

    [Fact]
    public void Calcular_SublimiteSemGasto_NaoEstouradoEFracaoZero()
    {
        var alimentacao = NovaCategoria("Alimentacao");
        var plano = NovoPlano(1000m, 10m, (alimentacao, 30m));
        var lancamentos = new[] { NovoLancamento(1500m, contaFixaId: Guid.NewGuid()) };

        var resultado = PlanejamentoMensalCalculator.Calcular(plano, lancamentos, new[] { alimentacao });
        var sublimite = SublimiteDe(resultado, "Alimentacao");

        Assert.Equal(0m, sublimite.GastoRealizado);
        Assert.Equal(0m, sublimite.FracaoUtilizada);
        Assert.False(sublimite.Estourado);
    }

    [Fact]
    public void Calcular_GastoIgualAoValor_NaoEstouradoEFracaoUm()
    {
        var alimentacao = NovaCategoria("Alimentacao");
        var plano = NovoPlano(5000m, 0m, (alimentacao, 25m));
        var lancamentos = new[]
        {
            NovoLancamento(1000m, contaFixaId: Guid.NewGuid()),
            NovoLancamento(1000m, categoriaId: alimentacao.Id)
        };

        var resultado = PlanejamentoMensalCalculator.Calcular(plano, lancamentos, new[] { alimentacao });
        var sublimite = SublimiteDe(resultado, "Alimentacao");

        Assert.Equal(1000m, sublimite.Valor);
        Assert.False(sublimite.Estourado);
        Assert.Equal(1m, sublimite.FracaoUtilizada);
    }

    [Fact]
    public void Calcular_GastoAcimaDoValor_EstouradoEFracaoMaiorQueUm()
    {
        var alimentacao = NovaCategoria("Alimentacao");
        var plano = NovoPlano(5000m, 0m, (alimentacao, 25m));
        var lancamentos = new[]
        {
            NovoLancamento(1000m, contaFixaId: Guid.NewGuid()),
            NovoLancamento(1250m, categoriaId: alimentacao.Id)
        };

        var resultado = PlanejamentoMensalCalculator.Calcular(plano, lancamentos, new[] { alimentacao });
        var sublimite = SublimiteDe(resultado, "Alimentacao");

        Assert.True(sublimite.Estourado);
        Assert.Equal(1.25m, sublimite.FracaoUtilizada);
    }

    [Fact]
    public void Calcular_CategoriaArquivadaComSublimite_ExibeFlagERealizaGasto()
    {
        var alimentacao = NovaCategoria("Alimentacao", arquivada: true);
        var plano = NovoPlano(5000m, 0m, (alimentacao, 25m));
        var lancamentos = new[]
        {
            NovoLancamento(1000m, contaFixaId: Guid.NewGuid()),
            NovoLancamento(100m, categoriaId: alimentacao.Id)
        };

        var resultado = PlanejamentoMensalCalculator.Calcular(plano, lancamentos, new[] { alimentacao });
        var sublimite = SublimiteDe(resultado, "Alimentacao");

        Assert.True(sublimite.CategoriaArquivada);
        Assert.Equal(100m, sublimite.GastoRealizado);
    }

    [Fact]
    public void Calcular_CategoriaSemSublimiteNoPlano_NaoAparecaEmSublimites()
    {
        var alimentacao = NovaCategoria("Alimentacao");
        var lazer = NovaCategoria("Lazer");
        var plano = NovoPlano(5000m, 0m, (alimentacao, 25m));

        var resultado = PlanejamentoMensalCalculator.Calcular(
            plano,
            Array.Empty<Lancamento>(),
            new[] { alimentacao, lazer });

        var nomes = resultado.Sublimites.Select(sublimite => sublimite.Nome);
        Assert.Equal(new[] { "Alimentacao" }, nomes);
    }

    [Fact]
    public void Calcular_OrdemDosSublimites_PrimeiroNivelPorNomeESubcategoriaLogoAposOPai()
    {
        var alimentacao = NovaCategoria("Alimentacao");
        var bares = NovaCategoria("Bares", alimentacao);
        var moradia = NovaCategoria("Moradia");
        var condominio = NovaCategoria("Condominio", moradia);
        var aluguel = NovaCategoria("Aluguel", moradia);
        var transporte = NovaCategoria("Transporte");
        var uber = NovaCategoria("Uber", transporte);
        var abastecimento = NovaCategoria("Abastecimento", transporte);
        var plano = NovoPlano(
            10000m,
            0m,
            (uber, 10m),
            (moradia, 10m),
            (condominio, 20m),
            (transporte, 10m),
            (alimentacao, 10m),
            (aluguel, 30m),
            (bares, 5m),
            (abastecimento, 15m));
        var categorias = new[]
        {
            alimentacao, bares, moradia, condominio, aluguel, transporte, uber, abastecimento
        };

        var resultado = PlanejamentoMensalCalculator.Calcular(plano, Array.Empty<Lancamento>(), categorias);

        var nomes = resultado.Sublimites.Select(sublimite => sublimite.Nome);
        Assert.Equal(
            new[]
            {
                "Alimentacao", "Bares", "Moradia", "Aluguel", "Condominio", "Transporte", "Abastecimento", "Uber"
            },
            nomes);
    }

    [Fact]
    public void Calcular_FiltrosProprios_IgnoraLancamentosNaoElegiveis()
    {
        // Calcular nao confia na query: aplica filtros por conta propria.
        var plano = NovoPlano(1000m, 10m);
        var lancamentos = new[]
        {
            NovoLancamento(500m, tipo: TipoLancamento.Credit, contaFixaId: Guid.NewGuid()),
            NovoLancamento(300m, oculto: true, contaFixaId: Guid.NewGuid()),
            NovoLancamento(200m, transferenciaId: Guid.NewGuid()),
            NovoLancamento(100m, status: StatusLancamento.Sugerido),
            NovoLancamento(700m, data: OutroMes)
        };

        var resultado = PlanejamentoMensalCalculator.Calcular(plano, lancamentos, Array.Empty<Categoria>());

        Assert.Equal(0m, resultado.Fixas);
        Assert.Equal(0m, resultado.GastoSemSublimite);
        Assert.Equal(900m, resultado.Sobra);
        Assert.Equal(100m, resultado.Investimento);
    }

    [Fact]
    public void Calcular_InvarianteDeTotalVariavel_PrimeiroNivelMaisSemSublimiteIgualAoTotalDoMes()
    {
        // Total variavel do mes: 100 + 300 + 50 + 400 + 70 + 30 + 90 = 1040.
        // Fixas (1000 + 200) e excluidas, assim como credit, oculto, transferencia, sugerido e outro mes.
        const decimal totalVariavelDoMes = 1040m;

        var moradia = NovaCategoria("Moradia");
        var aluguel = NovaCategoria("Aluguel", moradia);
        var lazer = NovaCategoria("Lazer");
        var cinema = NovaCategoria("Cinema", lazer);
        var mercado = NovaCategoria("Mercado");
        var presentes = NovaCategoria("Presentes");
        var plano = NovoPlano(10000m, 0m, (moradia, 10m), (aluguel, 50m), (mercado, 20m));
        var lancamentos = new[]
        {
            NovoLancamento(1000m, categoriaId: moradia.Id, contaFixaId: Guid.NewGuid()),
            NovoLancamento(200m, categoriaId: lazer.Id, assinaturaCartaoId: Guid.NewGuid()),
            NovoLancamento(100m, categoriaId: moradia.Id),
            NovoLancamento(300m, categoriaId: aluguel.Id, status: StatusLancamento.Pendente),
            NovoLancamento(50m, categoriaId: cinema.Id),
            NovoLancamento(400m, categoriaId: mercado.Id),
            NovoLancamento(70m),
            NovoLancamento(30m, categoriaId: presentes.Id),
            NovoLancamento(90m, categoriaId: mercado.Id, compraParceladaId: Guid.NewGuid()),
            NovoLancamento(500m, tipo: TipoLancamento.Credit, categoriaId: mercado.Id),
            NovoLancamento(600m, oculto: true, categoriaId: mercado.Id),
            NovoLancamento(700m, transferenciaId: Guid.NewGuid(), categoriaId: mercado.Id),
            NovoLancamento(800m, status: StatusLancamento.Sugerido, categoriaId: mercado.Id),
            NovoLancamento(900m, data: OutroMes, categoriaId: mercado.Id)
        };
        var categorias = new[] { moradia, aluguel, lazer, cinema, mercado, presentes };

        var resultado = PlanejamentoMensalCalculator.Calcular(plano, lancamentos, categorias);

        var gastoPrimeiroNivel = resultado.Sublimites
            .Where(sublimite => sublimite.ParentId == null)
            .Sum(sublimite => sublimite.GastoRealizado);
        Assert.Equal(890m, gastoPrimeiroNivel);
        Assert.Equal(150m, resultado.GastoSemSublimite);
        Assert.Equal(totalVariavelDoMes, gastoPrimeiroNivel + resultado.GastoSemSublimite);
    }

    [Fact]
    public void Calcular_PlanoSemSublimitesSalvos_PreencheGastosPorCategoriaDeTodasCategoriasDespesa()
    {
        var alimentacao = NovaCategoria("Alimentacao");
        var bares = NovaCategoria("Bares", alimentacao);
        var lazer = NovaCategoria("Lazer");
        var salario = NovaCategoria("Salario", tipo: TipoCategoria.Receita);
        var plano = NovoPlano(5000m, 10m);
        var lancamentos = new[]
        {
            NovoLancamento(100m, categoriaId: alimentacao.Id),
            NovoLancamento(60m, categoriaId: bares.Id)
        };

        var resultado = PlanejamentoMensalCalculator.Calcular(
            plano,
            lancamentos,
            new[] { alimentacao, bares, lazer, salario });

        Assert.Empty(resultado.Sublimites);
        Assert.Equal(3, resultado.GastosPorCategoria.Count);
        Assert.Equal(160m, GastoDe(resultado.GastosPorCategoria, alimentacao.Id).GastoRealizado);
        Assert.Equal(60m, GastoDe(resultado.GastosPorCategoria, bares.Id).GastoRealizado);
        Assert.Equal(0m, GastoDe(resultado.GastosPorCategoria, lazer.Id).GastoRealizado);
        Assert.DoesNotContain(resultado.GastosPorCategoria, gasto => gasto.CategoriaId == salario.Id);
    }

    #endregion

    #region Helpers

    private static Lancamento NovoLancamento(
        decimal valor,
        TipoLancamento tipo = TipoLancamento.Debit,
        StatusLancamento status = StatusLancamento.Pago,
        DateOnly? data = null,
        bool oculto = false,
        Guid? categoriaId = null,
        Guid? contaFixaId = null,
        Guid? assinaturaCartaoId = null,
        Guid? transferenciaId = null,
        Guid? compraParceladaId = null) => new()
    {
        Id = Guid.NewGuid(),
        Tipo = tipo,
        Valor = valor,
        Status = status,
        Data = data ?? DentroDoMes,
        Oculto = oculto,
        CategoriaId = categoriaId,
        ContaFixaId = contaFixaId,
        AssinaturaCartaoId = assinaturaCartaoId,
        TransferenciaId = transferenciaId,
        CompraParceladaId = compraParceladaId
    };

    private static Categoria NovaCategoria(
        string nome,
        Categoria? pai = null,
        TipoCategoria tipo = TipoCategoria.Despesa,
        bool arquivada = false,
        Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Nome = nome,
        Tipo = tipo,
        ParentId = pai?.Id,
        Parent = pai,
        Arquivada = arquivada
    };

    private static PlanoMensal NovoPlano(
        decimal renda,
        decimal percentualInvestimento = 0m,
        params (Categoria Categoria, decimal Percentual)[] sublimites)
    {
        var plano = new PlanoMensal
        {
            Id = Guid.NewGuid(),
            Ano = Ano,
            Mes = Mes,
            RendaPrevista = renda,
            PercentualInvestimento = percentualInvestimento
        };

        foreach (var (categoria, percentual) in sublimites)
        {
            plano.Sublimites.Add(new PlanoMensalSublimite
            {
                Id = Guid.NewGuid(),
                PlanoMensalId = plano.Id,
                CategoriaId = categoria.Id,
                Percentual = percentual
            });
        }

        return plano;
    }

    private static SublimiteCalculado SublimiteDe(PlanoMensalCalculado resultado, string nome) =>
        resultado.Sublimites.Single(sublimite => sublimite.Nome == nome);

    private static GastoCategoriaCalculado GastoDe(
        IEnumerable<GastoCategoriaCalculado> gastos,
        Guid categoriaId) =>
        gastos.Single(gasto => gasto.CategoriaId == categoriaId);

    #endregion
}
