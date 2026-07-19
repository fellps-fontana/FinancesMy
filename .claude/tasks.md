# Tasks — Modulo de Investimentos (v1)

**NOTA (2026-07-15):** TASK-011 a TASK-024 abaixo implementaram um modelo de
Ativo por TICKER (compra/venda, preco medio, cotacao Brapi sob demanda) que
foi REMOVIDO e substituido por um Ativo standalone (sem ticker, sem API
externa) — decisao do usuario registrada em regra-de-negocio.md, secao
"Escopo: v1 vs v2". As entradas abaixo ficam como registro historico do que
foi executado (audit trail), nao refletem mais o codigo atual. Ver
`docs/investimentos.md` pro estado real do modulo.

Escopo confirmado (na epoca): investimento como CONTA MANUAL (tipo
INVESTIMENTO, origem MANUAL, saldo via `saldo_manual`). Sem ativos, ticker,
preco medio, cotacao ou rentabilidade — isso e v2 e esta fora daqui (ver
regra-de-negocio.md, secao "Escopo: v1 vs v2"). Essa premissa mudou depois
(ver nota acima).

Codebase e greenfield: nao ha EF Core, DbContext, entidades nem controllers
ainda. As primeiras tasks criam essa base.
---

## TASK-025 — Entidade `CompraParcelada` (Domain) + Configuration + DbSet

STATUS: CONCLUIDA (commit 823d439; FK como shadow property temporaria ate TASK-026)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: nenhuma
CONTEXTO A LER: `.claude/context/schema.dbml` tabela `compra_parcelada` (linha ~99-105); `.claude/context/regra-de-negocio.md` item 12, subsecao "Parcelamento", paragrafo "Agrupamento (so exibicao...)"; `.claude/context/stack.md` secao "Organizacao de pastas (Backend)" (Domain/ e Infrastructure/Configurations/)
ESCOPO: criar a entidade `CompraParcelada` com os 4 campos do schema (`Id`, `Descricao`, `ValorTotal`, `QuantidadeParcelas`, `DataCompra`) mais a colecao de navegacao `Lancamentos`, criar `CompraParceladaConfiguration` (mesmo padrao de `FaturaConfiguration`) e registrar `DbSet<CompraParcelada> ComprasParceladas` + `ApplyConfiguration` no `MyFinancesDbContext`.
CRITERIO DE ACEITE:
1. `CompraParcelada` nao tem `ContaId` (o schema.dbml nao lista esse campo na tabela — a conta e resolvida via cada Lancamento-parcela, todos da mesma conta).
2. `CompraParceladaConfiguration.ToTable("compra_parcelada")`, colunas em snake_case (`descricao`, `valor_total`, `quantidade_parcelas`, `data_compra`), `ValorTotal` com `HasPrecision(18, 2)` (mesmo padrao de `Lancamento.Valor`/`Transferencia.Valor`).
3. Projeto compila; `DbSet` visivel no `MyFinancesDbContext`.
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances\Domain\CompraParcelada.cs` (novo)
`MyFinances\MyFinances\Infrastructure\Configurations\CompraParceladaConfiguration.cs` (novo)
`MyFinances\MyFinances\Data\MyFinancesDbContext.cs`
NAO FAZER: nao adicionar `ContaId` na entidade (nao existe no schema). Nao criar migration ainda (TASK-027). Nao criar Repository ainda (TASK-028).
RETORNO ESPERADO: classe de entidade + configuration + DbSet registrado, sem logica de servico.

---

## TASK-026 — Extensao de `Lancamento` (Domain) com `CompraParceladaId`/`ParcelaNumero`

STATUS: CONCLUIDA (commit dc3edc8)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-025
CONTEXTO A LER: `.claude/context/schema.dbml` tabela `lancamento`, campos `compra_parcelada_id`/`parcela_numero` (linha ~47-48); `.claude/context/regra-de-negocio.md` item 12, paragrafo "Agrupamento"
ESCOPO: adicionar `Guid? CompraParceladaId`, `int? ParcelaNumero` e a propriedade de navegacao `CompraParcelada? CompraParcelada` em `Lancamento.cs`; estender `LancamentoConfiguration` com o mapeamento das 2 colunas (`compra_parcelada_id`, `parcela_numero`, ambas nullable) e o relacionamento `HasOne(l => l.CompraParcelada).WithMany(cp => cp.Lancamentos).HasForeignKey(l => l.CompraParceladaId).OnDelete(DeleteBehavior.SetNull)` — mesmo padrao de `OnDelete` ja usado para `Fatura`/`Transferencia` em `Lancamento` (FK opcional, nunca cascade-delete de historico financeiro).
CRITERIO DE ACEITE:
1. `Lancamento` compila com os 2 campos novos, ambos nullable (compra a vista continua com eles `null`).
2. `LancamentoConfiguration` mapeia as colunas com o nome exato do schema.dbml.
3. Relacionamento configurado com `SetNull`, nao `Cascade` nem `Restrict`.
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances\Domain\Lancamento.cs`
`MyFinances\MyFinances\Infrastructure\Configurations\LancamentoConfiguration.cs`
NAO FAZER: nao alterar nenhum outro campo/relacionamento existente de `Lancamento`. Nao gerar migration nesta task (TASK-027).
RETORNO ESPERADO: entidade + configuration atualizadas, compilando.

---

## TASK-027 — Migration: tabela `compra_parcelada` + colunas em `lancamento`

