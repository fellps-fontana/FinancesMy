# Tasks — Modulo de Investimentos (v1)

Escopo confirmado: investimento como CONTA MANUAL (tipo INVESTIMENTO, origem
MANUAL, saldo via `saldo_manual`). Sem ativos, ticker, preco medio, cotacao ou
rentabilidade — isso e v2 e esta fora daqui (ver regra-de-negocio.md, secao
"Escopo: v1 vs v2").

Codebase e greenfield: nao ha EF Core, DbContext, entidades nem controllers
ainda. As primeiras tasks criam essa base.

---

## TASK-001 — Setup EF Core + Npgsql + DbContext

STATUS: CONCLUIDA
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: nenhuma
CONTEXTO A LER: stack.md secao "Stack" e "Convencoes"
ESCOPO: adicionar pacotes EF Core + Npgsql ao csproj, criar `MyFinancesDbContext` vazio e configurar a connection string via appsettings/variavel de ambiente.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/MyFinances.csproj`, `MyFinances/MyFinances/Program.cs`, `MyFinances/MyFinances/appsettings.json`, `MyFinances/MyFinances/appsettings.Development.json`, `MyFinances/MyFinances/Data/MyFinancesDbContext.cs` (novo)
NAO FAZER: nao versionar credencial real de banco em `appsettings.json` (so em `appsettings.Development.json`, local); nao criar nenhum DbSet ainda (fica pra TASK-002); nao remover o endpoint `weatherforecast` (isso e limpeza, nao faz parte desta task).
RETORNO ESPERADO: projeto compila, `DbContext` registrado via DI, connection string configuravel.

---

## TASK-002 — Entidade Conta + migration inicial

STATUS: CONCLUIDA
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-001
CONTEXTO A LER: schema.dbml tabela `conta`; regra-de-negocio.md secao 1 (origem OPEN_FINANCE/MANUAL) e secao 10 (saldo de conta)
ESCOPO: criar a entidade `Conta` refletindo TODOS os campos da tabela `conta` do schema.dbml (nao so os de investimento) e gerar a migration inicial.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Models/Conta.cs` (novo), `MyFinances/MyFinances/Data/MyFinancesDbContext.cs`, `MyFinances/MyFinances/Migrations/**` (gerado pelo EF)
NAO FAZER: nao criar entidade Lancamento, Transferencia, Fatura ou qualquer outra tabela do schema — so `conta`, que e a unica necessaria para este modulo. Nao implementar logica de servico aqui, so o modelo de dados.
RETORNO ESPERADO: migration aplicavel, tabela `conta` criada no Postgres com os campos e tipos do schema.dbml.

---

## TASK-003 — Repository + Service de Conta (investimento)

STATUS: CONCLUIDA (aprovada pelo style apos 1 rodada de correcao)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-002
CONTEXTO A LER: regra-de-negocio.md secao 8 (cofrinho/investimentos/acoes) e secao 10 (saldo de conta manual)
ESCOPO: criar `ContaRepository` (acesso a dados via EF Core) e `ContaService` com metodos criar, listar (filtro por `tipo=INVESTIMENTO`), atualizar `saldo_manual` e desativar (`ativa=false`), validando que `tipo=INVESTIMENTO` sempre exige `origem=MANUAL`.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Repositories/ContaRepository.cs` (novo), `MyFinances/MyFinances/Services/ContaService.cs` (novo)
NAO FAZER: nao implementar logica de conta BANCO/CARTAO (sync Pierre, saldo calculado por lancamento) — so o caminho MANUAL/INVESTIMENTO. Nao expor entity diretamente para fora da camada (isso e responsabilidade do controller/DTO na TASK-004). Nao implementar hard-delete — desativar e sempre soft (`ativa=false`), igual ao padrao de soft-delete ja usado no restante do dominio (item 4).
RETORNO ESPERADO: `ContaService` testavel isoladamente (sem depender do controller), com metodos nomeados por intencao (nao if solto).

---

## TASK-004 — Controller REST de Conta (investimento)

STATUS: CONCLUIDA (aprovada pelo style apos 1 rodada de correcao)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-003
CONTEXTO A LER: clean-code.md secao "Organizacao (.NET)" (DTOs, camadas); regra-de-negocio.md secao 8
ESCOPO: criar `ContasController` com endpoints `POST /contas`, `GET /contas?tipo=INVESTIMENTO`, `PATCH /contas/{id}/saldo`, `PATCH /contas/{id}/desativar`, usando DTOs de entrada/saida (nunca a entity).
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Controllers/ContasController.cs` (novo), `MyFinances/MyFinances/Dtos/Conta/*.cs` (novo)
NAO FAZER: nao expor `dia_fechamento`/`dia_vencimento` no DTO (campos exclusivos de CARTAO, fora de escopo). Nao colocar regra de negocio no controller — so orquestra a chamada ao Service.
RETORNO ESPERADO: contrato de API documentado (rota, verbo, body de entrada, shape de retorno) para os 4 endpoints.

