# Demands — modulos pendentes (v1)

Levantado em 2026-07-12 apos reconciliar main com origin/main (merge e118ee6).
Atualizado em 2026-07-25: reconciliado de novo contra o codigo real em
MyFinances/MyFinances/ (Controllers/, Services/) e docs/. As 6 demandas
abertas neste arquivo (DEMANDA-001 a DEMANDA-006) estao TODAS concluidas e
mergeadas — o arquivo estava desatualizado em varios blocos (mesmo problema
ja registrado em DEMANDA-002: "tasks.md estava desatualizado"). Nao ha
demanda pendente de v1 neste momento.

Modulos ja fechados e mergeados: Usuario/Auth, Investimentos (conta manual),
Investimento Detalhado (Ativo/Cotacao), Categorias (+ de-para), Cartao de
Credito (fatura/compra/pagamento/estorno), Contas a Receber (Recebivel e
Emprestimo), Lancamento Geral / Fluxo de Caixa (DEMANDA-001), Conta Fixa
(DEMANDA-002), Projecao do mes / Dashboard (DEMANDA-003), Limite de gasto
por categoria (DEMANDA-004), Parcelamento de compra no cartao (DEMANDA-005),
Estorno de compra parcelada (DEMANDA-006).

---

## DEMANDA-001 — Lancamento Geral / Fluxo de Caixa