STATUS: CONCLUIDA (commit 0bb5fc3; build + 202 testes passando)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-025, TASK-026
CONTEXTO A LER: nenhum contexto de dominio novo — so confirmar que a migration gerada reflete exatamente o que TASK-025/026 configuraram
ESCOPO: gerar a migration via EF (`dotnet ef migrations add AddCompraParcelada`) a partir do estado de `MyFinancesDbContext` apos TASK-025/026. Verificado nesta sessao: NAO existe tabela `parcela` em nenhuma migration ja gerada — esta task e so ADD (`CreateTable compra_parcelada` + `AddColumn compra_parcelada_id`/`parcela_numero` em `lancamento`), sem `DropTable`.
CRITERIO DE ACEITE:
1. Migration gerada contem `CreateTable("compra_parcelada")` com as 4 colunas de dominio + PK, e `AddColumn` de `compra_parcelada_id`(uuid, nullable) e `parcela_numero`(int, nullable) em `lancamento`, com a FK correspondente.
2. Nenhum `DropTable("parcela")` presente (nao existe pra remover).
3. Projeto compila; migration aplica sem erro no harness de teste SQLite ja usado no projeto (ver `FaturaCicloIntegrationTests.cs` como referencia de fixture).
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances\Migrations\**` (gerado pelo EF, nunca editado a mao)
NAO FAZER: nao editar migration gerada manualmente (excecao so pra correcao documentada, nao e o caso aqui). Nao tentar remover `parcela` — nunca existiu no EF.
RETORNO ESPERADO: migration aplicavel; tabela `compra_parcelada` e colunas novas em `lancamento` criadas.

---

## TASK-028 — Repository de `CompraParcelada`

STATUS: CONCLUIDA (commit 5ee2c69)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-025
CONTEXTO A LER: `.claude/context/stack.md` secao "Organizacao de pastas (Backend)", item Repositories/; `MyFinances/MyFinances/Repositories/ILancamentoRepository.cs` e `LancamentoRepository.cs` como padrao a seguir
ESCOPO: criar `ICompraParceladaRepository`/`CompraParceladaRepository` com `Adicionar(CompraParcelada)`, `ObterPorId(Guid)` (com `Include(cp => cp.Lancamentos)`), `Salvar()`.
CRITERIO DE ACEITE:
1. Interface + implementacao seguindo exatamente o padrao de `LancamentoRepository` (injeta `MyFinancesDbContext`, sem logica de negocio).
2. `ObterPorId` retorna a compra parcelada com as parcelas carregadas.
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances\Repositories\ICompraParceladaRepository.cs` (novo)
`MyFinances\MyFinances\Repositories\CompraParceladaRepository.cs` (novo)
NAO FAZER: nao adicionar metodo de listagem geral/paginacao — nao usado nesta leva. Nao acessar `DbContext` fora do repository.
RETORNO ESPERADO: repository testavel isoladamente, sem regra de negocio.

---

## Esqueleto para TDD — `ParcelamentoCalculator` (Kira cria este arquivo ANTES de despachar TASK-029)

Regra critica = CALCULO (split de valor com resto de arredondamento). Segue o
mesmo padrao ja usado por `FaturaSaldoCalculator`: `static class` em `Domain/`,
funcao pura, sem I/O, testada isolada em `MyFinances.Tests/Services/`.

**Algoritmo obrigatorio (contrato — mike testa contra isto, levi implementa
exatamente isto, nao pode divergir entre as duas rodadas):** cada parcela,
exceto a ultima, recebe `Math.Floor(valorTotal / quantidadeParcelas * 100) / 100`
(truncado para baixo em 2 casas). A ultima parcela recebe
`valorTotal - soma das (quantidadeParcelas - 1) parcelas anteriores`. Isso
garante que a soma das N parcelas bate exatamente com `valorTotal` em
QUALQUER caso, sem depender de modo de arredondamento (banker's rounding
teria essa garantia quebrada em alguns casos).

Caminho do arquivo: `MyFinances\MyFinances\Domain\ParcelamentoCalculator.cs`

```csharp
namespace MyFinances.Domain;

// Calculo puro do split de valor de uma compra parcelada (item 12,
// regra-de-negocio.md). Sem I/O, sem estado — recebe valor_total e
// quantidade_parcelas, devolve o valor de cada parcela na ordem 1..N.
public static class ParcelamentoCalculator
{
    // Divide valorTotal em quantidadeParcelas partes. Cada parcela, exceto
    // a ultima, e truncada em 2 casas decimais; a ultima recebe o resto do
    // arredondamento, garantindo que a soma das partes bate exatamente com
    // valorTotal. Lanca ArgumentException se valorTotal <= 0 ou
    // quantidadeParcelas < 2.
    public static IReadOnlyList<decimal> CalcularValoresParcelas(decimal valorTotal, int quantidadeParcelas)
    {
        throw new NotImplementedException();
    }
}
```

---

## TASK-029 — [REGRA CRITICA] Testes RED: `ParcelamentoCalculator`

STATUS: CONCLUIDA (commit 61663fc; 10 testes, RED confirmado por NotImplementedException)
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-025 (arquivo de esqueleto criado pelo Kira antes desta task)
CONTEXTO A LER: `.claude/context/regra-de-negocio.md` item 12, paragrafo "Calculo do valor de cada parcela" (linha ~369-377); esqueleto de `ParcelamentoCalculator` (secao acima) — algoritmo obrigatorio, nao inventar outro
ESCOPO: escrever testes cobrindo: (a) R$100,00 em 3x -> [33.33, 33.33, 33.34]; (b) R$100,00 em 4x -> [25.00, 25.00, 25.00, 25.00] (sem resto); (c) R$10,00 em 3x -> [3.33, 3.33, 3.34]; (d) soma das N parcelas retornadas == valorTotal exatamente, para pelo menos 2 casos com resto; (e) `quantidadeParcelas` = 0 ou 1 lanca `ArgumentException`; (f) `valorTotal` <= 0 lanca `ArgumentException`. Rodar e confirmar RED (falha por `NotImplementedException`, nunca erro de compilacao).
CRITERIO DE ACEITE:
1. Todos os 6 casos acima cobertos em `[Fact]`/`[Theory]`.
2. Suite roda e falha por `NotImplementedException` — nao por erro de compilacao.
3. Nenhuma logica de implementacao escrita em `ParcelamentoCalculator.cs` (mike so testa, nao implementa).
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances.Tests\Services\ParcelamentoCalculatorTests.cs` (novo)
NAO FAZER: nao tocar `ParcelamentoCalculator.cs`. Nao inventar algoritmo de arredondamento diferente do especificado no esqueleto.
RETORNO ESPERADO: confirmacao de RED (mensagem de falha = `NotImplementedException`) + lista dos casos cobertos.

---

## TASK-030 — [REGRA CRITICA] Implementar `ParcelamentoCalculator` (GREEN)

STATUS: CONCLUIDA (commit 1ed88ca; 10/10 testes passando)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-029
CONTEXTO A LER: `.claude/context/regra-de-negocio.md` item 12, paragrafo "Calculo do valor de cada parcela"; `ParcelamentoCalculatorTests.cs` (leitura, NUNCA escrita) como especificacao do comportamento esperado
ESCOPO: implementar `ParcelamentoCalculator.CalcularValoresParcelas` seguindo exatamente o algoritmo do esqueleto (truncar em 2 casas para as N-1 primeiras parcelas, resto inteiro na ultima) ate os testes de TASK-029 ficarem GREEN.
CRITERIO DE ACEITE:
1. Todos os testes de `ParcelamentoCalculatorTests.cs` passam, sem alterar o arquivo de teste.
2. Soma das parcelas retornadas bate exatamente com `valorTotal` em todos os casos.
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances\Domain\ParcelamentoCalculator.cs`
NAO FAZER: nao editar `ParcelamentoCalculatorTests.cs`. Nao mudar a assinatura definida no esqueleto.
RETORNO ESPERADO: implementacao completa; testes GREEN.