---

## TASK-005 — Testes: CRUD de conta investimento e regra de saldo manual

STATUS: CONCLUIDA (14 testes de integracao HTTP no Controller; regra de negocio ja coberta na TASK-003 com 17 testes no Service)
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-004
CONTEXTO A LER: regra-de-negocio.md secao 10 (saldo de conta manual); secao 8 (cada conta e separada, sem classificacao por nome)
ESCOPO: escrever e rodar testes cobrindo: saldo da conta INVESTIMENTO e sempre `saldo_manual` (nunca calculado), criacao de multiplas contas de investimento distintas (cofrinho/XP/carteira) sem colisao, validacao que rejeita `tipo=INVESTIMENTO` com `origem=OPEN_FINANCE`, e que "desativar" so seta `ativa=false` (nao apaga linha).
ARQUIVOS PERMITIDOS: pasta de testes definida em stack.md (criar se nao existir, ex: `MyFinances/MyFinances.Tests/`)
NAO FAZER: nao alterar `ContaService`/`ContasController` para fazer o teste passar sem reportar — bug de codigo volta pro levi.
RETORNO ESPERADO: testes passando; se falhar por bug de codigo (nao de teste), relatorio estruturado apontando arquivo e linha.

---

## TASK-006 — Calculo do total investido

STATUS: CONCLUIDA (aprovada pelo style apos 1 rodada de correcao)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-003
CONTEXTO A LER: regra-de-negocio.md secao "Escopo: v1 vs v2" (racional "ver o total investido no patrimonio"); secao 10
ESCOPO: criar funcao nomeada e testavel no `ContaService` que soma `saldo_manual` de todas as contas ativas com `tipo=INVESTIMENTO`, e expor via `GET /contas/investimentos/total` no controller.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Services/ContaService.cs`, `MyFinances/MyFinances/Controllers/ContasController.cs`
NAO FAZER: NAO tentar calcular "patrimonio total" do app (isso somaria tambem contas BANCO/CARTAO via Open Finance, cuja logica de saldo calculado por lancamento ainda nao existe — ver duvida em aberto no fim do arquivo). Escopo aqui e so o total das contas de investimento.
RETORNO ESPERADO: endpoint retornando `{ totalInvestido: decimal }`, funcao de calculo isolada e nomeada (ex: `CalcularTotalInvestido`).

---

## TASK-007 — Teste do calculo de total investido

STATUS: CONCLUIDA (8 testes novos, 39 no total, todos passando)
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-006
CONTEXTO A LER: regra-de-negocio.md secao 10 e secao "Escopo: v1 vs v2"
ESCOPO: testar que o total soma somente contas `tipo=INVESTIMENTO` E `ativa=true`, ignora contas desativadas e contas de outros tipos, e retorna zero sem contas cadastradas.
ARQUIVOS PERMITIDOS: pasta de testes (mesma da TASK-005)
NAO FAZER: nao testar patrimonio total do app — fora de escopo aqui.
RETORNO ESPERADO: testes passando cobrindo os 3 casos citados; relatorio de bug se falhar por codigo.

---

## TASK-008 — Camada de dados no front (hook de contas de investimento)

STATUS: CONCLUIDA (aprovada pelo style; ja inclui os hooks de mutation criar/atualizar-saldo/desativar, entao TASK-010 so consome, nao recria)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-004, TASK-006
CONTEXTO A LER: stack.md secao "Frontend (React)"; clean-code.md secao "Organizacao (React)"
ESCOPO: criar hook/data layer (`useContasInvestimento`) que consome `GET /contas?tipo=INVESTIMENTO` e `GET /contas/investimentos/total`, sem fetch solto em componente.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/investimentos/**` (projeto real trazido da main, nao `frontend/` que foi descartado)
NAO FAZER: nao colocar chamada de fetch dentro de componente de UI (isso e TASK-009/010).
RETORNO ESPERADO: hook tipado (sem `any`) retornando lista de contas e total investido, com estado de loading/erro.

