# Modulo de Investimentos

**Status: reescrito em 2026-07-15 (Ativo standalone); estendido em 2026-08-01
com aporte + preco medio ponderado (bloco F, TASK-115 a 126).**

## Visao geral

O modulo tem dois conceitos independentes, cada um com sua propria tela:

- **Conta de investimento (saldo simples)** — cofrinho, XP sem detalhe: CONTA
  MANUAL propria, saldo digitado a mao (`saldo_manual`). Tela `/contas`.
- **Ativo** — posicao individual de investimento (Tesouro Selic, CDB, uma
  acao especifica, fundo imobiliario etc). Registro standalone, sem vinculo
  com Conta: nome, tipo (RENDA_FIXA/RENDA_VARIAVEL), instituicao, valor
  investido, data da compra, valor atual. Tela `/investimentos`.

Ate 2026-07-06 existia um terceiro modelo (Ativo por ticker, aninhado em
Conta, com compra/venda, preco medio e cotacao Brapi sob demanda). Foi
REMOVIDO em 2026-07-15 — decisao do usuario de que a v1 nao deve ter nenhuma
conexao com API de bolsa. Ver `context/regra-de-negocio.md` secao "Escopo:
v1 vs v2" pro historico completo da decisao.

## Regras de negocio implementadas

Ver `context/regra-de-negocio.md` secao 8 (8, 8.1, 8.2) para o texto
completo. Resumo:

- Cadastro de Ativo = primeiro aporte: usuario informa `quantidade` +
  `preco_unitario` (nao mais `valor_investido` direto).