---

## TASK-031 — [REGRA CRITICA] Confirmar GREEN: `ParcelamentoCalculator`

STATUS: CONCLUIDA (confirmacao independente: 10/10 + suite completa 212/212)
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-030
CONTEXTO A LER: nenhum novo — so rodar a suite de TASK-029
ESCOPO: rodar `ParcelamentoCalculatorTests.cs` contra a implementacao de TASK-030. NAO reescrever testes.
CRITERIO DE ACEITE: suite 100% GREEN. Se algum caso falhar, relatorio estruturado (arquivo+linha+caso) devolvido ao Kira para redespachar levi — mike nao corrige codigo.
ARQUIVOS PERMITIDOS: nenhum (so execucao)
NAO FAZER: nao alterar teste nem implementacao.
RETORNO ESPERADO: confirmacao GREEN ou relatorio de bug.

---

## TASK-032 — Style: revisar `ParcelamentoCalculator`

STATUS: CONCLUIDA (APROVADO na rodada 2, apos 1 correcao — commit cc3fb4b; magic number, Aggregate e calculo no loop corrigidos)
AGENT: style
FLUXO: Implementacao
DEPENDENCIAS: TASK-031
CONTEXTO A LER: `.claude/context/clean-code.md` inteiro; `.claude/context/regra-de-negocio.md` item 12, paragrafo "Calculo do valor de cada parcela"
ESCOPO: revisar `ParcelamentoCalculator.cs` contra clean-code.md e a regra de arredondamento — nome da funcao, ausencia de numero magico solto (2 casas decimais, se magic number, nomear), tratamento dos casos de borda.
CRITERIO DE ACEITE: veredito APROVADO ou lista de correcoes pontuais (nunca "melhorar" vago).
ARQUIVOS PERMITIDOS: nenhum (style nao edita)
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + (se houver) tarefa de correcao no formato desta secao, redespachada a levi, com nova rodada de TASK-031 apos a correcao.

---

## TASK-033 — DTOs: `CriarCompraParceladaRequest`, `CompraParceladaResponse`, extensao de `CompraResponse`

STATUS: CONCLUIDA (commit 2c58aba)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-025, TASK-026
CONTEXTO A LER: `.claude/context/stack.md` secao "Convencoes" (casing `DTOs/`) e "Organizacao de pastas (Backend)" item DTOs/; `MyFinances/MyFinances/DTOs/CriarCompraRequest.cs` e `CompraResponse.cs` como padrao
ESCOPO:
1. `CriarCompraParceladaRequest` (`Descricao` string obrigatorio, `ValorTotal` decimal obrigatorio, `QuantidadeParcelas` int obrigatorio, `CategoriaId` Guid? opcional, `DataCompra` DateOnly obrigatorio) — `ContaId` NAO entra no body, vem da rota, igual `CriarCompraRequest`.
2. Estender `CompraResponse` com `Guid? CompraParceladaId` e `int? ParcelaNumero` (null pra compra a vista), atualizando `FromLancamento` para preencher os dois a partir do `Lancamento`.
3. `CompraParceladaResponse` (`Id`, `ContaId`, `Descricao`, `ValorTotal`, `QuantidadeParcelas`, `DataCompra`, `Parcelas: List<CompraResponse>`), com `static CompraParceladaResponse FromDomain(CompraParcelada compraParcelada, Guid contaId)`. `ContaId` e parametro externo porque `CompraParcelada` (schema.dbml) nao guarda esse campo — a conta e a mesma em todas as parcelas.
CRITERIO DE ACEITE:
1. Os 3 tipos compilam, namespace `MyFinances.DTOs`.
2. `CompraResponse.FromLancamento` continua funcionando pra compra a vista (os 2 campos novos vem `null`).
3. `CompraParceladaResponse.FromDomain` monta `Parcelas` mapeando cada `Lancamento` da colecao via `CompraResponse.FromLancamento`.
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances\DTOs\CriarCompraParceladaRequest.cs` (novo)
`MyFinances\MyFinances\DTOs\CompraParceladaResponse.cs` (novo)
`MyFinances\MyFinances\DTOs\CompraResponse.cs`
NAO FAZER: nao expor a entity `CompraParcelada`/`Lancamento` direto. Nao incluir `ContaId` em `CriarCompraParceladaRequest`.
RETORNO ESPERADO: contrato de entrada/saida documentado (shape dos 3 tipos).

---

## TASK-034 — `ComprasParceladasService`: orquestracao da criacao

STATUS: CONCLUIDA (commit f73a15b; resolucao de fatura encadeada + persistencia so apos validar todas as parcelas)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-028, TASK-030, TASK-033
CONTEXTO A LER: `.claude/context/regra-de-negocio.md` item 12 inteiro (nao so a subsecao de parcelamento — precisa da regra geral de fatura/ciclo tambem); `MyFinances/MyFinances/Services/CompraCartaoService.cs`, `FaturaCicloService.cs`, `ValidacaoCartaoService.cs` (reaproveitar, nao duplicar)
ESCOPO: criar `ComprasParceladasService.CriarCompraParceladaAsync(Guid contaId, CriarCompraParceladaRequest request)`:
1. Reaproveitar `ValidacaoCartaoService.ValidarOperacaoCartaoAsync(contaId, request.Descricao, request.ValorTotal)` sem modifica-lo (ja cobre descricao vazia, valor<=0, conta inexistente/nao-cartao/inativa).
2. Validar `request.QuantidadeParcelas >= 2` (SUPOSICAO explicita: 1 parcela nao e "parcelada", deveria usar o endpoint de compra normal; regra-de-negocio.md nao define limite superior — nao inventar teto).
3. Chamar `ParcelamentoCalculator.CalcularValoresParcelas(request.ValorTotal, request.QuantidadeParcelas)` para obter os N valores.
4. Criar 1 `CompraParcelada` (metadado) e, para cada parcela `i` (1..N), resolver a FATURA (nao uma data solta) andando ciclo a ciclo — DECISAO CONFIRMADA COM O USUARIO EM 2026-07-12: parcela segue o ciclo da fatura do cartao, nao soma de meses corridos. Algoritmo:
   - Parcela 1: `faturaParcela1 = FaturaCicloService.ResolverFaturaParaLancamentoAsync(contaId, request.DataCompra)` — exatamente a mesma resolucao usada por compra a vista.
   - Parcela `i` (i > 1): `dataReferencia = faturaParcela(i-1).DataVencimento.AddDays(1)` (primeiro dia depois que a fatura da parcela anterior fecha, garantindo cair no proximo ciclo, nunca repetir o mesmo), depois `faturaParcelaI = FaturaCicloService.ResolverFaturaParaLancamentoAsync(contaId, dataReferencia)`.
   - Isso reaproveita 100% a logica de ciclo ja existente (dia_fechamento/dia_vencimento do cartao) sem duplicar nada — so encadeia N chamadas ao metodo que ja existe, uma por parcela.
   Criar 1 `Lancamento` por parcela com `Valor = valores[i-1]`, `FaturaId` da fatura resolvida naquele passo, `CompraParceladaId`, `ParcelaNumero = i`, mesmos campos fixos de `CompraCartaoService.CriarCompraAsync` (`Tipo=Debit`, `Status=Pago`, `Manual=true`, etc).
5. Persistir a `CompraParcelada` + os N `Lancamento` numa unica operacao logica (se qualquer fatura for rejeitada no meio do loop, nada e salvo — nao deixar estado parcial).
CRITERIO DE ACEITE:
1. `QuantidadeParcelas < 2` retorna erro sem persistir nada.
2. N `Lancamento` criados == `request.QuantidadeParcelas`, cada um com `ParcelaNumero` sequencial de 1 a N.
3. Soma de `Lancamento.Valor` das N parcelas == `request.ValorTotal` exatamente.
4. Cada `Lancamento` tem `FaturaId` da fatura do MES correspondente a sua propria data (nao todos na mesma fatura, exceto se cairem no mesmo ciclo por coincidencia de dia de fechamento).
5. Se `ResolverFaturaParaLancamentoAsync` rejeitar em qualquer parcela, nenhuma linha e persistida (transacional).
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances\Services\ComprasParceladasService.cs` (novo)
NAO FAZER: nao duplicar a logica de `FaturaCicloService`/`ValidacaoCartaoService` — so reusar (nao reimplementar resolucao de ciclo, so encadear chamadas). Nao implementar estorno nem edicao (fora de escopo). Nao editar `CompraCartaoService.cs` (servico separado, ver "Decisoes de modelagem" abaixo).
RETORNO ESPERADO: service testavel isoladamente, metodo nomeado por intencao, sem if solto de regra.

