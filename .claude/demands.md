# Demands — modulos pendentes (v1)

Levantado em 2026-07-12 apos reconciliar main com origin/main (merge e118ee6).
Base: contas/regra-de-negocio.md vs codigo real em MyFinances/MyFinances.
Modulos ja fechados e mergeados: Usuario/Auth, Investimentos (conta manual),
Investimento Detalhado (Ativo/Cotacao), Categorias (+ de-para), Cartao de
Credito (fatura/compra/pagamento/estorno).

---

## DEMANDA-001 — Lancamento Geral / Fluxo de Caixa

**Situacao atual:** as entidades `Lancamento` e `Transferencia` existem
(Domain + Repository + Configuration), mas so sao consumidas internamente
pelo modulo Cartao (CompraCartaoService grava `Lancamento` para compras de
cartao). Nao existe `LancamentoService`, `LancamentosController`,
`TransferenciaService` nem endpoint de transferencia manual.

**Escopo:**
- CRUD de lancamento manual em conta BANCO/MANUAL (nao-cartao).
- Regra de sinal (regra-de-negocio.md item 2): tipo DEBIT/CREDIT como fonte
  da verdade, nunca o sinal cru de `valor`.
- Transferencia entre contas do proprio usuario (item 3): duas pernas
  compartilhando `transferencia_id`, excluidas do calculo de gasto/receita.
- Contas a pagar — caminho manual (item 5): lancamento PENDENTE -> marcar
  como PAGO direto (sem SUGERIDO, que e exclusivo do caminho Open Finance/v2).
- Listagem geral / fluxo de caixa (item 12, visao CAIXA): lista lancamentos
  exceto compras de cartao (essas ficam so na visao categorica).

**Depende de:** Categorias (pronto) para vincular `categoria_id`.
**Bloqueia:** Conta Fixa (DEMANDA-002) e Projecao do mes (DEMANDA-003), que
geram/consomem lancamento.

---

## DEMANDA-002 — Conta Fixa

STATUS: CONCLUIDA em 2026-07-23 (worktree `conta-fixa-tasks`). Todas as
tasks TASK-051 a TASK-065 fechadas (backend + front). Regra critica
(ContaFixaLancamentoFactory/ContaFixaService) com ciclo TDD completo,
aprovada pelo style em 2 rodadas apos achados reais (validacao de
DiaVencimento/Valor ausente, string magica decidindo status HTTP). Ver
`docs/conta-fixa.md` para o resumo completo do modulo.

**Escopo entregue (regra-de-negocio.md item 6):**
- Entidade `ContaFixa`: molde com `dia_vencimento`.
- Geracao: ao CRIAR ou REATIVAR uma ContaFixa, gera Lancamento PENDENTE pro
  mes corrente + proximo (2 meses), vinculado por `conta_fixa_id`,
  idempotente.
- Editar propaga pra Lancamentos `Status=Pendente` (nunca `Pago`); desativar
  exclui os `Pendente` (nunca `Pago`). Tipo do lancamento gerado sempre
  DEBIT.

**Dependia de:** DEMANDA-001 (Lancamento Geral) — CONFIRMADO EM DISCO em
2026-07-20 que ja estava pronto (commits 69f8cf7..83b172e ja mergeados em
main; `tasks.md` estava desatualizado, TASK-039/050 corrigidas para
CONCLUIDA).

---

## DEMANDA-003 — Projecao do mes (Dashboard)

**Situacao atual:** nenhum controller/service de projecao ou dashboard no
codigo.

**Escopo (regra-de-negocio.md item 9):**
- `saldo_projetado = total_recebido_no_mes - (total_pago + total_a_pagar)`.
- Considera todas as contas a pagar do mes (PENDENTE ate PAGO) e todo valor
  recebido no mes.
- Cartao de credito entra como UMA linha = total da fatura atual do mes
  (pago/nao pago) — nao lista compras individuais na projecao.

**Depende de:** DEMANDA-001 (Lancamento Geral) e Cartao de Credito (pronto,
ja expoe saldo/fatura calculados) para agregar os dois.

---

## DEMANDA-004 — Limite de gasto por categoria