---

## TASK-009 — UI: lista de contas de investimento + total

STATUS: CONCLUIDA (aprovada pelo style apos 1 rodada de correcao)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-008
CONTEXTO A LER: identidade-visual.md (se existir); regra-de-negocio.md secao 8
ESCOPO: tela listando as contas de investimento (nome, saldo atual) com o total investido em destaque.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/investimentos/*.tsx` (novo; use os hooks ja existentes em `hooks/`, nao recrie)
NAO FAZER: nao exibir campos de CARTAO (dia_fechamento/vencimento) nem qualquer dado de ativo/ticker (v2).
RETORNO ESPERADO: componente de apresentacao consumindo o hook da TASK-008, sem logica de calculo embutida.

---

## TASK-010 — UI: criar/editar/desativar conta de investimento

STATUS: CONCLUIDA (aprovada pelo style de primeira)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-008
CONTEXTO A LER: regra-de-negocio.md secao 8 e secao 10
ESCOPO: formulario para criar conta de investimento (nome + saldo inicial), editar `saldo_manual` e acao de desativar.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/investimentos/*.tsx` (novo; use os hooks de mutation ja existentes em `hooks/` — useCriarContaInvestimento, useAtualizarSaldoConta, useDesativarConta — nao recrie)
NAO FAZER: nao permitir escolher `origem` no form (sempre MANUAL, implicito); nao expor campo de tipo de ativo/ticker (v2).
RETORNO ESPERADO: componente de formulario chamando o hook da TASK-008 para POST/PATCH.

---

## TASK-011 — Entidades Ativo e MovimentacaoAtivo + migration

STATUS: CONCLUIDA
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-002
CONTEXTO A LER: schema.dbml tabelas `ativo` e `movimentacao_ativo`; regra-de-negocio.md secao 8 (intro) e 8.1-8.3
ESCOPO: criar as entidades `Ativo` e `MovimentacaoAtivo` (e enum `TipoMovimentacaoAtivo`: Compra|Venda, seguindo o mesmo padrao de conversao ja usado em `OrigemConta`/`TipoConta`) refletindo todos os campos do schema.dbml, registrar `DbSet<Ativo>` e `DbSet<MovimentacaoAtivo>` no `MyFinancesDbContext`, e gerar a migration.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Models/Ativo.cs` (novo), `MyFinances/MyFinances/Models/MovimentacaoAtivo.cs` (novo), `MyFinances/MyFinances/Models/TipoMovimentacaoAtivo.cs` (novo), `MyFinances/MyFinances/Data/MyFinancesDbContext.cs`, `MyFinances/MyFinances/Migrations/**`
NAO FAZER: nao criar Repository/Service ainda (TASK-012); nao adicionar FK/CHECK de banco para validar `conta.tipo=INVESTIMENTO` — essa validacao e do Service, nao do schema; nao mexer na tabela `conta`.
RETORNO ESPERADO: migration aplicavel; tabelas `ativo` e `movimentacao_ativo` criadas no Postgres com campos e tipos do schema.dbml.

---

## TASK-012 — Repository de Ativo

STATUS: CONCLUIDA
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-011
CONTEXTO A LER: schema.dbml tabelas `ativo`/`movimentacao_ativo`; clean-code.md secao "Organizacao (.NET)"
ESCOPO: criar `IAtivoRepository`/`AtivoRepository` com metodos: `Adicionar(Ativo)`, `AdicionarMovimentacao(MovimentacaoAtivo)`, `ObterPorId(Guid)`, `ListarPorConta(Guid contaId)` (todos, incluindo historico inativo), `ListarAtivosAtivosPorConta(Guid contaId)` (so `ativa=true`), `ObterAtivoAtivoPorTicker(Guid contaId, string ticker)` (busca ativo `ativa=true` com o mesmo ticker na conta — usado para decidir incrementar vs criar novo), `SomarValorAtivosPorConta(IEnumerable<Guid> contaIds)` retornando `Dictionary<Guid, decimal>` com soma de `quantidade x preco_atual` dos ativos `ativa=true` de cada conta (consumido pelo `ContaService` na TASK-017), `Salvar()`.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Repositories/IAtivoRepository.cs` (novo), `MyFinances/MyFinances/Repositories/AtivoRepository.cs` (novo)
NAO FAZER: nao implementar recalculo de preco medio aqui — isso e regra de Service (TASK-013). Nao expor a entity fora da camada de dados.
RETORNO ESPERADO: repository testavel, metodos nomeados por intencao, sem if solto de regra de negocio.

---

## TASK-013 — AtivoService: compra e venda (regra critica)

STATUS: CONCLUIDA (aprovada pelo style apos 1 rodada de correcao)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-012
CONTEXTO A LER: regra-de-negocio.md secao 8, 8.1, 8.2, 8.3 INTEIRAS (nao pule nenhuma — recalculo de preco medio e o ponto mais sensivel de todo o modulo)
ESCOPO: criar `IAtivoService`/`AtivoService` com:
- `RegistrarCompra(Guid contaId, string ticker, decimal quantidade, decimal precoUnitario, DateOnly data, string? nome)`: valida que a conta existe e `tipo=INVESTIMENTO` (senao `ContaNaoEhInvestimentoException`); busca ativo `ativa=true` com o mesmo ticker na conta; se nao existir, cria Ativo novo (`quantidade=quantidade`, `preco_medio=precoUnitario`, `preco_atual=precoUnitario`); se existir, recalcula `preco_medio` pela formula do item 8.2 usando quantidade/preco_medio ATUAIS como peso, soma a quantidade, e atualiza `preco_atual=precoUnitario` (item 8.1 — o preco da compra vale pra toda a posicao, nao so a leva nova); grava `MovimentacaoAtivo` tipo Compra.
- `RegistrarVenda(Guid contaId, Guid ativoId, decimal quantidade, decimal precoUnitario, DateOnly data, string? observacao)`: valida que o ativo existe, pertence a conta e esta `ativa=true`; rejeita se `quantidade > ativo.Quantidade` (`QuantidadeVendaInvalidaException`, sem alterar nada); reduz `quantidade`; se chegar a zero, `ativa=false`; `preco_medio` NUNCA muda; grava `MovimentacaoAtivo` tipo Venda; NAO gera lancamento nem transferencia em nenhuma outra conta.
- `ListarAtivosPorConta(Guid contaId)`: valida que a conta existe, retorna ativos `ativa=true`.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Services/IAtivoService.cs` (novo), `MyFinances/MyFinances/Services/AtivoService.cs` (novo), `MyFinances/MyFinances/Exceptions/ContaNaoEhInvestimentoException.cs` (novo), `MyFinances/MyFinances/Exceptions/AtivoNaoEncontradoException.cs` (novo), `MyFinances/MyFinances/Exceptions/QuantidadeVendaInvalidaException.cs` (novo)
NAO FAZER: nao implementar endpoint de "marcar a mercado" sem compra (pendencia em aberto na regra, fora de v1). Nao gerar lancamento em outra conta na venda (item 8.3, escopo minimo explicito). Nao reaproveitar Ativo desativado numa compra do mesmo ticker — SEMPRE cria Ativo novo.
RETORNO ESPERADO: `AtivoService` testavel isoladamente; formula de preco medio isolada em metodo nomeado (ex: `CalcularPrecoMedio`), nunca if solto.

---

## TASK-014 — Testes: compra, venda e recalculo de preco medio

STATUS: CONCLUIDA (19 testes novos, 123 no total, todos passando)
AGENT: mike
FLUXO: Implementacao (automatico apos regra critica)
DEPENDENCIAS: TASK-013
CONTEXTO A LER: regra-de-negocio.md secao 8.1, 8.2, 8.3 inteiras
ESCOPO: testar (a) compra de ticker novo cria Ativo com `preco_medio=preco_atual=preco da compra`; (b) compra de ticker existente incrementa `quantidade` e recalcula `preco_medio` pela formula ponderada, atualizando `preco_atual`; (c) venda parcial reduz `quantidade` e NAO altera `preco_medio`; (d) venda total zera `quantidade` e seta `ativa=false`; (e) venda de quantidade maior que a posicao e rejeitada, sem alterar estado; (f) compra do mesmo ticker apos venda total cria Ativo NOVO (o antigo continua `ativa=false`, nao e reaproveitado); (g) venda nao gera nenhum lancamento/transferencia em outra conta.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances.Tests/Services/AtivoServiceTests.cs` (novo)
NAO FAZER: nao alterar `AtivoService` pra fazer teste passar sem reportar — bug de codigo volta pro levi.
RETORNO ESPERADO: testes passando cobrindo os 7 casos; relatorio estruturado (arquivo+linha) se achar bug de codigo.

---

## TASK-015 — Controller REST de Ativo

STATUS: CONCLUIDA (aprovada pelo style apos 2 rodadas - achou bug real: venda ignorava contaId da rota)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-013
CONTEXTO A LER: clean-code.md "Organizacao (.NET)"; regra-de-negocio.md secao 8
ESCOPO: criar `AtivosController` com `GET /api/contas/{contaId}/ativos` (lista ativos `ativa=true`), `POST /api/contas/{contaId}/ativos/compras` (compra — cria ou incrementa), `POST /api/contas/{contaId}/ativos/{ativoId}/vendas` (venda). DTOs de entrada/saida, nunca a entity. Traducao de excecoes: `ContaNaoEncontradaException`->404, `ContaNaoEhInvestimentoException`->422, `AtivoNaoEncontradoException`->404, `QuantidadeVendaInvalidaException`->422.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Controllers/AtivosController.cs` (novo), `MyFinances/MyFinances/DTOs/Ativo/*.cs` (novo: `RegistrarCompraRequest`, `RegistrarVendaRequest`, `AtivoResponse`)
NAO FAZER: nao expor `preco_medio` como campo editavel no DTO de entrada (e sempre calculado); nao criar endpoint de "marcar a mercado" (pendencia, fora de v1).
RETORNO ESPERADO: contrato de API documentado (rota, verbo, body, shape de retorno) para os 3 endpoints.

---

## TASK-016 — Testes de integracao HTTP do AtivosController

STATUS: CONCLUIDA (15 testes novos, 138 no total, inclui teste de regressao do bug de contaId da TASK-015)
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-015
CONTEXTO A LER: regra-de-negocio.md secao 8, 8.1-8.3
ESCOPO: testes HTTP cobrindo: compra em conta que nao e INVESTIMENTO -> 422; compra em conta inexistente -> 404; venda maior que a posicao -> 422; fluxo feliz compra->compra->venda parcial->venda total via HTTP, validando o shape do response (`precoMedio`, `precoAtual`, `quantidade`, `ativa`).
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances.Tests/Controllers/AtivosControllerTests.cs` (novo)
NAO FAZER: nao alterar controller/service pra fazer teste passar sem reportar.
RETORNO ESPERADO: testes passando; relatorio estruturado se achar bug de codigo.

---

## TASK-017 — Saldo calculado da conta com ativos (extensao)

STATUS: CONCLUIDA (aprovada pelo style apos 1 rodada de correcao - duplicacao e dead code em ContaService)
AGENT: levi
FLUXO: Correcao (extensao de codigo ja existente, nao feature nova)
DEPENDENCIAS: TASK-012, TASK-006
CONTEXTO A LER: regra-de-negocio.md secao 8 (paragrafo "Conta com carteira de ativos"), secao 10 (saldo de conta, versao atualizada), secao 8.4
ESCOPO: atualizar `ContaService` (injetando `IAtivoRepository`) para que uma conta INVESTIMENTO com ao menos um Ativo deixe de usar `saldo_manual` e passe a usar a soma de `quantidade x preco_atual` dos ativos `ativa=true`, tanto em `ListarContasInvestimento` (saldo por conta) quanto em `CalcularTotalInvestido` (soma total). Adicionar campo `Saldo` (decimal, sempre populado) em `ContaResponse`; `SaldoManual` continua no DTO so como informativo (null quando a conta tem ativos, igual o schema ja documenta). DECISAO JA CONFIRMADA PELO USUARIO: uma vez que a conta recebeu seu primeiro Ativo, ela fica PERMANENTEMENTE no modo calculado (mesmo que todos os ativos sejam vendidos e o saldo va a zero) — nunca volta a aceitar `saldo_manual`.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Services/ContaService.cs`, `MyFinances/MyFinances/Services/IContaService.cs`, `MyFinances/MyFinances/DTOs/Conta/ContaResponse.cs`, `MyFinances/MyFinances/Controllers/ContasController.cs`
NAO FAZER: nao alterar a regra de conta simples (sem ativos) — continua em `saldo_manual`. Nao implementar patrimonio total do app (mantem o mesmo escopo estrito de investimento da TASK-006). Nao fazer o saldo voltar a `saldo_manual` mesmo se todos os ativos forem vendidos.
RETORNO ESPERADO: `GET /contas?tipo=investimento` retornando saldo correto por conta (calculado ou manual conforme o caso); `GET /contas/investimentos/total` somando o saldo correto de cada conta.

---

## TASK-018 — Testes do saldo calculado com ativos

STATUS: CONCLUIDA (6 testes novos, 145 no total)
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-017
CONTEXTO A LER: regra-de-negocio.md secao 10 (versao atualizada)
ESCOPO: testar que conta INVESTIMENTO com ativos ativos usa soma(`quantidade x preco_atual`) e ignora `saldo_manual`; conta sem ativos continua usando `saldo_manual`; total investido combina corretamente contas dos dois formatos; ativo desativado (`ativa=false`, vendido totalmente) nao entra na soma; conta que teve todos os ativos vendidos continua no modo calculado (saldo zero, nao volta pra saldo_manual).
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances.Tests/Services/ContaServiceTests.cs`
NAO FAZER: nao alterar `ContaService` pra fazer teste passar sem reportar.
RETORNO ESPERADO: testes passando; relatorio estruturado se achar bug.

---

## TASK-019 — Endpoint proxy de cotacao historica (Brapi)

STATUS: CONCLUIDA (aprovada pelo style apos 1 rodada - bug de DI que quebrava o endpoint sempre, corrigido e confirmado com chamada HTTP real)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: nenhuma
CONTEXTO A LER: regra-de-negocio.md secao 8 (paragrafo do grafico) e "Escopo: v1 vs v2"; stack.md secao "Cotacao"
ESCOPO: criar servico de proxy que consulta a API Brapi (ou equivalente) para historico de cotacao de um ticker, exposto via `GET /api/ativos/cotacao/{ticker}/historico?range=...`, SEM persistir nada no banco (chamada sob demanda, sem sync/polling). Tratar erro de API externa (timeout, ticker invalido, rate limit) sem quebrar a aplicacao.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Services/ICotacaoExternaService.cs` (novo), `MyFinances/MyFinances/Services/CotacaoExternaService.cs` (novo), `MyFinances/MyFinances/Controllers/CotacaoController.cs` (novo), `MyFinances/MyFinances/DTOs/Cotacao/*.cs` (novo), `MyFinances/MyFinances/Program.cs` (registro de `HttpClient` nomeado), `MyFinances/MyFinances/appsettings.json`/`appsettings.Development.json` (base URL/chave, se exigir)
NAO FAZER: nao persistir cotacao no banco; nao criar `BackgroundService`/polling; nao expor API key da Brapi no frontend — e exatamente por isso que o proxy vive no backend.
RETORNO ESPERADO: contrato do endpoint (rota, query params, shape — serie de pontos data/preco) documentado; erro de API externa tratado com status HTTP apropriado (ex: 502/504) e mensagem, nunca stack trace cru.

---

## TASK-020 — Camada de dados no front: ativos e cotacao

STATUS: CONCLUIDA (aprovada pelo style de primeira)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-015, TASK-019
CONTEXTO A LER: stack.md secao "Frontend (React)"; clean-code.md "Organizacao (React)"
ESCOPO: criar types (`AtivoResponse`, `RegistrarCompraRequest`, `RegistrarVendaRequest`, `CotacaoHistoricoResponse`), funcoes de `api.ts` (`listarAtivosDaConta`, `registrarCompraAtivo`, `registrarVendaAtivo`, `buscarCotacaoHistorico`) e hooks React Query (`useAtivosDaConta`, `useRegistrarCompraAtivo`, `useRegistrarVendaAtivo`, `useCotacaoHistorico`), seguindo exatamente o padrao ja usado em `features/investimentos/{types.ts,api.ts,hooks/}`.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/investimentos/types.ts`, `MyFinanceFrontEnd/src/features/investimentos/api.ts`, `MyFinanceFrontEnd/src/features/investimentos/query-keys.ts`, `MyFinanceFrontEnd/src/features/investimentos/hooks/useAtivosDaConta.ts` (novo), `MyFinanceFrontEnd/src/features/investimentos/hooks/useRegistrarCompraAtivo.ts` (novo), `MyFinanceFrontEnd/src/features/investimentos/hooks/useRegistrarVendaAtivo.ts` (novo), `MyFinanceFrontEnd/src/features/investimentos/hooks/useCotacaoHistorico.ts` (novo)
NAO FAZER: nao colocar fetch solto em componente; nao renderizar UI aqui (TASK-021 a TASK-024).
RETORNO ESPERADO: hooks tipados (sem `any`), com invalidacao de cache de `useContasInvestimento`/`useTotalInvestido` apos compra/venda (o saldo mudou).

---

## TASK-021 — UI: lista de ativos da conta com saldo calculado

STATUS: CONCLUIDA (aprovada pelo style; achou e corrigiu no meio do caminho um gap real de backend - desativacao de conta com ativos nao era bloqueada)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-020
CONTEXTO A LER: identidade-visual.md; regra-de-negocio.md secao 8 (paragrafo "Conta com carteira de ativos")
ESCOPO: estender `ContaInvestimentoCard`/`ContaInvestimentoItem` para exibir, quando a conta tiver ativos, a lista de ativos (ticker, quantidade, preco medio, preco atual, valor) em vez do fluxo de editar saldo manual (que so faz sentido pra conta simples).
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/investimentos/components/ContaInvestimentoCard.tsx`, `MyFinanceFrontEnd/src/features/investimentos/components/ContaInvestimentoItem.tsx`, `MyFinanceFrontEnd/src/features/investimentos/components/ListaAtivos.tsx` (novo)
NAO FAZER: nao remover o fluxo de saldo manual da conta simples (continua existindo pra contas sem ativos); nao implementar rentabilidade/percentual de ganho (v2).
RETORNO ESPERADO: componente de apresentacao consumindo `useAtivosDaConta`, sem logica de calculo embutida.

---

## TASK-022 — UI: formulario de compra de ativo

STATUS: CONCLUIDA (aprovada pelo style de primeira)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-020
CONTEXTO A LER: identidade-visual.md; regra-de-negocio.md secao 8.1 e 8.2
ESCOPO: formulario (ticker, quantidade, preco unitario, data) para registrar compra, deixando explicito ao usuario que o preco informado passa a valer pra toda a posicao (item 8.1), nao so pra leva comprada.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/investimentos/FormRegistrarCompraAtivo.tsx` (novo), `MyFinanceFrontEnd/src/features/investimentos/lib/validarCompraAtivo.ts` (novo)
NAO FAZER: nao expor campo de `preco_medio` (e sempre calculado no back); nao permitir compra fora do contexto de uma conta de investimento.
RETORNO ESPERADO: componente chamando `useRegistrarCompraAtivo`, com validacao de quantidade/preco > 0.

---

## TASK-023 — UI: acao de venda de ativo

STATUS: PENDENTE
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-020, TASK-021
CONTEXTO A LER: identidade-visual.md; regra-de-negocio.md secao 8.3
ESCOPO: acao de venda (quantidade, preco unitario, data) a partir do item do ativo na lista, com validacao no client pra nao deixar digitar quantidade maior que a posicao atual (validacao de UX; a validacao de verdade e no back, TASK-013), e feedback claro quando o back rejeitar (422).
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/investimentos/FormRegistrarVendaAtivo.tsx` (novo), `MyFinanceFrontEnd/src/features/investimentos/lib/validarVendaAtivo.ts` (novo), `MyFinanceFrontEnd/src/features/investimentos/components/ListaAtivos.tsx`
NAO FAZER: nao implementar nenhum fluxo de "transferir valor vendido pra outra conta" (fora de escopo v1, item 8.3).
RETORNO ESPERADO: componente chamando `useRegistrarVendaAtivo`; quando a quantidade chega a zero, o ativo some da lista (invalidacao de cache, `ativa=false`).

---

## TASK-024 — UI: grafico de historico de cotacao do ativo

STATUS: PENDENTE
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-020, TASK-021
CONTEXTO A LER: identidade-visual.md; regra-de-negocio.md secao 8 (paragrafo do grafico) e "Escopo: v1 vs v2"; stack.md (Recharts)
ESCOPO: tela/modal do ativo com grafico de linha (Recharts) consumindo `useCotacaoHistorico` sob demanda ao abrir; loading/erro tratado (API externa pode falhar, ver TASK-019).
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/investimentos/GraficoCotacaoAtivo.tsx` (novo), `MyFinanceFrontEnd/package.json` (adicionar dependencia `recharts`)
NAO FAZER: nao persistir/cachear cotacao alem do cache padrao do React Query; nao calcular rentabilidade (v2).
RETORNO ESPERADO: componente de apresentacao consumindo o hook, com estado vazio/erro tratado sem quebrar a tela.

---

## Decisoes de modelagem (Killua)

- **Migration cria a tabela `conta` inteira**, nao so os campos de investimento. A tabela e compartilhada por BANCO/CARTAO/INVESTIMENTO no schema.dbml — criar uma versao parcial agora e recriar depois pra outros modulos seria retrabalho e schema fragmentado. Decisao que afeta outros modulos futuros (reaproveitam esta tabela, nao remigram).
- **`tipo=INVESTIMENTO` implica `origem=MANUAL` sempre** — a regra de negocio so descreve investimento como manual (cofrinho, XP, carteira); nao ha mencao a investimento via Open Finance. Virou validacao explicita na Service (TASK-003).
- **Total investido != patrimonio total do app.** A regra diz "ver o total investido no patrimonio", mas patrimonio completo somaria tambem contas BANCO/CARTAO com saldo calculado por lancamento (item 10) — e `lancamento` nao existe ainda no codebase (outro modulo). Escopo mantido estrito: so total de contas de investimento.

## Duvida em aberto para o usuario

Um "patrimonio total" de verdade (somando Open Finance + manual) depende de
modulos que ainda nao existem (conta corrente, cartao, lancamento, sync
Pierre). Vira modulo separado, ou o endpoint de total (TASK-006) ja deveria
nascer com um contrato generico (ex: `/patrimonio`) pensado para acomodar
outros tipos de conta depois? Nao assumido — mantido escopo estrito de
investimento.

---

## Decisoes de modelagem adicionais — modulo de Ativo (TASK-011 a 024)

- **Cotacao historica via proxy no backend** (TASK-019), nao chamada direta do
  frontend — evita expor eventual API key da Brapi no client e centraliza
  tratamento de erro/formato num unico lugar. Tradeoff aceito: mais uma
  chamada de rede (front->back->Brapi) em vez de front->Brapi direto; ganho
  de seguranca/consistencia supera a latencia extra.
- **`AtivoService` depende de `IContaRepository` diretamente** (nao de
  `IContaService`) pra validar existencia/tipo da conta — mantem a camada
  Service->Repository sem Service chamando Service, igual ao restante do
  dominio.
- **Venda nunca reaproveita Ativo desativado**: qualquer compra do mesmo
  ticker apos zerar a posicao cria linha nova em `ativo`, preservando o Ativo
  antigo como historico auditavel (`ativa=false`), conforme item 8.3.
- **Conta com ativos fica permanentemente em modo calculado** (confirmado
  pelo usuario): uma vez que a conta recebeu seu primeiro Ativo, o saldo
  nunca mais volta a usar `saldo_manual`, mesmo que todos os ativos sejam
  vendidos (saldo mostra R$0 ate a proxima compra).
- **Grafico usa Recharts** (confirmado pelo usuario, registrado em
  `stack.md`), nao lightweight-charts — mais simples de integrar com o
  Tailwind/shadcn ja usado no projeto.
