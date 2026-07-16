# Modulo de Investimentos

**Status: reescrito em 2026-07-15 — modelo por ticker substituido por Ativo standalone.**

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

- Ativo nasce com `valor_atual == valor_investido` (evolucao = 0 no dia 0).
- `evolucao_percentual = (valor_atual - valor_investido) / valor_investido`
  — calculada sob demanda, NUNCA persistida.
- `valor_atual` so muda por atualizacao manual do usuario (mesmo espirito do
  `saldo_manual` de Conta) — nenhuma fonte externa, nenhuma API de bolsa.
- Exclusao de ativo e soft-delete (`ativa = false`).
- Conta de investimento simples (`saldo_manual`) e Ativo sao independentes —
  nao ha calculo cruzado entre os dois.

## Modelo de dados

- `conta`: inalterado para o caso simples — `tipo=INVESTIMENTO`,
  `origem=MANUAL`, `saldo_manual`.
- `ativo` (redefinido): `nome`, `tipo`, `instituicao`, `valor_investido`,
  `valor_atual`, `data_compra`, `ativa`, `criado_em`, `atualizado_em`. Sem
  `conta_id` — standalone.
- Removidos: `movimentacao_ativo` (historico de compra/venda do modelo por
  ticker nao existe mais).

## Backend

- `AtivosController` (`api/ativos`): criar, listar (so ativas), atualizar
  valor atual, desativar, resumo (`totalInvestido`, `totalAtual`, breakdown
  por tipo com percentual da carteira).
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
- 202 testes (194 pre-existentes + 8 da correcao de validacao).

## Frontend

- `MyFinanceFrontEnd/src/features/investimentos/`:
  - `ListaAtivosPage.tsx` — tela `/investimentos`: resumo (total investido =
    soma de valor atual, cards renda fixa/variavel com % da carteira),
    filtro Todos/Renda fixa/Renda variavel, lista de ativos com evolucao
    colorida (positivo/negativo), modal "Novo ativo", editar valor atual,
    desativar.
  - `ListaContasSimplesPage.tsx` — tela `/contas`: gestao de conta manual
    simples (cofrinho/XP), reaproveitando os componentes que ja existiam
    (`ContaInvestimentoCard`/`Item`, `FormCriarContaInvestimento`).
  - Sem sparkline nem "% no mes" no resumo — nao ha tabela de historico de
    valores na v1 (ver "Pendencias em aberto").
- Removido: `GraficoCotacaoAtivo` (Recharts), `FormRegistrarCompraAtivo`,
  `FormRegistrarVendaAtivo`, `ListaAtivos` (antiga), hooks/lib do fluxo de
  compra/venda/cotacao. Dependencia `recharts` removida do `package.json`
  (sem outro uso no projeto).

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

## Pendencias em aberto (nao decidido)

- Sparkline por ativo e "% no mes" do total (presentes no mockup) exigem
  historico de snapshots de `valor_atual` — nao existe tabela de historico
  na v1. Decidir se entra em v1.2 ou v2.
- "Patrimonio total" do app (somando Open Finance + manual) depende de
  modulos que ainda nao existem (conta corrente, cartao, lancamento).

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