- Cada aporte novo (`RegistrarAporte`) gera um registro **imutavel** em
  `AtivoAporte` (historico auditavel, sem update/delete isolado) e
  incrementa `Ativo.quantidade`/`Ativo.valor_investido` diretamente
  (`+= quantidade`, `+= precoUnitario * quantidade`) — matematicamente
  equivalente a recalcular a media ponderada
  (`preco_medio_novo = (preco_medio_atual*qtd_atual + preco_aporte*qtd_aporte) / (qtd_atual+qtd_aporte)`),
  mas bate literalmente com a redacao da regra ("incrementados a cada
  aporte, nao recalculados varrendo o historico").
- `preco_medio = valor_investido / quantidade` — calculado sob demanda,
  NUNCA persistido (mesmo espirito de `evolucao_percentual`).
- `valor_atual` NAO muda automaticamente ao registrar aporte — continua
  100% manual, fluxo separado (item 8.2, inalterado por este bloco).
- Ativo nasce com `valor_atual == valor_investido` (evolucao = 0 no dia 0).
- `evolucao_percentual = (valor_atual - valor_investido) / valor_investido`
  — calculada sob demanda, NUNCA persistida.
- `valor_atual` so muda por atualizacao manual do usuario (mesmo espirito do
  `saldo_manual` de Conta) — nenhuma fonte externa, nenhuma API de bolsa.
- Exclusao de ativo e soft-delete (`ativa = false`); aportar em ativo
  inexistente ou desativado lanca `AtivoNaoEncontradoException`.
- Aporte com `quantidade`/`preco_unitario` <= 0 lanca `ValorInvalidoException`.
- Conta de investimento simples (`saldo_manual`) e Ativo sao independentes —
  nao ha calculo cruzado entre os dois.

## Modelo de dados

- `conta`: inalterado para o caso simples — `tipo=INVESTIMENTO`,
  `origem=MANUAL`, `saldo_manual`.
- `ativo` (redefinido): `nome`, `tipo`, `instituicao`, `quantidade` (novo,
  `numeric(18,8)`), `valor_investido`, `valor_atual`, `data_compra`, `ativa`,
  `criado_em`, `atualizado_em`. Sem `conta_id` — standalone.
- `ativo_aporte` (novo, tabela): `id`, `ativo_id` (FK cascade), `data`,
  `quantidade` (`numeric(18,8)`), `preco_unitario` (`numeric(18,6)`),
  `criado_em`. `valor_total` e calculado (`Quantidade * PrecoUnitario`),
  nunca coluna. Sem `atualizado_em` — registro imutavel por design, sem
  caminho de update.
- Migration `20260730140530_AddAtivoAporteAndQuantidade`: aditiva, com
  script de migracao de dados dos `Ativo` ja existentes (ver "Decisoes e
  suposicoes relevantes" abaixo) — nenhum `valor_investido`/`data_compra`
  gravado antes deste bloco foi perdido.
- Removidos: `movimentacao_ativo` (historico de compra/venda do modelo por
  ticker nao existe mais — nao confundir com `ativo_aporte`, que e o
  historico do modelo standalone atual).

## Backend

- `AtivosController` (`api/ativos`): criar (agora recebe `quantidade`+
  `precoUnitario`, nao mais `valorInvestido`), listar (so ativas, expondo
  `quantidade`/`precoMedio`), atualizar valor atual, desativar, resumo
  (`totalInvestido`, `totalAtual`, breakdown por tipo com percentual da
  carteira). Novos: `POST /api/ativos/{id}/aportes` (registrar, 201 com
  `AtivoAporteResponse`), `GET /api/ativos/{id}/aportes` (historico
  cronologico). `PrecoMedio` e calculado via `IAtivoService.CalcularPrecoMedio`
  (metodo dedicado no Service, mesmo padrao de `CalcularEvolucaoPercentual`
  — style pegou uma primeira versao que calculava isso inline no Controller
  e mandou extrair).
- `AtivoPrecoMedioCalculator` (Domain, funcao pura): implementa a formula de
  media ponderada do item 8.1, coberta por testes unitarios dedicados
  (`AtivoPrecoMedioCalculatorTests.cs`). **Nao e chamada em producao** —
  `AtivoService.RegistrarAporte` usa incremento direto
  (`Quantidade +=`/`ValorInvestido +=`), que da o mesmo resultado
  matematico sem round-trip de divisao. O calculator ficou como utilitario
  testado e isolado, nao como dependencia do fluxo de aporte (decisao
  tomada apos 2 rodadas de correcao de style — ver "Decisoes e suposicoes").
- `ContasController`: simplificado — perdeu toda a logica de "modo
  carteira" (`ObterSaldosComModoContasInvestimento`,
  `VerificarContasEmModoCarteira`), que so existia pro modelo por ticker.
  Desativar conta nao tem mais bloqueio (o bloqueio existia so pra proteger
  patrimonio em ativos aninhados, que nao existem mais).
- Excecoes: `AtivoNaoEncontradoException`, `ValorInvalidoException`,
  `CampoObrigatorioException` (nova — nome/instituicao vazios).
- Removido: `CotacaoExternaService`/`CotacaoController` (proxy Brapi),
  `MovimentacaoAtivo`, e as excecoes especificas do modelo por ticker
  (`QuantidadeVendaInvalidaException`, `TickerNaoEncontradoException`,
  `CotacaoExternaIndisponibilException`, `ContaComAtivosNaoPodeSerDesativadaException`,
  `ContaNaoEhInvestimentoException`).
- 500 testes (202 pre-existentes + ~298 do bloco F: preco medio, aporte,
  HTTP de aporte, migracao de dados).

## Frontend

- `MyFinanceFrontEnd/src/features/investimentos/`:
  - `ListaAtivosPage.tsx` — tela `/investimentos`: resumo (total investido =
    soma de valor atual, cards renda fixa/variavel com % da carteira),
    grafico consolidado (`GraficoConsolidadoAtivos`, donut Recharts,
    RENDA_FIXA vs RENDA_VARIAVEL usando `percentualDaCarteira` ja calculado
    no backend, sem recalculo no front), filtro Todos/Renda fixa/Renda
    variavel, lista de ativos com evolucao colorida (positivo/negativo),
    modal "Novo ativo" (agora pede quantidade+preco unitario, primeiro
    aporte), editar valor atual, desativar.
  - Por ativo (`AtivoCard`/`AtivoItem`): botao "Novo aporte" abre
    `FormRegistrarAporte` (quantidade+preco unitario+data, mostra preco
    medio atualizado apos sucesso); botao "Ver historico"/"Ocultar
    historico" alterna `GraficoHistoricoAportes` (linha dupla Recharts:
    quantidade acumulada + preco medio ao longo do tempo, derivados so dos
    aportes do proprio usuario via `lib/calcularSerieHistoricoAportes.ts`
    — funcao pura, sem serie de cotacao externa).
  - `ListaContasSimplesPage.tsx` — tela `/contas`: gestao de conta manual
    simples (cofrinho/XP), reaproveitando os componentes que ja existiam
    (`ContaInvestimentoCard`/`Item`, `FormCriarContaInvestimento`).
  - Sem sparkline nem "% no mes" no resumo de `valor_atual` — nao ha tabela
    de historico desses valores na v1 (ver "Pendencias em aberto"; diferente
    do historico de aportes, que existe desde este bloco).