STATUS: CONCLUIDA E MERGEADA (fechada em 2026-07-21, PR #30 + #31). Ver
`docs/lancamento-geral.md` para o resumo vivo do modulo (regras cobertas,
endpoints reais, lacunas, o que cada agent entregou). `LancamentoManualService`,
`TransferenciaService`, `FluxoCaixaService`, `LancamentosController` e
`TransferenciasController` implementados, testados (324/324) e revisados
pelo style. Detalhe de execucao em `tasks.md` (TASK-038 a TASK-050).

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
CONCLUIDA), e formalmente fechada e mergeada em 2026-07-21 (PR #30/#31).
Dependencia satisfeita, sem bloqueio.

---

## DEMANDA-003 — Projecao do mes (Dashboard)

STATUS: CONCLUIDA E MERGEADA. `ProjecaoMesService`, `FaturaProjecaoService`
e `DashboardController` (endpoint `GET /api/dashboard/projecao-mes`)
implementados e testados (TDD RED->GREEN, TASK-057/066). Ver
`docs/projecao-do-mes.md` para o resumo do modulo.

**Escopo entregue (regra-de-negocio.md item 9):**
- `saldo_projetado = total_recebido_no_mes - (total_pago + total_a_pagar)`.
- Considera todas as contas a pagar do mes (PENDENTE ate PAGO) e todo valor
  recebido no mes.
- Cartao de credito entra como UMA linha = total da fatura atual do mes
  (pago/nao pago) — nao lista compras individuais na projecao.

**Dependia de:** DEMANDA-001 (Lancamento Geral) e Cartao de Credito — ambos
prontos, dependencia satisfeita.

---

## DEMANDA-004 — Limite de gasto por categoria

STATUS: CONCLUIDA E MERGEADA (PR #35, worktree `worktree-limite-de-gastos-tasks`).
`LimiteGastoService`, `ILimiteGastoService` e `LimitesGastoController`
implementados; front com dashboard/comparativo por categoria (TASK-059 a
TASK-062). Ver `docs/limite-gasto.md` para a decisao tomada sobre
comportamento ao estourar o limite e o resumo do modulo.

---

## DEMANDA-005 — Parcelamento de compra no cartao

STATUS: CONCLUIDA E MERGEADA. `ComprasParceladasService` e
`CartaoComprasParceladasController` implementados (TASK-025 a TASK-037). Ver
`docs/modulo-parcelamento-cartao.md` para o resumo do modulo.

**Decisao tomada em 2026-07-12 (nao mais regra omissa):** compra parcelada
gera N `Lancamento`s, um por parcela, cada um com `fatura_id` proprio
resolvido encadeando o ciclo de fatura ja existente (nao soma de meses
corridos). Tabela `parcela` do schema.dbml removida (conflitava com a regra
de pagamento por fatura inteira); entra `compra_parcelada` como agrupador
so de exibicao. Split de valor automatico (`valor_total / quantidade`, resto
na ultima parcela). Detalhe completo em regra-de-negocio.md item 12,
subsecao "Parcelamento".

**Ficou fora desta leva, virou demanda propria ja tambem concluida:** ver
DEMANDA-006 (estorno de compra parcelada). Edicao de compra parcelada
existente (mudar `quantidade_parcelas` depois de criada) continua sem
demanda aberta — regra omissa, nao pedida ainda.

---

## DEMANDA-006 — Estorno de compra parcelada

STATUS: CONCLUIDA E MERGEADA (PR #33, worktree
`worktree-estorno-compra-parcelada`). `EstornoCompraParceladaService`
implementado. NOTA: `docs/modulo-parcelamento-cartao.md` ainda lista
"Estorno de compra parcelada: fora de escopo desta leva, regra omissa" —
doc desatualizada, precisa de patch separado (fora do escopo deste ajuste
em demands.md).

**Situacao anterior (historico):** o modulo Cartao de Credito ja tinha
estorno de compra a vista (`EstornoCartaoService`, regra-de-negocio.md item
12 — "Estorno: compra negativa dentro do cartao"). Compra parcelada
(DEMANDA-005) nao cobria estorno — a leva de tasks TASK-025/037 excluia
isso explicitamente. Resolvido por esta demanda (ver decisao abaixo).

**Decisao tomada em 2026-07-20 (nao mais regra omissa):** estorno de compra
parcelada e uma ACAO UNICA sobre a compra inteira (via `compra_parcelada_id`),
que cancela TODAS as parcelas restantes ainda nao pagas — nao existe estorno
parcela-por-parcela isolado. O estorno tambem ALCANCA RETROATIVAMENTE
parcelas ja em fatura PAGA, gerando um lancamento de estorno mesmo numa
fatura ja fechada/paga (nao so removendo lancamentos futuros). Detalhe
completo em regra-de-negocio.md item 12, subsecao "Estorno de compra
parcelada".

**Perguntas originais (respondidas em 2026-07-20 — mantidas como historico):**
- Estornar cancela todas as parcelas futuras, so a proxima, ou nenhuma
  automaticamente? -> RESPONDIDA: todas as parcelas restantes ainda nao
  pagas, automaticamente.
- Parcelas em fatura ja PAGA podem ser estornadas retroativamente? ->
  RESPONDIDA: sim, gera lancamento de estorno na fatura ja paga.
- Estorno e acao unica sobre a compra inteira ou tambem parcela-por-parcela
  isolada? -> RESPONDIDA: acao unica sempre, sem estorno de parcela isolada.

**Pendencia do credito em fatura paga — RESOLVIDA em 2026-07-20:** quando o
estorno retroativo deixa o saldo pendente de uma fatura ja PAGA negativo
(credito), a fatura MANTEM status PAGA e o credito e automaticamente
abatido do total da PROXIMA fatura em aberto do mesmo cartao. Sem mudanca de
status, sem acao manual. Detalhe em regra-de-negocio.md item 12, subsecao
"Estorno de compra parcelada".

**Depende de:** DEMANDA-005 (Lancamento-parcela + `compra_parcelada`)
implementada, ja que o estorno opera sobre esse modelo — CONFIRMADO
implementado e mesclado em main (`ComprasParceladasService`, `CompraParcelada`,
`ICompraParceladaRepository` presentes no codebase em 2026-07-20).

---

## Fora de escopo v1 (nao sao demanda agora)

- Sync real com Pierre / Open Finance (regra item 11) — adiado para v2,
  decisao consciente registrada em regra-de-negocio.md "Escopo: v1 vs v2".
- Exclusao/conciliacao automatica de lancamento Open Finance (itens 4 e 5,
  branch OF) — mesma decisao.
- Import de fatura Nubank (item 12, "Origem das compras") — mencionado como
  futuro em "Pendencias a definir", sem decisao de trazer para v1 ainda.
