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

Telas em `MyFinanceFrontEnd/src/features/cartao/`: `ContaCartaoPage` (conta+
saldo, agora lista **N cartões** navegáveis — o backend nunca teve restrição
de unicidade, o gap era só a UI pegar `data?.[0]`), lançar compra (com
categoria real via `CategoriaSelect` e parcelamento opcional, ver
`docs/modulo-parcelamento-cartao.md`), faturas (pagamento parcial/antecipado
explícito na UX).

**Bloco H (TASK-131 a 136, follow-up front-only)** fechou as lacunas abaixo:
categoria funcional na compra, parcelamento no formulário, múltiplos cartões,
e removeu a tela morta de relatório por categoria em favor de reusar
`/limites-gasto` (ver `docs/limite-gasto.md`) — sem duplicar endpoint.

## Recorrência de cartão (2026-08-26)

Compra recorrente (assinatura tipo Netflix/Spotify) passou a existir como
extensão da `ContaFixa` (destino `Cartao`), não como conceito novo —
detalhes completos, arquitetura e histórico de agents em
`docs/conta-fixa.md`. Aqui fica só o que muda na visão do cartão: cada
ocorrência nasce como Compra normal (regime de competência, vinculada à
fatura do mês), sempre à vista, sem parcelamento; a fatura passou a se
garantir sob demanda ao ser lida (`FaturasController`), não só quando o
usuário lança uma compra manual.

## Lacunas conhecidas

- Antecipar/quitar de uma vez as parcelas restantes de uma compra parcelada
  (pergunta do usuário durante a entrega de recorrência, 2026-08-26) — não
  existe hoje, escopo (v1 ou v2) ainda não decidido.
- Sem `GET` de compras/lançamentos por fatura (telas mostram "não disponível
  nesta versão").
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

**Bloco H (PR #52, follow-up front-only, sem TDD por não tocar regra
crítica)**: `hanzo` fechou múltiplos cartões, categoria funcional e
parcelamento no formulário de compra, e removeu `RelatorioCategoriaPage.tsx`
(chamava endpoint que nunca existiu) redirecionando para `/limites-gasto`.
`style` aprovou em 2 rodadas — 1ª apontou validação de parcelas fora de
`lib/` e tipos órfãos em `types.ts`, ambos corrigidos.

## Notas operacionais

Durante a execução, `levi`/`hanzo`/`style` operaram concorrentemente na mesma
worktree em um ponto — um `git checkout -- .` temporário do `style` (para
comparar contra um commit anterior) atropelou uma edição não commitada do
`hanzo`. Nada foi perdido (recuperado via `git stash`), mas a partir daí os
agents que escrevem arquivo passaram a rodar serializados, não mais em
paralelo, na mesma worktree.

## PR #73 — ajuste ao novo mockup e regras novas (2026-10-14)

**Ciclo e status da fatura (TASK-238, regra 12 "Ciclo e status da fatura").**
O bug antigo: um lancamento com data >= fechamento fechava a fatura ABERTA. Por isso, uma compra parcelada
fechava todas as faturas intermediarias. O modelo novo:
- Estados `FUTURA -> ABERTA -> FECHADA -> PAGA`, com uma unica ABERTA por cartao (indice
  `IX_fatura_conta_aberta` mantido).
- Fechamento automatico pelo calendario (`hoje > data_fechamento`), aplicado de forma preguicosa ao resolver
  lancamento, ao listar ou obter faturas, na antecipacao e na edicao de assinatura. Nao ha job. O dia de
  fechamento e inclusivo.
- O destino de um lancamento e a fatura do ciclo da data:
  - fatura inexistente: e criada;
  - FUTURA ou ABERTA: o lancamento entra;
  - FECHADA (retroativa): entra;
  - PAGA encerrada: rejeita;
  - PAGA nao encerrada: reabre a fatura.
- FUTURA pode ser paga. O dashboard "contas a pagar" so lista ABERTA e FECHADA.
- Codigo:
  - `Domain/FaturaCicloPolitica.cs` (regra pura);
  - `FaturaCicloService` (com `TimeProvider`, `ReconciliarAsync`, `ObterFaturaAtualAsync` e resolver por ciclo);
  - `FaturaRepository.ObterPorContaEDataFechamento` e `ListarParaReconciliacao`.
- Migration de dados `ReclassificarStatusFaturaPorCalendario`:
  - consolida duplicatas de ciclo e aborta se houver duas PAGA;
  - reclassifica as faturas por data;
  - foi aplicada so no banco de preview.

**Endpoint novo:** `GET /api/contas/{contaId}/faturas/{faturaId}/compras` (lista as compras da fatura na tela).

**Tela Cartao** foi refeita no mockup:
- compras da fatura;
- modais de Nova compra, Antecipar e Pagar com `FormModal`;
- badge FUTURA;
- `selecionarFaturaAtual` (ABERTA; senao, a nao FECHADA de menor vencimento).

**Pendencias novas:**
- TASK-239: revalidar a fatura quando a assinatura muda de dia (lacuna pre-existente).
- TASK-240: indice unico `(conta_id, data_fechamento)` contra corrida.
- A migration usa `CURRENT_DATE` do banco, enquanto o app usa a data local do servidor. A reconciliacao preguicosa
  corrige a diferenca no primeiro acesso.

**Agents:**
- killua: diagnostico com 2 bugs extras (ciclo duplicado no dia do fechamento e compra de hoje caindo na ultima
  ABERTA) e o desenho da opcao B (FUTURA).
- mike: 50 testes antes do codigo.
- levi: implementacao e migration.
- hanzo: front.
- style: 1 rodada de correcao (parcela em fatura PAGA posterior era elegivel a antecipacao) e depois APROVADO.
- Suite: 1157/1157.

## Pendencias pos-PR #73 (2026-10-14)

**TASK-240, indice unico de fatura.** O indice `IX_fatura_conta_data_fechamento` impede duas faturas
do mesmo ciclo. Ele vem da migration `AddIndiceUnicoFaturaContaDataFechamento`, que aborta se ja existir duplicata
e roda depois da consolidacao `ReclassificarStatusFaturaPorCalendario`. Em corrida, `CriarFaturaAsync` captura a
violacao, descarta a fatura nova e devolve a vencedora.

**Divida tecnica (aceita pelo style):** no Postgres, dentro de `BeginTransactionAsync`, a recuperacao nao funciona.
- A violacao aborta a transacao (25P02), entao o erro sobe e a operacao inteira e desfeita, sem duplicata.
- Fluxos afetados: compra parcelada, antecipacao, estorno de parcelada e parcelado em banco.
- O SQLite dos testes nao reproduz o problema.
- Solucao futura: savepoint ou excecao de dominio no repositorio. Hoje o servico captura `DbUpdateException`, como
  `AuthService` ja fazia.

**TASK-239, mover a compra da assinatura.** Quando o `dia_referencia` muda e a nova data cai em outro ciclo, a
compra e movida para a fatura desse ciclo, que e criada se nao existir:
- So move se o destino for ABERTA ou FUTURA (`FaturaCicloPolitica.PodeReceberCompraMovida`,
  `FaturaCicloService.ResolverFaturaParaCompraMovidaAsync`).
- Se o destino for FECHADA ou PAGA, a compra fica onde esta e o novo dia vale dali para frente. Valor, descricao e
  categoria continuam propagando.

**Agents:**
- killua: desenho.
- mike: 35 testes RED e o ajuste de 1 cenario que contradizia a regra.
- levi: implementacao.
- style: 1 rodada de correcao (honestidade sobre o Postgres no teste e no comentario), depois APROVADO.
- Suite: 1199/1199.