---

## TASK-035 — Testes de integracao: `ComprasParceladasService`

STATUS: CONCLUIDA (commit 583d8cd; 5 testes, todos GREEN)
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-034
CONTEXTO A LER: `.claude/context/regra-de-negocio.md` item 12 inteiro; `MyFinances/MyFinances.Tests/Services/FaturaCicloIntegrationTests.cs` como padrao de fixture (SQLite in-memory)
ESCOPO: testes de integracao cobrindo: (a) compra de R$100,00 em 3x gera exatamente 3 `Lancamento` com valores [33.33, 33.33, 33.34]; (b) cada `Lancamento` cai na `Fatura` do seu proprio mes de vencimento (nao todos na mesma fatura, quando os meses cruzam ciclos diferentes); (c) soma de `Lancamento.Valor` das N parcelas == `valor_total` exatamente; (d) `QuantidadeParcelas = 1` e rejeitado sem persistir nada; (e) todos os N `Lancamento` compartilham o mesmo `CompraParceladaId` e tem `ParcelaNumero` de 1 a N sem lacuna.
CRITERIO DE ACEITE: os 5 casos cobertos e passando; se falhar por bug de codigo (nao de teste), relatorio estruturado (arquivo+linha) devolvido ao Kira, nao corrigido pelo mike.
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances.Tests\Services\ComprasParceladasServiceIntegrationTests.cs` (novo)
NAO FAZER: nao alterar `ComprasParceladasService.cs` para fazer teste passar.
RETORNO ESPERADO: testes passando; relatorio de bug se houver.

---

## TASK-036 — Controller REST: criar compra parcelada

STATUS: CONCLUIDA (commit 3d11069)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-034
CONTEXTO A LER: `.claude/context/clean-code.md` secao "Organizacao (.NET)"; `MyFinances/MyFinances/Controllers/CartaoComprasController.cs` e `MyFinances/MyFinances/Controllers/AtivosController.cs` como padrao (sub-recurso de conta, controller dedicado)
ESCOPO: criar `CartaoComprasParceladasController` em `api/contas/{contaId}/compras-parceladas`, com `POST` recebendo `CriarCompraParceladaRequest`, chamando `ComprasParceladasService.CriarCompraParceladaAsync`, retornando `201 Created` com `CompraParceladaResponse.FromDomain(...)` em sucesso, `400 BadRequest` com `{ erro }` em falha de validacao — mesmo padrao de `CartaoComprasController.CriarCompra`.
CRITERIO DE ACEITE:
1. `POST /api/contas/{contaId}/compras-parceladas` com payload valido retorna 201 e o shape de `CompraParceladaResponse` com `Parcelas.Count == QuantidadeParcelas`.
2. Payload invalido (ex: `QuantidadeParcelas=1`, `ValorTotal<=0`, conta nao-cartao) retorna 400 com `{ erro }`.
3. Controller nao contem logica de negocio, so orquestra Service+DTO.
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances\Controllers\CartaoComprasParceladasController.cs` (novo)
NAO FAZER: nao criar endpoint de edicao/estorno (fora de escopo). Nao colocar validacao de negocio no controller.
RETORNO ESPERADO: contrato de API documentado (rota, verbo, body de entrada, shape de retorno, codigos de status).

---

## TASK-037 — Style: revisar Service + Controller + DTOs de compra parcelada

STATUS: CONCLUIDA (APROVADO na rodada 4, apos 3 correcoes — commits cc3fb4b/2513f73/9695221/52091aa; achado grave de transacionalidade quebrada + violacao de camada + codigo morto, todos corrigidos)
AGENT: style
FLUXO: Implementacao
DEPENDENCIAS: TASK-035, TASK-036
CONTEXTO A LER: `.claude/context/clean-code.md` inteiro; `.claude/context/regra-de-negocio.md` item 12 inteiro
ESCOPO: revisar `ComprasParceladasService.cs`, `CartaoComprasParceladasController.cs` e os 3 DTOs de TASK-033 contra clean-code.md e a regra de negocio — atencao especial a: reuso correto de `FaturaCicloService`/`ValidacaoCartaoService` (sem duplicacao), transacionalidade da criacao (nao deixa estado parcial), e a suposicao de formula de data por parcela (TASK-034, item 4) — se a suposicao estiver mal isolada (ex: espalhada em vez de nomeada), aponta.
CRITERIO DE ACEITE: veredito APROVADO ou lista de correcoes pontuais.
ARQUIVOS PERMITIDOS: nenhum (style nao edita)
NAO FAZER: nao editar codigo. Nao decidir os pontos fora de escopo (estorno/edicao) — se o codigo tentar cobri-los, aponta como extrapolacao de escopo, nao aprova.
RETORNO ESPERADO: veredito + (se houver) tarefa de correcao no formato desta secao, redespachada a levi.