**Situacao atual:** tabela `limite_gasto` existe no schema.dbml mas nao esta
descrita em nenhum item numerado da regra-de-negocio.md, e nao ha nenhum
codigo (`Domain/LimiteGasto.cs` inexistente).

**Escopo:** indefinido alem do schema (`categoria_id`, `valor_limite`,
`periodo` default MENSAL). **Nao assumir comportamento** — regra omissa.
Antes de arquitetar, perguntar ao usuario: o que acontece ao estourar o
limite (bloqueio? alerta? so exibicao no relatorio por categoria)? Onde isso
aparece na UI?

---

## DEMANDA-005 — Parcelamento de compra no cartao

STATUS: EM ANDAMENTO — arquitetado e com tasks geradas na worktree
`parcelamento-cartao-tasks` (TASK-025 a TASK-037, ver tasks.md la).

**Decisao tomada em 2026-07-12 (nao mais regra omissa):** compra parcelada
gera N `Lancamento`s, um por parcela, cada um com `fatura_id` proprio
resolvido encadeando o ciclo de fatura ja existente (nao soma de meses
corridos). Tabela `parcela` do schema.dbml removida (conflitava com a regra
de pagamento por fatura inteira); entra `compra_parcelada` como agrupador
so de exibicao. Split de valor automatico (`valor_total / quantidade`, resto
na ultima parcela). Detalhe completo em regra-de-negocio.md item 12,
subsecao "Parcelamento".

**Escopo original desta demanda ja resolvido** — o que falta agora e so
execucao das tasks (levi/mike/style), nao mais arquitetura.

**Ficou fora desta leva, viraram demandas proprias:** ver DEMANDA-006
(estorno de compra parcelada). Edicao de compra parcelada existente (mudar
`quantidade_parcelas` depois de criada) continua sem demanda aberta — regra
omissa, nao pedida ainda.

---

## DEMANDA-006 — Estorno de compra parcelada

**Situacao atual:** o modulo Cartao de Credito ja tem estorno de compra a
vista (`EstornoCartaoService`, regra-de-negocio.md item 12 — "Estorno: compra
negativa dentro do cartao"). Compra parcelada (DEMANDA-005, em andamento) nao
cobre estorno — a leva de tasks TASK-025/037 exclui isso explicitamente.

**Escopo:** regra omissa, nao decidida. Antes de arquitetar, perguntar ao
usuario:
- Estornar uma compra parcelada cancela **todas as parcelas futuras** (as
  que ainda nao caíram numa fatura PAGA), so a proxima parcela, ou nenhuma
  automaticamente (usuario estorna parcela por parcela manualmente, cada uma
  como uma compra a vista qualquer, via o fluxo que ja existe)?
- Parcelas que ja estao numa fatura PAGA (dinheiro ja saiu) podem ser
  estornadas retroativamente, ou o estorno so alcança parcelas em fatura
  ABERTA/FECHADA (ainda nao paga)? Isso decide se o estorno de parcela
  precisa gerar um lancamento de estorno numa fatura ja fechada (regra de
  pagamento parcial do item 12 fica mais complexa) ou so remove/anula
  lancamentos futuros.
- O estorno de uma compra parcelada e uma acao unica ("estornar a compra
  inteira", que internamente cancela N-k parcelas restantes), ou o usuario
  tambem precisa poder estornar SO uma parcela especifica no meio (ex:
  parcela 5 de 10), deixando as demais intactas?

**Depende de:** DEMANDA-005 (Lancamento-parcela + `compra_parcelada`)
implementada, ja que o estorno opera sobre esse modelo.

---

## Fora de escopo v1 (nao sao demanda agora)

- Sync real com Pierre / Open Finance (regra item 11) — adiado para v2,
  decisao consciente registrada em regra-de-negocio.md "Escopo: v1 vs v2".
- Exclusao/conciliacao automatica de lancamento Open Finance (itens 4 e 5,
  branch OF) — mesma decisao.
- Import de fatura Nubank (item 12, "Origem das compras") — mencionado como
  futuro em "Pendencias a definir", sem decisao de trazer para v1 ainda.