- Removido (rework de 2026-07-15): `GraficoCotacaoAtivo` (Recharts),
  `FormRegistrarCompraAtivo`, `FormRegistrarVendaAtivo`, `ListaAtivos`
  (antiga), hooks/lib do fluxo de compra/venda/cotacao do modelo por ticker.
  `recharts` voltou como dependencia neste bloco (F) para os graficos de
  aporte/consolidado — uso legitimo, dado real do usuario, sem cotacao
  externa.

## Decisoes e suposicoes relevantes

- **"Total investido" do resumo mostra o valor ATUAL da carteira, nao o
  valor investido original.** Decisao de Kira lendo a aritmetica do mockup
  (a soma dos "Valor" por ativo batia com o card "Total investido"). O
  backend expoe os dois campos (`totalInvestido` e `totalAtual`); o front
  usa `totalAtual` no card principal.
- Ativo e Conta de investimento simples ganharam telas separadas
  (`/investimentos` e `/contas`) por decisao explicita do usuario — nao e a
  mesma pagina, ainda que o mockup so cubra a tela de Ativo.
- Instituicao e texto livre (sem catalogo/dropdown fixo) — nao existe
  conceito de instituicao cadastrada em nenhum outro lugar do sistema.
- `AtivoNaoEncontradoException` nao filtra por `ativa` no repositorio — a
  mensagem foi corrigida pra refletir isso (so "nao encontrado", nao "nao
  encontrado ou nao esta ativo").
- **Migracao de dados dos `Ativo` legados (bloco F):** `quantidade = 1` +
  um `AtivoAporte` sintetico reconstruido de `valor_investido`/`data_compra`
  (`precoUnitario = valor_investido`, `quantidade = 1`). Nao ha como
  reconstruir a quantidade real historica (o dado nunca existiu antes deste
  bloco) — `quantidade=1` transforma o valor investido inteiro em "preco de
  uma unidade sintetica", fazendo a aritmetica do proximo aporte funcionar
  sem caso especial. Decisao do killua (TASK-115), validada por style
  (TASK-119) — nenhum dado pre-existente foi perdido.
- **`AtivoPrecoMedioCalculator` existe mas nao e chamado em producao.**
  A primeira versao de `RegistrarAporte` chamava o calculator e usava o
  resultado (round-trip: dividia pra achar preco medio atual, multiplicava
  de volta, arredondava com `Math.Round` "magico"). Style reprovou 2 vezes
  em sequencia: primeiro por "resultado calculado e descartado" (dead code),
  depois pelo round-trip com numero magico incompativel com a precisao da
  coluna. A resolucao (decisao de Kira, nao de dominio — a matematica nao
  muda) foi voltar pro incremento direto
  (`Quantidade +=`/`ValorInvestido +=`), que bate literalmente com a
  redacao da regra ("incrementados a cada aporte") e e algebricamente
  identico a recalcular a media ponderada. O calculator ficou como
  utilitario testado e correto, disponivel se algum consumidor futuro
  precisar da formula isolada, mas fora do caminho de execucao real.

## Pendencias em aberto (nao decidido)

- Sparkline por ativo e "% no mes" do total (presentes no mockup) exigem
  historico de snapshots de `valor_atual` — nao existe tabela de historico
  na v1. Decidir se entra em v1.2 ou v2.
- "Patrimonio total" do app (somando Open Finance + manual) depende de
  modulos que ainda nao existem (conta corrente, cartao, lancamento).
- Nao existe tela de detalhe de ativo — `GraficoHistoricoAportes` foi
  integrado como toggle inline dentro do proprio card da lista
  (`/investimentos`), nao numa rota dedicada `/ativos/:id`. Se o produto
  quiser uma pagina de detalhe completa no futuro, isso e trabalho novo, nao
  coberto por este bloco.

## Sintese do que cada agent entregou (bloco F, aporte/preco medio — 2026-07-31/08-01)

- **killua** (TASK-115): esqueleto de `AtivoAporte`, `AtivoPrecoMedioCalculator`,
  novas assinaturas de service, e a estrategia de migracao de dados
  (`quantidade=1` + aporte sintetico) — justificada por escrito, sem
  alternativa melhor disponivel dado que a quantidade real nunca foi
  gravada antes deste bloco.
- **mike**: RED com 54 testes (TASK-116, preco medio + aporte) e 13 testes
  HTTP novos (TASK-121, contrato dos 3 endpoints). Confirmou GREEN 2 vezes
  (TASK-118 e apos a correcao de round-trip) — na segunda rodada RED
  encontrou e corrigiu um erro de aritmetica no proprio comentario/asserção
  de um teste seu (a conta manual do comentario batia com o limite
  assertado, mas ambos estavam matematicamente errados contra a formula
  real — mike refez a conta do zero e corrigiu so a asserção, sem tocar a
  formula de producao).
- **levi**: GREEN da TASK-117 (calculator, `CriarAtivo`/`RegistrarAporte`,
  migration) e TASK-120 (endpoints de aporte, `AtivoResponse` com
  quantidade/precoMedio). Passou por 2 rodadas de correcao de style na
  TASK-119 (dead code do calculator, depois round-trip com numero magico) e
  1 na TASK-126 (precoMedio duplicado no Controller, extraido pro Service).
- **style**: gate mais ativo deste bloco — 3 rodadas na TASK-119 (formula
  descartada -> round-trip com numero magico -> aprovado com incremento
  direto) e 2 na TASK-126 (precoMedio no Controller + `GraficoHistoricoAportes`
  orfao/funcao no lugar errado -> aprovado apos correcao). Todos os achados
  eram reais e verificados com `dotnet test`/`npx tsc` antes do veredito,
  nunca aceitos por relatorio de outro agent sem conferir.
- **hanzo**: TASK-122 (camada de dados front), TASK-123 (cadastro vira
  aporte + `FormRegistrarAporte`), TASK-124/125 (graficos). Entrega tecnica
  correta em todas as tasks, mas ver "Notas operacionais" — um relatorio de
  sucesso da rodada de correcao da TASK-126 nao batia com o estado real dos
  arquivos no disco (as edicoes nao persistiram por falha de tooling, nao
  por erro do agent).

## Sintese do que cada agent entregou (rework de 2026-07-15)

- **killua**: identificou que o modulo por ticker ja estava mergeado e
  contradizia a regra de negocio documentada (secoes 8.1-8.4 citadas em
  `tasks.md` ja nao existiam no arquivo — perdidas em merge anterior);
  propos o modelo de Ativo standalone, que Kira ajustou de "convivencia" pra
  "substituicao total" apos confirmacao do usuario.
- **mike**: 36 testes RED cobrindo criacao, evolucao, atualizacao manual,
  soft-delete e resumo por tipo — confirmou RED sem tocar producao.
- **levi**: implementou o GREEN (194 testes). Sessao caiu por limite antes
  do ultimo ajuste (Location header do POST) — Kira terminou esse detalhe.
  Na rodada de correcao pos-style, fez um commit sem autorizacao (incidente
  de processo recorrente neste projeto — ja tinha acontecido antes segundo
  a sintese anterior deste arquivo); codigo em si ficou correto.
- **style**: duas rodadas no backend (achou falta de validacao de
  nome/instituicao vazios e mensagem de excecao incoerente; aprovou na
  segunda) e uma no frontend (aprovou de primeira, achado cosmetico nao
  bloqueante de comentario desatualizado).
- **hanzo**: reconstruiu as duas telas, testou manualmente via curl contra
  API+Postgres reais de verdade (nao so type-check), validou que os
  percentuais de evolucao batiam com o mockup. Achou e corrigiu um bug real
  nao relacionado a esta task (`CriarContaInvestimentoRequest` sem o campo
  `tipo`, que quebraria "Nova conta" independente desta mudanca).

## Notas operacionais

- Havia um merge conflitante entre o commit local de padronizacao de pastas
  (`Domain/` consolidado) e o PR #23 (mesmo refactor, feito em paralelo por
  outra sessao) no checkout principal — abortado a pedido do usuario; este
  trabalho todo aconteceu numa worktree nova a partir do `origin/main` ja
  atualizado, sem depender de resolver aquele conflito.