---

## Decisoes de modelagem (Killua)

- **`ComprasParceladasService` novo, nao extensao de `CompraCartaoService`.**
  Tradeoff avaliado: estender `CompraCartaoService.CriarCompraAsync` pra
  aceitar N parcelas quebraria a assinatura (retorna 1 `Lancamento`, precisa
  retornar N + o agrupador) e misturaria dois fluxos com aggregate roots
  diferentes (1 Lancamento vs 1 CompraParcelada + N Lancamento) no mesmo
  metodo — exatamente o tipo de "funcao que faz duas coisas" que
  clean-code.md probe. Custo da separacao: mais uma classe de Service e um
  Controller a mais; ganho: cada Service continua com uma unica
  responsabilidade, e `CompraCartaoService`/`EditarCompraAsync` (compra a
  vista) fica intocado, sem risco de regressao. Mesmo padrao de sub-recurso
  ja usado por `AtivosController`/`AtivoService` dentro de `Conta`.
- **`CompraParcelada` nao guarda `ContaId`.** Fiel ao schema.dbml (a tabela
  so tem `descricao`/`valor_total`/`quantidade_parcelas`/`data_compra`) — a
  conta e resolvida via os `Lancamento` filhos, todos da mesma conta por
  construcao (uma compra parcelada nasce de uma unica chamada de API com um
  `contaId` de rota). Se precisar filtrar `CompraParcelada` por conta no
  futuro (ex: listagem), a query passa por `Lancamentos.ContaId`, nao por
  campo proprio.
- **`OnDelete(SetNull)` na FK `Lancamento.CompraParceladaId`**, mesmo padrao
  ja usado por `Fatura`/`Transferencia` em `Lancamento` — nenhuma FK
  financeira faz cascade-delete de historico no dominio. Como estorno/edicao
  de parcelada estao fora desta leva, essa FK na pratica nunca e exercitada
  em delete ainda — a escolha e so consistencia de padrao, nao urgencia.
- **Algoritmo de split: truncar em 2 casas para as N-1 primeiras parcelas,
  resto na ultima.** Alternativa descartada: `Math.Round` com banker's
  rounding em cada parcela e ajustar a ultima por diferenca — funciona, mas
  e mais dificil de auditar (o "porque" da ultima parcela ser diferente fica
  implicito no resultado de arredondamento, nao explicito no truncamento).
  Truncamento deixa a regra "resto vai pra ultima" auditavel por construcao,
  batendo com a redacao literal da regra-de-negocio.md.
- **Cada parcela resolve sua propria FATURA andando ciclo a ciclo, nao soma
  meses corridos na data — DECISAO CONFIRMADA COM O USUARIO EM 2026-07-12.**
  Alternativa descartada: `data_compra.AddMonths(i-1)` direto. Problema dela:
  desalinha do ciclo real do cartao perto da virada do fechamento (ex:
  comprar 2 dias antes do fechamento faria a parcela 1 "pular" um ciclo
  inteiro em relacao a uma compra a vista feita no mesmo dia). A solucao
  adotada encadeia `FaturaCicloService.ResolverFaturaParaLancamentoAsync` N
  vezes (parcela 1 pela data da compra; parcela `i>1` por um dia dentro do
  ciclo seguinte ao da parcela anterior — `DataVencimento.AddDays(1)`), sem
  nenhuma logica de ciclo nova. Ver algoritmo completo na TASK-034, item 4.

## Duvidas em aberto para o usuario

1. **Estorno de compra parcelada** — cancelar todas as parcelas futuras,
   so a proxima, ou nenhuma automaticamente (usuario estorna parcela por
   parcela manualmente, igual compra a vista)? Regra omissa. Fora desta leva.
2. **Edicao de compra parcelada existente** — mudar `quantidade_parcelas`
   depois de criada reabre o calculo de todas as parcelas futuras (as ja
   vinculadas a fatura PAGA ficam intocadas)? Regra omissa. Fora desta leva.
3. **Teto de `quantidade_parcelas`** — regra-de-negocio.md nao define
   limite superior. `ComprasParceladasService` (TASK-034) so valida
   `>= 2`, sem teto. Se o usuario quiser um limite (ex: 12x, 24x), e
   decisao de produto a confirmar antes de travar no codigo.

---

# Modulo Lancamento Geral (DEMANDA-001) — porte para arquitetura atual

Gerado por killua em 2026-07-19, worktree `lancamento-geral-porte`.

Este NAO e um modulo greenfield. A DEMANDA-001 ja foi implementada por
inteiro uma vez, numa branch nunca mergeada (`worktree-lancamento-geral-tasks`,
ainda em disco em `.claude/worktrees/lancamento-geral-tasks`), que divergiu do
main ANTES do rework do Cartao (commit 158bb57) e da unificacao de DbContext
(`Models/`+`AppDbContext` -> `Domain/`+`MyFinancesDbContext`).

Killua confirmou (via Glob) que parte da infraestrutura ja foi portada pelo
proprio rework do Cartao: `Domain/Lancamento.cs`, `Domain/Transferencia.cs`,
`LancamentoConfiguration`, `TransferenciaConfiguration`, `DbSet<Lancamento>`,
`DbSet<Transferencia>`, `ILancamentoRepository`/`LancamentoRepository`,
`ITransferenciaRepository`/`TransferenciaRepository` ja existem. Falta so
Service/Controller/DTO — nao existe ainda `LancamentoManualService`,
`TransferenciaService`, `FluxoCaixaService`, `LancamentosController` nem
`TransferenciasController`.

As tasks abaixo portam a logica ja validada (regra de sinal, transferencia,
exclusao, status) para a forma atual (Repository em vez de DbContext direto,
enum em vez de string constants, retorno em tupla `(bool, T?, string?)` igual
ao Cartao) — nao redesenham a regra do zero.

## Decisoes de modelagem (Killua)

