# Tasks — Modulo de Parcelamento de Compra no Cartao (v1)

Escopo confirmado: compra parcelada gera N Lancamentos (um por parcela, cada
um com `fatura_id` proprio resolvido pelo ciclo do MES DE VENCIMENTO da
parcela), agrupados so para exibicao por `compra_parcelada`. Divisao
automatica `valor_total / quantidade_parcelas`, resto do arredondamento na
ULTIMA parcela. Ver regra-de-negocio.md, item 12, subsecao "Parcelamento
(compra parcelada) — decisao registrada em 2026-07-12".

Verificado nesta sessao (nao assumido): `Lancamento.cs` ainda NAO tem
`CompraParceladaId`/`ParcelaNumero` — extensao obrigatoria. A unica migration
existente (`20260711225259_InitialCreate.cs`) NAO tem tabela `parcela` —
nunca foi criada via EF, entao a tarefa de migration e so ADD (tabela nova +
2 colunas), sem DROP TABLE.

Fora de escopo desta leva (regra omissa — nao decidido, nao implementado):
- **Estorno de compra parcelada** (cancelar parcelas futuras vs ja pagas).
  Se qualquer task abaixo esbarrar nisso, a implementacao NAO cobre estorno —
  so a criacao da compra parcelada.
- **Edicao de compra parcelada existente** (mudar quantidade de parcelas
  depois de criada). Mesma coisa: fora de escopo, nao implementar.
- **Front (hanzo)**: nao entra nesta leva. Formulario de criacao + exibicao
  agrupada "3/10" na lista de compras do cartao e volume suficiente pra
  rodada propria, depois do back estar fechado e revisado. Kira decide
  quando abrir.

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

STATUS: PENDENTE
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