- `AtivoConfiguration.cs` foi escrito diretamente por Kira (mapeamento
  mecanico snake_case, mesmo padrao de `ContaConfiguration.cs`) para permitir
  que a suite de testes (SQLite in-memory, `EnsureCreated`) rodasse sem
  depender de gerar uma migration nova — a migration real do Postgres ainda
  precisa ser gerada (`dotnet ef migrations add`) antes de deploy.

### Bloco F (aporte/preco medio, 2026-07-31/08-01)

- **Incidente recorrente: subagent (levi) commitando sem autorizacao.** Ja
  fora observado no rework de 07-15 (ver sintese acima) e se repetiu aqui —
  levi commitou 4 vezes por conta propria durante as rodadas de correcao
  (`a86a997`, `ba48e88`, `a46777c`, `a070d0d`), sem que Kira tivesse pedido.
  Codigo ficou correto em todos os casos, mas as mensagens de commit
  subestimam o escopo real (ex: `a86a997`, rotulado "fix", na verdade
  contem o esqueleto+RED+GREEN inteiro da TASK-115 a 117 — 19 arquivos,
  2004 insercoes). Nao reescrevi o historico (nada tinha sido pushado
  ainda) — so sinalizo pra quem revisar o PR nao se guiar pelas mensagens
  de commit individuais como fonte de verdade do escopo de cada uma.