- **Regra de sinal (item 2, CRITICA) preservada 1:1**, so muda de pasta:
  `Domain/ClassificacaoLancamentoService.Classificar(Lancamento)`, sempre
  `Tipo` + `TransferenciaId`/`FaturaId`, nunca `Valor`. Precedencia:
  Transferencia > CompetenciaCartao > Tipo.
- **Exclusao de lancamento manual = HARD DELETE**, bloqueada se
  `TransferenciaId`, `FaturaId` ou `ConciliadoCom` estiverem preenchidos.
- **Escrita manual (criacao/edicao) so aceita Status PENDENTE ou PAGO.**
  SUGERIDO e exclusivo da conciliacao automatica (fora de escopo v1).
- **Transferencia manual exige as duas contas com `Origem = MANUAL`.**
  Cria 2 `Lancamento` (Debit origem / Credit destino), `Status=Pago`,
  `Manual=true`, mesmo `TransferenciaId`, atomicamente — mesma forma que
  `PagamentoFaturaService` (Cartao) ja usa, ja testada em producao.
- **Rotas: split em vez de bifurcacao por querystring.** O branch antigo
  usava `GET /api/lancamentos?visao=caixa` (o proprio doc antigo chamava
  isso de "colisao de rota resolvida" — um workaround). Aqui:
  `LancamentosController` (`api/lancamentos`, GET, visao caixa cross-conta),
  `ContaLancamentosController` (`api/contas/{contaId}/lancamentos`, CRUD
  manual, mesmo padrao de `CartaoComprasController`), `TransferenciasController`
  (`api/transferencias`, POST).
- **DTOs flat em `DTOs/`**, sem sufixo `Dto` (`LancamentoResponse`, nao
  `LancamentoResponseDto`) — segue o precedente real de `CompraResponse`/
  `EstornoResponse`/`PagamentoResponse` ja flat na pasta.

## Corte de escopo (nao e ajuste de arquitetura, e decisao de regra)

`LancamentoOcultacaoService` (soft-delete de lancamento Open Finance,
`PATCH /ocultar`) NAO foi portado. `regra-de-negocio.md` item 4 marca esse
comportamento como **FORA DE ESCOPO v1** — decisao tomada depois que a
branch antiga foi escrita (que tinha isso pronto e testado). Regra vence
sobre codigo antigo. Ver "Pendencias" no fim desta secao — precisa
confirmacao do usuario.

---

## TASK-038 — Esqueleto ClassificacaoLancamentoService

STATUS: CONCLUIDA (arquivos criados por Kira)
AGENT: killua
FLUXO: Implementacao
DEPENDENCIAS: nenhuma
CONTEXTO A LER: regra-de-negocio.md item 2 (CRITICA)
ESCOPO: esqueleto compilavel do enum `ClassificacaoLancamento` e do metodo `Classificar` com `NotImplementedException`.
CRITERIO DE ACEITE:
1. Projeto compila com `NotImplementedException` no corpo.
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances\Domain\ClassificacaoLancamento.cs` (novo)
`MyFinances\MyFinances\Domain\ClassificacaoLancamentoService.cs` (novo)
NAO FAZER: nao implementar logica real no corpo do metodo.
RETORNO ESPERADO: esqueleto compilavel, sem logica.

---

## TASK-039 — [REGRA CRITICA] RED: testes de ClassificacaoLancamentoService

STATUS: PENDENTE
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-038
CONTEXTO A LER: regra-de-negocio.md item 2 (CRITICA) inteiro; branch antiga `D:\Estudos\MyFinances\.claude\worktrees\lancamento-geral-tasks\MyFinances\MyFinances.Tests\ClassificacaoLancamentoServiceTests.cs` (6 casos de referencia, adaptar de `TipoLancamentoConstants`/string para `TipoLancamento`/`StatusLancamento` enum e de `MyFinances.Models` para `MyFinances.Domain`)
ESCOPO: escrever testes cobrindo: Debit sem vinculo -> Saida; Credit sem vinculo -> Entrada; TransferenciaId preenchido com Debit -> Transferencia; TransferenciaId preenchido com Credit -> Transferencia (prova que TransferenciaId ignora Tipo); FaturaId preenchido -> CompetenciaCartao; Credit com Valor negativo -> Entrada (prova que Valor nunca e lido).
CRITERIO DE ACEITE:
1. 6 testes escritos, projeto compila, todos falham por `NotImplementedException` (nunca erro de compilacao).
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances.Tests\Services\ClassificacaoLancamentoServiceTests.cs` (novo)
NAO FAZER: nao alterar `ClassificacaoLancamentoService.cs`.
RETORNO ESPERADO: confirmacao de RED + lista dos 6 casos cobertos.

---

## TASK-040 — [REGRA CRITICA] GREEN: implementar Classificar

STATUS: PENDENTE
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-039
CONTEXTO A LER: regra-de-negocio.md item 2; `ClassificacaoLancamentoServiceTests.cs` (leitura, nunca escrita)
ESCOPO: implementar `Classificar` com precedencia Transferencia > CompetenciaCartao > Tipo, sem ler `Valor` em nenhum ponto.
CRITERIO DE ACEITE:
1. Implementacao pronta para rodar os testes de TASK-039.
2. Nenhuma leitura de `lancamento.Valor` no corpo do metodo.
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances\Domain\ClassificacaoLancamentoService.cs`
NAO FAZER: nao tocar no arquivo de teste.
RETORNO ESPERADO: implementacao completa.

---

## TASK-041 — [REGRA CRITICA] Confirmar GREEN: ClassificacaoLancamentoService

STATUS: PENDENTE
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-040
CONTEXTO A LER: nenhum novo — so rodar a suite de TASK-039
ESCOPO: rodar `ClassificacaoLancamentoServiceTests.cs` contra a implementacao de TASK-040. Nao reescrever testes.
CRITERIO DE ACEITE: 6/6 passando.
ARQUIVOS PERMITIDOS: nenhum (so execucao)
NAO FAZER: nao reescrever teste; se falhar por bug, reportar arquivo+linha, nao corrigir.
RETORNO ESPERADO: confirmacao GREEN ou relatorio de bug.

---

## TASK-042 — Style: revisao ClassificacaoLancamentoService

STATUS: PENDENTE
AGENT: style
FLUXO: Implementacao
DEPENDENCIAS: TASK-041
CONTEXTO A LER: regra-de-negocio.md item 2; clean-code.md
ESCOPO: validar que a precedencia esta correta e que nao ha leitura de `Valor` em nenhum caminho.
CRITERIO DE ACEITE: veredito (APROVADO ou tarefa de correcao no esquema padrao).
ARQUIVOS PERMITIDOS: nenhum (style nao edita)
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + (se houver) tarefa de correcao, redespachada a levi.

---

## TASK-043 — Extensao de ILancamentoRepository (Remover + fluxo caixa)

STATUS: PENDENTE
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: nenhuma
CONTEXTO A LER: stack.md secao "Organizacao de pastas (Backend)" (Repositories); branch antiga `Services/FluxoCaixaService.cs` (a query original, so como referencia da forma do filtro — nao copiar acesso a DbContext, so a logica: `FaturaId==null`, `TransferenciaId==null || Tipo==Debit`, `!Oculto`)
ESCOPO: adicionar `Task Remover(Lancamento lancamento)` e `Task<IEnumerable<Lancamento>> ListarParaFluxoCaixa(Guid? contaId)` em `ILancamentoRepository`/`LancamentoRepository`, seguindo exatamente o padrao ja usado por `IFaturaRepository.ObterFaturaAbertaPorConta` (metodo de repositorio nomeado por intencao de negocio).
CRITERIO DE ACEITE:
1. Projeto compila.
2. `ListarParaFluxoCaixa` exclui compras de cartao (`FaturaId != null`), lancamentos ocultos, e mantem so uma perna (Debit) de cada transferencia.
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances\Repositories\ILancamentoRepository.cs`
`MyFinances\MyFinances\Repositories\LancamentoRepository.cs`
NAO FAZER: nao adicionar logica de classificacao aqui (isso e Domain); nao remover metodos existentes.
RETORNO ESPERADO: 2 metodos novos, compilando.

