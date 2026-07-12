# Módulo: Cartão de Crédito

## Visão geral

Modela o cartão de crédito como uma `Conta` própria (tipo `CARTAO`), separando
COMPETÊNCIA (a compra) de CAIXA (o pagamento da fatura) — núcleo do item 12 da
`regra-de-negocio.md`. Compras vivem na visão categórica; o pagamento da
fatura é a única linha que aparece no lançamento geral/fluxo de caixa. Uma
fatura agrupa as compras de um ciclo (`data_fechamento` → `data_vencimento`) e
aceita pagamento **parcial e antecipado**: pode receber vários pagamentos até
o saldo pendente zerar, inclusive antes do fechamento do ciclo.

Este módulo chegou pronto (PR #14, branch `worktree-cartao-credito-tasks`,
40/40 tasks) mas ficou dias sem sincronizar com a `main`, que nesse meio tempo
dividiu `AppDbContext` em `AppDbContext`/`MyFinancesDbContext` + camada
`Infrastructure/Configurations`, e migrou o frontend para `MyFinanceFrontEnd/`
(Tailwind/shadcn, `apiClient` com auth, rotas protegidas). O trabalho aqui é a
reimplementação do módulo contra essa arquitetura atual — não um merge
mecânico.

## Regras de negócio implementadas

- **Item 12 (competência vs caixa)**: compra é `Lancamento` tipo `Debit` na
  conta `CARTAO`, regime de competência, não aparece no fluxo de caixa.
  Estorno é `Debit`/`Credit` invertido (compra negativa). Pagamento é uma
  `Transferencia` conta corrente → cartão (item 3), com as duas pernas de
  `Lancamento` (saída/entrada) vinculadas por `TransferenciaId`.
- **Pagamento parcial e antecipado** (correção aplicada nesta sessão — o
  documento estava desatualizado): `transferencia.fatura_id` é 1:N, não 1:1.
  Fatura vira `PAGA` só quando saldo pendente (compras − pagamentos) chega a
  zero; aceita pagamento com fatura ainda `ABERTA`.
- **Ciclo de fatura**: uma fatura `ABERTA` por conta (índice único parcial
  `IX_fatura_conta_aberta`). Ao virar o ciclo, a fatura antiga fecha
  (`FECHADA`) antes de abrir a próxima — bug real encontrado e corrigido em
  revisão (violava o índice único todo mês, no caminho normal, não só em
  edge case).
- **Item 2 (regra de sinal)**: saldo da fatura sempre respeita `TipoLancamento`
  (Debit soma, Credit subtrai) — outro bug real de revisão (estorno estava
  sendo somado em vez de subtraído).

## Modelo de dados e endpoints

`Categoria`, `Lancamento`, `Transferencia`, `Fatura` entram em
`MyFinancesDbContext` (não em `AppDbContext`, que é só `Usuario`), com
`Infrastructure/Configurations/*Configuration.cs` próprias. Enums
(`TipoLancamento`, `StatusLancamento`, `StatusFatura`, `TipoCategoria`) no
padrão `HasConversion` já usado por `Conta`. Camada de Repository nova
(não existia na implementação isolada original).

`ContaService`/`ContasController` — que já existiam na main só para
Investimentos — foram fundidos com os do cartão num único `CriarConta`
genérico por `Tipo`. `GET /api/contas?tipo=` aceita `investimento`, `cartao`
e `banco`.

Endpoints do cartão: `POST/PUT /api/cartao/compras`, `POST /api/faturas/{id}/pagamento`,
`POST /api/faturas/{id}/estornos`, `GET /api/contas/{id}/saldo`. Padrão de
erro: retorno funcional em tupla `(bool Sucesso, T?, string? Erro)` — decisão
explícita, diverge do padrão de exception do resto do app.

Telas em `MyFinanceFrontEnd/src/features/cartao/`: conta+saldo, lançar
compra, faturas (com pagamento parcial/antecipado explícito na UX), relatório
por categoria.

## Lacunas conhecidas

- Sem `GET` de compras/lançamentos por fatura (telas mostram "não disponível
  nesta versão").
- Sem endpoint de relatório por categoria nem de categorias do usuário —
  telas do frontend já refletem esse estado em vez de simular dado.
- Cobertura de teste do ciclo de fatura contra índice único real (SQLite)
  existe; contra Postgres real, não foi possível validar neste ambiente
  (sem Docker/Postgres disponível no sandbox).

## O que cada agent entregou

- **killua**: modelou o reencaixe completo (backend: qual DbContext recebe
  cada entidade, o que vira enum, fusão de ContaService/ContasController,
  estratégia de migration; frontend: adaptação para `MyFinanceFrontEnd/`) e
  identificou a contradição de regra (pagamento parcial vs "fecha como um
  todo"), devolvida ao usuário antes de qualquer código.
- **levi**: reimplementou o backend (44 arquivos na primeira passada); 3
  rodadas de correção após revisão do `style` (sinal de estorno invertido,
  fatura sem as duas pernas de lançamento, DTO vazando entity, ciclo de
  fatura não fechava a fatura antiga — bug real que quebraria todo mês).
- **hanzo**: reimplementou as 5 telas contra `MyFinanceFrontEnd/`
  (Tailwind/shadcn/apiClient em vez do CSS Modules/axios isolado da branch
  original); segunda rodada trocou hack de `localStorage` por consulta real
  à API assim que o endpoint ficou disponível.
- **style**: 3 rodadas de revisão do backend. 1ª rodada: 5 bloqueantes reais
  (não frescura de estilo). 2ª: 2 bloqueantes adicionais (fechamento de
  ciclo, escopo de `tipo=banco`). 3ª: aprovado, com ressalva sobre cobertura
  de teste contra banco relacional real.
- **mike**: 17 testes novos (cálculo de saldo respeitando sinal, pagamento
  parcial/antecipado, ciclo de fatura com SQLite validando o índice único
  parcial que o `style` apontou como não coberto).

## Notas operacionais

Durante a execução, `levi`/`hanzo`/`style` operaram concorrentemente na mesma
worktree em um ponto — um `git checkout -- .` temporário do `style` (para
comparar contra um commit anterior) atropelou uma edição não commitada do
`hanzo`. Nada foi perdido (recuperado via `git stash`), mas a partir daí os
agents que escrevem arquivo passaram a rodar serializados, não mais em
paralelo, na mesma worktree.
