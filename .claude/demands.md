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

**Situacao atual:** nao existe nenhum arquivo de codigo (`Domain/ContaFixa.cs`
nao existe). `Lancamento.conta_fixa_id` no schema referencia uma tabela que
ainda nao foi criada.

**Escopo (regra-de-negocio.md item 6):**
- Entidade `ContaFixa`: molde com `dia_vencimento`.
- Geracao mensal: cada conta fixa origina um `Lancamento` novo PENDENTE,
  vinculado por `conta_fixa_id`.
- Definir quantos meses a frente sao gerados (pendencia em aberto no proprio
  regra-de-negocio.md — sugestao la: mes corrente + proximo, regenerar no
  sync mensal). Como o sync (item 11) e v2, a geracao aqui precisa de um
  gatilho v1 (ex: on-demand ou job simples), a decidir com killua.

**Depende de:** DEMANDA-001 (Lancamento Geral) pronto.

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