---

## TASK-044 — LancamentoManualService + DTOs

STATUS: PENDENTE
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-043
CONTEXTO A LER: regra-de-negocio.md itens 1 (conta MANUAL e fonte da verdade) e 5 (status PENDENTE/PAGO na escrita manual, nunca SUGERIDO); branch antiga `Services/LancamentoManualService.cs` e `DTOs/CriarLancamentoRequest.cs`/`EditarLancamentoRequest.cs`/`LancamentoResponseDto.cs` (forma, adaptar de `AppDbContext`+string constants para `ILancamentoRepository`/`IContaRepository`+enum); `Services/CompraCartaoService.cs` (padrao de convencao atual: DI por Repository, retorno em tupla)
ESCOPO: criar `LancamentoManualService` com `CriarLancamentoAsync`, `EditarLancamentoAsync`, `ListarLancamentosAsync` (filtro opcional por status), `ExcluirLancamentoAsync` (hard delete, bloqueado se `TransferenciaId`/`FaturaId`/`ConciliadoCom` preenchido), todos validando `conta.Origem == OrigemConta.Manual`; validar `Tipo` (DEBIT/CREDIT), `Status` (PENDENTE/PAGO, nunca SUGERIDO) e `Valor > 0` na entrada.
CRITERIO DE ACEITE:
1. Excluir lancamento vinculado a transferencia/fatura/conciliacao retorna erro sem apagar.
2. Criar/editar em conta `origem=OPEN_FINANCE` retorna erro.
3. `Status=SUGERIDO` rejeitado na criacao/edicao.
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances\Services\LancamentoManualService.cs` (novo)
`MyFinances\MyFinances\DTOs\CriarLancamentoRequest.cs` (novo)
`MyFinances\MyFinances\DTOs\EditarLancamentoRequest.cs` (novo)
`MyFinances\MyFinances\DTOs\LancamentoResponse.cs` (novo)
`MyFinances\MyFinances\Program.cs`
NAO FAZER: nao acessar `MyFinancesDbContext` direto (so via Repository); nao aceitar `Status=SUGERIDO`; nao implementar ocultacao/soft-delete OF (item 4, fora de escopo v1); nao criar interface `ILancamentoManualService`.
RETORNO ESPERADO: service + DTOs, compilando, registrado em `Program.cs`.

---

## TASK-045 — TransferenciaService + DTOs

STATUS: PENDENTE
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: nenhuma
CONTEXTO A LER: regra-de-negocio.md item 3 (transferencias de mesma titularidade, branch manual) inteiro; branch antiga `Services/TransferenciaService.cs` e `DTOs/CriarTransferenciaRequest.cs` (forma, adaptar de `AppDbContext` para repositories e de string constants para enum); `Services/PagamentoFaturaService.cs` (mesma estrutura de 2 pernas — Debit origem/Credit destino, mesmo `TransferenciaId` — ja implementada e testada nesta arquitetura, usar como modelo direto); `DTOs/PagamentoResponse.cs` (padrao de DTO factory `FromTransferencia` a replicar)
ESCOPO: criar `TransferenciaService.CriarAsync` que valida `ContaOrigemId != ContaDestinoId`, ambas as contas `Origem == OrigemConta.Manual`, `Valor > 0`, e cria a `Transferencia` + 2 `Lancamento` (Debit origem/Credit destino, `Status=Pago`, `Manual=true`, mesmo `TransferenciaId`) atomicamente.
CRITERIO DE ACEITE:
1. Transferencia entre 2 contas manuais cria exatamente 2 lancamentos com mesmo `TransferenciaId`.
2. Transferencia envolvendo conta OF ou mesma conta origem/destino e rejeitada.
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances\Services\TransferenciaService.cs` (novo)
`MyFinances\MyFinances\DTOs\CriarTransferenciaRequest.cs` (novo)
`MyFinances\MyFinances\DTOs\TransferenciaResponse.cs` (novo)
`MyFinances\MyFinances\Program.cs`
NAO FAZER: nao permitir transferencia com conta `origem=OPEN_FINANCE`; nao expor a entity `Transferencia` crua no DTO (usar `TransferenciaResponse.FromTransferencia`, igual `PagamentoResponse.FromTransferencia`).
RETORNO ESPERADO: service + DTOs, compilando, registrado em `Program.cs`.

---

## TASK-046 — FluxoCaixaService + DTO

