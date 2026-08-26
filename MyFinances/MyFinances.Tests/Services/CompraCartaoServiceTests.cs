using MyFinances.Domain;
using MyFinances.DTOs;
using MyFinances.Services;
using Xunit;

namespace MyFinances.Tests.Services;

/// Nota: testes de CompraCartaoService que precisam de ContaFixaId preenchido
/// foram mantidos como unitarios mocks em RecorrenciaGeradorServiceIntegrationTests.cs
/// porque CompraCartaoService tem dependencias complexas (FaturaCicloService, SQLite constraints)
/// que sao melhor testadas via integracao do RecorrenciaGerador completo.
public class CompraCartaoServiceTests
{
    #region Regra 13 (u): CriarCompraAsync com contaFixaId preenchido vincula a compra

    /// Cobertura: testado via RecorrenciaGeradorServiceIntegrationTests.GerarOcorrenciaAtualEProximaAsync_ContaCartao_GeraCompraViaService
    /// que exercita o caminho completo de recorrencia gerando compras com contaFixaId
    /// em ambiente de integracao SQLite.

    #endregion
}