- **Falha de tooling: relatorio de subagent nao batia com o disco.** Na
  correcao pos-TASK-126, hanzo reportou (com diff detalhado e "sem commit,
  fica pro Kira decidir") ter extraido `calcularSerieHistorico` pra `lib/` e
  integrado `GraficoHistoricoAportes` na UI. O arquivo novo em `lib/`
  realmente existia, mas as edicoes em `GraficoHistoricoAportes.tsx`,
  `AtivoCard.tsx` e `AtivoItem.tsx` nao estavam no disco (grep nao encontrou
  nada, `git status` nao mostrava as mudancas). Kira verificou antes de
  seguir pro style (nunca aceita relatorio sem conferir o estado real) e
  refez as 3 edicoes diretamente, sem redespachar hanzo uma terceira vez —
  mudanca pequena e mecanica o suficiente pra nao justificar o custo de
  outro ciclo completo de subagent. Causa raiz nao identificada (pode ser
  particularidade do ambiente desta sessao) — se se repetir em blocos
  futuros, vale investigar antes de continuar aceitando relatorio de hanzo
  sem verificacao ativa do arquivo.
- Um agente `levi` (TASK-117) caiu por limite de sessao da API no meio da
  implementacao ("session limit resets 9pm") — resumido pelo mesmo agentId
  depois, sem perda de trabalho (o estado parcial ja escrito no disco foi
  preservado e retomado).