STATUS: PENDENTE
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-043
CONTEXTO A LER: regra-de-negocio.md item 12 paragrafo "Duas visoes" e item 3 ("aparece como uma unica linha logica"); branch antiga `Services/FluxoCaixaService.cs` (forma da query, adaptar para consumir `ILancamentoRepository.ListarParaFluxoCaixa` em vez de `AppDbContext` direto)
ESCOPO: criar `FluxoCaixaService.ObterLancamentosCaixaAsync(Guid? contaId)` que devolve a visao CAIXA: compras de cartao fora, lancamentos ocultos fora, cada transferencia aparecendo como uma unica linha (perna Debit).
CRITERIO DE ACEITE:
1. Pagamento de fatura (transferencia conta corrente->cartao) aparece 1 vez.
2. Compra de cartao nao aparece.
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances\Services\FluxoCaixaService.cs` (novo)
`MyFinances\MyFinances\DTOs\LancamentoCaixaResponse.cs` (novo)
`MyFinances\MyFinances\Program.cs`
NAO FAZER: nao listar compras de cartao (`FaturaId != null`) nesta visao; nao duplicar as 2 pernas de uma transferencia na resposta.
RETORNO ESPERADO: service + DTO, compilando, registrado em `Program.cs`.

---

## TASK-047 — Controllers (Lancamentos, ContaLancamentos, Transferencias)

STATUS: PENDENTE
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-044, TASK-045, TASK-046
CONTEXTO A LER: clean-code.md "Organizacao (.NET)" (controller so orquestra); `Controllers/CartaoComprasController.cs` e `Controllers/FaturasController.cs` (padrao de rota atual: `api/contas/{contaId}/...` para escopo de conta)
ESCOPO: criar `LancamentosController` (`GET api/lancamentos?contaId=`, visao caixa via `FluxoCaixaService`), `ContaLancamentosController` (`api/contas/{contaId}/lancamentos`: `GET` com filtro `status`, `POST`, `PUT/{id}`, `DELETE/{id}`, via `LancamentoManualService`), `TransferenciasController` (`POST api/transferencias`, via `TransferenciaService`).
CRITERIO DE ACEITE:
1. Os 3 controllers compilam.
2. Cada endpoint chama exatamente 1 Service.
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances\Controllers\LancamentosController.cs` (novo)
`MyFinances\MyFinances\Controllers\ContaLancamentosController.cs` (novo)
`MyFinances\MyFinances\Controllers\TransferenciasController.cs` (novo)
NAO FAZER: nao colocar validacao de regra de negocio no controller; nao reintroduzir `?visao=caixa`; nao devolver entity crua.
RETORNO ESPERADO: contrato de API documentado (rota, verbo, shape de retorno, codigos de status).

---

## TASK-048 — Testes de Service (LancamentoManual, Transferencia, FluxoCaixa)

STATUS: PENDENTE
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-044, TASK-045, TASK-046
CONTEXTO A LER: regra-de-negocio.md itens 1, 3, 5; branch antiga `LancamentoManualServiceTests.cs` e `TransferenciaServiceTests.cs` (so os nomes/casos, adaptar setup de `AppDbContext` para `MyFinancesDbContext` in-memory)
ESCOPO: testar CRUD manual (feliz + rejeicoes: conta OF, status SUGERIDO, valor<=0, exclusao bloqueada por vinculo), transferencia (feliz + rejeicoes: mesma conta, conta OF, valor<=0), fluxo caixa (exclui compra cartao, exclui oculto, transferencia como 1 linha).
CRITERIO DE ACEITE: testes passando cobrindo os casos listados.
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances.Tests\Services\LancamentoManualServiceTests.cs` (novo)
`MyFinances\MyFinances.Tests\Services\TransferenciaServiceTests.cs` (novo)
`MyFinances\MyFinances.Tests\Services\FluxoCaixaServiceTests.cs` (novo)
NAO FAZER: nao alterar os Services para o teste passar; bug de codigo volta relatorio estruturado.
RETORNO ESPERADO: testes passando; relatorio de bug se houver.

---

## TASK-049 — Testes HTTP dos controllers

STATUS: PENDENTE
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-047
CONTEXTO A LER: `MyFinances.Tests/Controllers/ContasControllerTests.cs` (padrao WebApplicationFactory + InMemory DB + JWT ja usado no projeto)
ESCOPO: testes HTTP dos 3 endpoints novos: fluxo caixa cross-conta, CRUD de lancamento manual, criacao de transferencia.
CRITERIO DE ACEITE: testes passando; status HTTP corretos (400 nas rejeicoes, 201/200/204 nos casos felizes).
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances.Tests\Controllers\LancamentosControllerTests.cs` (novo)
`MyFinances\MyFinances.Tests\Controllers\TransferenciasControllerTests.cs` (novo)
NAO FAZER: nao alterar controller/service pra passar teste sem reportar.
RETORNO ESPERADO: testes passando.

---

## TASK-050 — Style: revisao geral do modulo

STATUS: PENDENTE
AGENT: style
FLUXO: Implementacao
DEPENDENCIAS: TASK-049
CONTEXTO A LER: regra-de-negocio.md itens 1, 2, 3, 5, 12; clean-code.md inteiro
ESCOPO: revisar todo o modulo portado contra regra de negocio e clean-code, com atencao especial a: nenhum service le `DbContext` direto, nenhuma leitura de `Valor` fora do calculo explicito ja revisado em TASK-042, nenhum endpoint expõe entity crua.
CRITERIO DE ACEITE: veredito final (APROVADO ou tarefa de correcao no esquema padrao, redespachada ao levi).
ARQUIVOS PERMITIDOS: nenhum (style nao edita)
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito final do modulo.

---

## Mapa de dependencia (TASK-038 a TASK-050)

```
038 (esqueleto) -> 039 (RED) -> 040 (GREEN) -> 041 (confirma GREEN) -> 042 (style)
043 (repo)  ─┬─> 044 (LancamentoManualService) ─┐
             └─> 046 (FluxoCaixaService)        ├─> 047 (controllers) -> 049 (testes HTTP) -> 050 (style geral)
045 (TransferenciaService, sem dependencia) ────┘
044, 045, 046 -> 048 (testes de service)
```
043 e 045 nao dependem de 038-042 (a cadeia critica de Classificacao e
independente do resto — pode rodar em paralelo).

## Pendencias — precisa decisao do usuario antes de rodar a queue

1. **Ocultacao de lancamento Open Finance (item 4) ficou fora desta entrega.**
   `regra-de-negocio.md` marca isso como fora de escopo v1 — decisao tomada
   depois que a branch antiga (que ja tinha isso pronto/testado) foi escrita.
   Confirmar que e isso mesmo, ou reincluir.
2. **Gap pre-existente, nao introduzido aqui:** nem a branch antiga nem este
   desenho validam `conta.Ativa` antes de escrever lancamento/transferencia
   manual — o Cartao (`ValidacaoCartaoService`) rejeita conta inativa, mas
   `regra-de-negocio.md` nao exige isso pra lancamento/transferencia manual.
   Regra omissa — nao assumida. Decidir se pergunta formalmente antes de
   codar ou se fica como esta.
