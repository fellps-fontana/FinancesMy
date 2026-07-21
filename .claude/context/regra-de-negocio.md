# Regra de Negocio — Financeiro Pessoal

Documento de referencia obrigatorio. Toda tarefa (codar, revisar, testar) deve
ler este arquivo antes de comecar. As regras aqui descritas tem precedencia
sobre conveniencia de implementacao.

---

## 1. Fontes de dados

O sistema opera com DUAS fontes que convivem no mesmo painel:

- **Open Finance (via API Pierre):** conta corrente e cartao automaticos.
  Dados imutaveis — o sistema apenas le, nunca edita.
- **Manual:** contas criadas pelo usuario (cofrinho, XP, carteira de acoes,
  contas fixas, transferencias internas). O usuario e a fonte da verdade.

Toda CONTA tem o campo `origem` (OPEN_FINANCE | MANUAL).
Todo LANCAMENTO tem a flag `manual` (true | false), exibida como simbolo no UI.

**v1 opera SO com MANUAL** (decisao registrada em "Escopo: v1 vs v2"). O campo
`origem` e o schema ja preveem OPEN_FINANCE (inclusive `pierre_txn_id` ja
migrado em Conta/Lancamento), mas nenhum agent deve implementar sync,
conciliacao ou exclusao especificas de Open Finance (itens 4, 5, 11) na v1.
Decisao nao-retroativa: o schema existente com `pierre_txn_id` fica como esta.

---

## 2. Regra de sinal (CRITICA)

O sinal do campo `valor` NAO e confiavel para determinar entrada ou saida.
No dado do Pierre, transacao de cartao (CREDIT account) vem com valor positivo
mesmo sendo gasto.

**Regra:** usar SEMPRE o campo `tipo` (DEBIT | CREDIT) combinado com o
`account_type` para classificar entrada/saida. Nunca somar `valor` cru.

- DEBIT = saida (gasto)
- CREDIT = entrada (recebimento), EXCETO pagamento de fatura de cartao

Pagamento de fatura de cartao NAO e receita nem despesa: e transferencia
conta corrente -> cartao (ver item 3 e item 12). As compras feitas no cartao
seguem regime de competencia dentro da conta CARTAO e nao sao classificadas
pelo sinal cru (ver item 12).

---

## 3. Transferencias de mesma titularidade

Movimentacao entre contas do proprio usuario nao e gasto nem receita — apenas
muda dinheiro de lugar.

- **Open Finance:** transacoes com categoria "mesma titularidade" aparecem
  duplicadas (saida numa conta, entrada noutra). DEVEM ser excluidas do calculo
  de gasto e de receita.
- **Manual:** transferencia entre contas manuais e registrada explicitamente
  pelo usuario e tambem nao conta como gasto/receita.

**Representacao (schema):** transferencia e modelada como DUAS pernas — dois
lancamentos (saida na origem, entrada no destino) que compartilham o mesmo
`transferencia_id` (tabela `transferencia`). No fluxo de caixa a transferencia
aparece como uma unica linha logica; no calculo de gasto/receita as duas pernas
sao excluidas. O pagamento de fatura de cartao usa exatamente essa estrutura
(item 12).

---

## 4. Exclusao de lancamento Open Finance

**FORA DE ESCOPO v1** — depende de sync ativo com Pierre (item 11), adiado
para v2 (ver "Escopo: v1 vs v2"). Regra mantida documentada para quando a
integracao entrar.

O usuario pode ocultar um lancamento vindo do Open Finance.

**Regra:** exclusao e SOFT-DELETE. Marca `oculto = true`. O sync deve verificar
o `pierre_txn_id` e NUNCA re-importar um lancamento ja marcado como oculto.
Nao deletar fisicamente — o sync traria de volta.

---

## 5. Conciliacao (conta a pagar -> pagamento real)

**Em v1, so existe o caminho manual** (branch Open Finance abaixo fica para
v2, junto do sync — ver "Escopo: v1 vs v2").

Contas a pagar nascem como lancamento PENDENTE. O fechamento depende da origem
da conta de pagamento:

- **Conta de pagamento Open Finance (v2):** o sistema NAO marca como paga
  sozinho. No sync, busca uma transacao OF real que bata com a conta pendente:
  - mesmo `valor`
  - data da transacao dentro de +/- 1 dia do vencimento
  Se achar -> status vira SUGERIDO e o sistema PROPOE o vinculo.
  O usuario CONFIRMA -> status vira PAGO e os dois lancamentos sao vinculados
  (`conciliado_com`). Se nao achar -> permanece PENDENTE.

- **Conta de pagamento manual (v1):** ao marcar como paga, sai automatico. O
  usuario e a fonte da verdade, nao ha o que conferir.

Estados do lancamento em v1: PENDENTE -> PAGO direto (SUGERIDO so existe
quando a branch Open Finance entrar em v2).

---

## 6. Conta fixa

O usuario pode marcar/editar um lancamento como conta fixa.

**Regra:** a conta fixa e um molde (`CONTA_FIXA`) com `dia_vencimento`. Ao
CRIAR ou REATIVAR (`ativa` false->true) uma ContaFixa, o sistema gera
automaticamente um LANCAMENTO PENDENTE para o MES CORRENTE e um para o
PROXIMO MES (2 meses), vinculado por `conta_fixa_id`. Nao ha sync/job
separado na v1 (item 11 e v2) — a geracao acontece SO nesses dois gatilhos.
DECISAO CONFIRMADA COM O USUARIO EM 2026-07-20.

**Idempotencia (obrigatoria):** antes de gerar o Lancamento de um par
ano/mes para uma ContaFixa, o sistema verifica se ja existe um Lancamento
com aquele `conta_fixa_id` + mes/ano de vencimento. Se existir, nao duplica.
Rodar a geracao duas vezes para a mesma ContaFixa/mes e uma operacao segura
(no-op na segunda vez).

**Dia da geracao:** usa `dia_vencimento` da ContaFixa; se o mes tiver menos
dias que esse valor (ex: 31 em abril, ou fevereiro), a data e ajustada para
o ultimo dia do mes — mesmo padrao ja usado por
`FaturaCicloService.CriarDataValida` para o ciclo do cartao.

**Tipo do lancamento gerado:** sempre DEBIT (conta fixa e sempre despesa
recorrente, mesma familia do item 5 "contas a pagar"). Nao existe conta fixa
de recebimento (CREDIT) na v1. Status PENDENTE, `Manual = true`.

**Edicao propaga para lancamentos PENDENTE ja gerados.** Editar valor,
`dia_vencimento` ou categoria de uma ContaFixa atualiza os Lancamentos
vinculados (`conta_fixa_id`) que ainda estao `Status = Pendente`. Lancamentos
`Status = Pago` NUNCA sao alterados (fato historico, dinheiro ja saiu — mesmo
principio do item 13 "valor_total nunca muda apos registro").

**Desativar cancela os lancamentos PENDENTE ja gerados.** Ao desativar
(`ativa = true -> false`) uma ContaFixa, os Lancamentos vinculados com
`Status = Pendente` sao excluidos (hard delete, mesma regra de exclusao de
lancamento manual do item 5/12). Lancamentos `Status = Pago` permanecem
intocados. Reativar volta a gerar os 2 meses (mes corrente + proximo) do
zero, respeitando a idempotencia acima.
DECISOES CONFIRMADAS COM O USUARIO EM 2026-07-20.

---

## 7. Categorias

As categorias sao DO USUARIO, nao do Pierre.

- Tabela mestre propria, com `tipo` (DESPESA | RECEITA).
- Subcategoria via auto-relacionamento (`parent_id`).
- Subcategoria pode ser arquivada (`arquivada = true`), nao deletada.
- A `category` que vem do Pierre e apenas sugestao.

**De-para:** existe uma tela/aba para vincular a string de categoria do Pierre
a uma categoria do usuario (DE_PARA_CATEGORIA).
- Se existe vinculo cadastrado -> aplica a categoria do usuario no import.
- Se NAO existe vinculo -> lancamento fica com `categoria_id = null`
  (sem categoria) e aparece na aba de vinculo pendente.

---

## 8. Cofrinho, investimentos e ativos

Nao classificar por nome de transacao. O modulo de investimentos tem duas
formas independentes, sem relacao uma com a outra:

- **Conta de investimento (saldo simples):** cofrinho Mercado Pago, XP sem
  detalhe de ativo — CONTA MANUAL propria (tipo INVESTIMENTO), saldo
  atualizado pelo usuario via `saldo_manual`, igual qualquer conta manual
  (item 10).
- **Ativo (posicao individual, tela "Investimentos"):** Tesouro Selic, CDB,
  uma acao especifica, fundo imobiliario etc. Registro STANDALONE, SEM
  vinculo com Conta — o usuario nao precisa cadastrar uma "conta XP" antes de
  lancar um Tesouro Selic. Campos: `nome`, `tipo` (RENDA_FIXA |
  RENDA_VARIAVEL), `instituicao` (texto livre, ex: "Nubank"),
  `valor_investido`, `data_compra`, `valor_atual`.

**Decisao de 2026-07-12 (substitui a decisao de 2026-07-06, ver "Escopo: v1
vs v2"):** o modulo anterior de ativo por ticker (compra/venda, preco medio,
cotacao via Brapi sob demanda) foi REMOVIDO do codigo. Investimento detalhado
na v1 NAO tem conexao com nenhuma API de bolsa/cotacao, em nenhuma fase.
`valor_atual` e 100% manual.

### 8.1 Valor atual e evolucao (100% manual)

`valor_atual` e definido pelo usuario — mesmo espirito do `saldo_manual` de
conta manual (item 10), so que no nivel do ativo em vez da conta inteira. No
cadastro, `valor_atual` nasce IGUAL a `valor_investido` (evolucao = 0). So
muda quando o usuario edita explicitamente.

```
evolucao_percentual = (valor_atual - valor_investido) / valor_investido
```

Calculada sob demanda para exibicao, NUNCA armazenada.

### 8.2 Exclusao de ativo

Soft-delete (`ativa = false`), mesmo padrao ja usado no resto do dominio
(`conta.ativa`, `categoria.arquivada`, `lancamento.oculto` — item 4). Sem
hard-delete.

---

## 9. Projecao do mes (dashboard)

Projecao NAO e estimativa futura. E o balanco real do mes corrente:

```
saldo_projetado = (total_recebido_no_mes + total_a_receber_esperado_no_mes)
                  - (total_pago_no_mes + total_a_pagar_no_mes)
```

Considera todas as contas do mes, de PENDENTE ate PAGO, e todo valor recebido
no mes.

O cartao de credito entra na projecao como UMA conta a pagar = total da fatura
atual do mes, com status pago / nao pago (ver item 12). As compras individuais
do cartao NAO entram na projecao — sao competencia e aparecem apenas no
relatorio por categoria.

Contas a Receber (item 13) entram do lado da entrada, simetricas a conta a
pagar: `total_a_receber_esperado_no_mes` soma o SALDO PENDENTE (nao o
valor_total) de todo `conta_receber` com status PENDENTE (se a
`data_prevista` cair no mes corrente) ou PARCIAL (todo mes corrente,
independente da `data_prevista` original, ate o saldo zerar). O que ja foi
recebido conta via `total_recebido_no_mes` (lancamento CREDIT real) — por
isso somar o saldo pendente, e nao o valor_total, evita dupla contagem.

---

## 10. Saldo de conta

- **Conta Open Finance:** saldo e CALCULADO somando os lancamentos
  (respeitando a regra de sinal). Nao armazenar saldo fixo — evita
  desatualizacao.
- **Conta manual (incluindo investimento simples, item 8):** saldo e o campo
  `saldo_manual`, definido pelo usuario.
- **Ativo (item 8):** standalone, NAO participa do saldo de nenhuma Conta. O
  total do modulo de investimentos soma `ativo.valor_atual` separadamente
  (ver tela "Investimentos").

---

## 11. Sincronizacao (sync)

**FORA DE ESCOPO v1** — integracao real com Pierre inteira adiada para v2
(ver "Escopo: v1 vs v2"). Nao implementar nenhum item abaixo na v1.

- Nao precisa ser tempo real. Polling agendado (sugestao: a cada 6h).
- Fluxo: forcar atualizacao no Pierre (manual-update) -> buscar transacoes
  desde a ultima sync -> deduplicar por `pierre_txn_id` -> inserir novas ->
  rodar conciliacao -> aplicar de-para de categoria.
- Respeitar oculto (item 4) e janela de conciliacao de 1 dia (item 5).

---

## 12. Cartao de credito

Modelo estilo Organizze. O cartao e uma CONTA propria (tipo CARTAO) e separa
COMPETENCIA (a compra) de CAIXA (o pagamento da fatura). Essa separacao e o que
evita dupla contagem: a compra vive numa visao, o pagamento vive na outra.

Tres tipos de lancamento na conta CARTAO:

- **Compra:** lancamento vinculado a conta CARTAO, com categoria e data. Regime
  de COMPETENCIA — a divida nasceu, mas o dinheiro ainda nao saiu. NAO aparece
  no lancamento geral / fluxo de caixa. Aparece so na visao por categoria.
- **Pagamento de fatura:** um unico lancamento de TRANSFERENCIA conta corrente
  -> cartao (mesma titularidade, item 3). E a unica linha que sai no lancamento
  geral. Nao tem categoria de despesa.
- **Estorno:** compra negativa dentro do cartao.

**Duas visoes (nucleo do modelo):**

- **Lancamento geral / fluxo de caixa (CAIXA):** mostra o pagamento da fatura
  como saida real. Nao lista as compras individuais.
- **Categorico / gasto por categoria (COMPETENCIA):** ignora o pagamento (e
  transferencia) e soma as compras do cartao, cada uma pela sua categoria.

Como cada compra vive so na visao categorica e o pagamento so no fluxo, nunca ha
dupla contagem.

**Fatura:** recorte das compras por ciclo (`data_fechamento` -> `data_vencimento`).
Serve para agrupar as compras e para casar com o pagamento.

**Saldo do cartao** = compras - pagamentos - estornos. CALCULADO, nao armazenado
(mesma logica do item 10).

**Pagamento x fatura:** o pagamento pode ser PARCIAL (nao precisa quitar o
saldo pendente de uma vez — podem existir varios pagamentos ate a fatura ser
quitada) e ANTECIPADO (pode ocorrer antes do fechamento do ciclo ou do
vencimento, com a fatura ainda ABERTA). Cada pagamento continua fechando saldo
da fatura como um todo, NUNCA compra a compra especifica (igual Organizze) —
so que agora em incrementos. A fatura so recebe status PAGA quando o saldo
pendente (total das compras da fatura menos a soma dos pagamentos ja feitos)
chega a zero.

**Projecao:** o cartao entra na projecao do mes como UMA linha = total da fatura
atual, com status pago / nao pago, tratado como conta a pagar (ver item 9). As
compras individuais nao entram na projecao.

**Origem das compras:** manual por enquanto; futuramente via import da fatura
Nubank (ver Pendencias). O de-para de categoria (item 7) roda sobre a
`descricao` da compra em vez da `category` do Pierre.

### Parcelamento (compra parcelada) — decisao registrada em 2026-07-12

Regra estava omissa (Killua sinalizou) e foi decidida agora: uma compra
parcelada no cartao gera N Lancamentos, um por parcela — NAO um unico
Lancamento pai com N Parcelas dependentes.

Cada Lancamento-parcela e vinculado a fatura do MES DE VENCIMENTO da sua
propria parcela, via `fatura_id`, exatamente como uma compra a vista (mesma
regra de recorte de fatura do item 12: `data_fechamento -> data_vencimento`).
A parcela 1/10 cai na fatura do mes 1, a parcela 2/10 na fatura do mes 2, e
assim por diante — sem NENHUMA logica especial de soma de parcelas: o
mecanismo de fatura ja resolve isso, porque cada parcela e um lancamento
independente com sua propria data.

**Agrupamento (so exibicao — nunca entra em calculo):** as N parcelas da
mesma compra compartilham `compra_parcelada_id`, que aponta para a tabela
`compra_parcelada` (metadados da compra original: descricao, valor_total,
quantidade_parcelas, data_compra). Cada Lancamento-parcela tambem carrega
`parcela_numero` (posicao dela no grupo, ex: 3). Serve so para a UI mostrar
"Notebook Dell 3/10" agrupado — fatura, projecao (item 9) e relatorio por
categoria continuam somando cada Lancamento-parcela individualmente, sem ler
esse agrupamento.

**Decisao tecnica — tabela `parcela` do schema REMOVIDA.** O modelo anterior
(`parcela` como filha de UM Lancamento-compra, com `numero`, `total`,
`valor`, `vencimento`, `paga` proprios) ficou redundante e CONFLITANTE com
este modelo:
- `vencimento` e `valor` da parcela duplicavam exatamente o que
  `lancamento.data` e `lancamento.valor` ja resolvem quando cada parcela e
  seu proprio Lancamento.
- `parcela.paga` (booleano por parcela) CONTRADIZ a regra de pagamento do
  item 12: "cada pagamento fecha saldo da fatura como um todo, NUNCA compra a
  compra especifica". Um campo `paga` por parcela criaria DOIS lugares
  competindo pela verdade de quitacao (`fatura.status = PAGA` vs
  `parcela.paga` individual) — a mesma duplicidade que a regra de pagamento
  parcial do item 12 ja proibe.

`parcela` sai do schema.dbml. No lugar entra `compra_parcelada`, tabela leve
so de metadado de agrupamento — mesmo padrao ja usado por `transferencia`
no item 3 (entidade compartilhada que agrupa N lancamentos por um `_id`, sem
guardar estado de pagamento).

**Calculo do valor de cada parcela:** divisao automatica de
`valor_total / quantidade_parcelas`, SEM edicao manual por parcela. O resto
do arredondamento (centavos que sobram da divisao) vai inteiro para a ULTIMA
parcela, pra soma das N parcelas sempre bater exatamente com `valor_total`.
Exemplo: R$100,00 em 3x = R$33,33 + R$33,33 + R$33,34. Motivo: e assim que
parcelamento de cartao funciona na pratica (valor fixado no ato da compra);
permitir valor manual por parcela abriria brecha pra soma nao bater com
`valor_total`, quebrando a auditoria da compra original sem cobrir nenhum
caso de uso real.

---

## 13. Contas a Receber (Recebivel e Emprestimo)

Modela dois casos com a MESMA entidade (`conta_receber`, campo `tipo`):

- **RECEBIVEL:** valor generico esperado a entrar. NAO exige vinculo com
  nenhuma conta/origem no sistema — pode ser so uma expectativa solta ("vou
  receber X ate tal data"), igual um lembrete financeiro.
- **EMPRESTIMO:** dinheiro emprestado pelo usuario a uma pessoa. `pessoa` e
  texto livre (sem cadastro/entidade propria de PESSOA).

**Valor fixo:** `valor_total` e definido no registro e NUNCA muda — sem
juros, sem correcao. O que varia com o tempo e o saldo pendente, conforme
os recebimentos acontecem.

**Emprestimo: saida como transferencia de perna unica.** Ao registrar um
EMPRESTIMO, o valor sai da conta de origem escolhida pelo usuario. Usa a
MESMA tabela de transferencia do item 3, mas com UMA perna so — nao ha
conta destino real, o destino e uma pessoa fora do sistema:

- `transferencia.conta_destino_id` fica NULL (campo passa a ser opcional,
  usado apenas neste caso — nos demais fluxos de transferencia e pagamento
  de fatura continua obrigatorio).
- `transferencia.conta_receber_id` aponta pro `conta_receber` criado.
- Gera-se UM UNICO `lancamento` (DEBIT, status PAGO) vinculado a essa
  transferencia — nao dois.

A exclusao de gasto/receita (item 3) continua funcionando sem regra nova:
depende so de `lancamento.transferencia_id != null`, nao de existirem as
duas pernas.

**Parcelas / recebimento incremental.** O valor pode ser recebido em mais
de uma vez (mesmo espirito do pagamento parcial de fatura, item 12, mas
sem ciclo/fatura — aqui e incremento livre no tempo, sem quantidade de
parcelas pre-fixada). Cada recebimento gera um `lancamento` novo (CREDIT,
status PAGO) vinculado ao `conta_receber` via `conta_receber_id`, na conta
que o usuario escolher no momento (pode variar entre recebimentos).
Opcionalmente pode receber uma `categoria_id` propria, sobrescrevendo a
categoria sugerida do `conta_receber` pai.

**Recebimento que excede o saldo pendente e REJEITADO.** Se o valor do
recebimento for maior que o `saldo_pendente` atual, o sistema recusa a
operacao (nao registra o lancamento) — o usuario precisa corrigir o valor.
`saldo_pendente` nunca fica negativo.

**Estados:**
```
saldo_pendente = valor_total - soma(lancamentos CREDIT vinculados, status PAGO)

PENDENTE: saldo_pendente == valor_total (nada recebido ainda)
PARCIAL:  0 < saldo_pendente < valor_total
RECEBIDO: saldo_pendente == 0
```

**Projecao do mes:** ver item 9 — saldo pendente de PENDENTE (se
`data_prevista` cair no mes) ou PARCIAL (todo mes corrente, ate zerar)
entra como entrada esperada, simetrica a conta a pagar.

---

## Escopo: v1 vs v2

**Integracao real com Pierre (Open Finance) fica para a v2 — decisao
consciente do usuario em 2026-07-05, nao esquecimento.**

Racional: nenhuma integracao com a API do Pierre foi codada ate a decisao (sem
HTTP client, sem service de sync, sem regra de Open Finance implementada) —
so existe o campo `origem` e a coluna/indice `pierre_txn_id`, ja migrados em
Conta/Lancamento nos modulos cartao, lancamento e investimentos, como scaffold
de schema. Decisao NAO-retroativa: esse schema fica como esta, nao ha
migration de remocao. O que muda e o que entra na v1 daqui pra frente:

- **v1:** contas MANUAL, incluindo investimento em modo carteira de ativos
  (ver item 8 — compra/venda, preco medio, saldo calculado, cotacao Brapi sob
  demanda, grafico de diferenca). Sem sync (item 11), sem exclusao/conciliacao
  Open Finance (itens 4 e 5, branch OF), sem endpoint de integracao com
  Pierre. Pendencias de rate limit/paginacao do Pierre (ver "Pendencias a
  definir") saem da v1 tambem — so voltam a importar quando a integracao
  entrar.
- **v2:** integracao Pierre completa (sync polling, dedup por
  `pierre_txn_id`, conciliacao automatica com transacao OF real, exclusao
  soft-delete de lancamento OF) entra como modulo isolado, sem mexer no que
  ja funciona na v1.

**Modulo de investimento detalhado: EM v1, SEM nenhuma API externa — decisao
final em 2026-07-12.**

Historico da decisao (registrado para nao se perder de novo):
- 2026-07-05: investimento detalhado inteiro adiado pra v2.
- 2026-07-06: revisada — modelo por ticker (quantidade, preco medio, compra,
  venda) entra em v1, com cotacao Brapi sob demanda. Chegou a ser
  implementado e testado (148 testes, `Domain/Ativo.cs`, `AtivosController`,
  `CotacaoExternaService`).
- **2026-07-12: revisada de novo — o modelo por ticker foi REMOVIDO e
  substituido pelo modelo por ativo standalone (item 8: nome, tipo RENDA_FIXA
  ou RENDA_VARIAVEL, instituicao, valor investido, data da compra, valor
  atual manual). Motivo: decisao do usuario de que a v1 nao deve ter NENHUMA
  conexao com API de bolsa, nem sob demanda — o modelo por ticker dependia da
  Brapi para o grafico de cotacao, incompativel com isso. O codigo anterior
  (`Domain/Ativo.cs` por ticker, `MovimentacaoAtivo`, `AtivosController`,
  `CotacaoExternaService`, `CotacaoController` e as telas de compra/venda/
  grafico no front) foi removido, nao mantido como legado morto.**

- **v1 (entra agora):**
  - Ativo standalone (item 8): nome, tipo, instituicao, valor investido, data
    da compra, valor atual (editavel manualmente pelo usuario).
  - Listar, criar, atualizar valor atual, desativar.
  - Resumo por tipo (renda fixa vs renda variavel) para a tela
    "Investimentos".
- **v2 (fora por enquanto):**
  - Qualquer cotacao via API externa (Brapi ou outra), em qualquer
    modalidade (sob demanda ou automatica).
  - Rentabilidade/serie historica automatica, sparkline com base em
    snapshots de valor (nao ha tabela de historico na v1 — ver "Pendencias a
    definir").
  - Dividendos/proventos.

**Na v1**, cofrinho e XP (sem detalhe de ativo) continuam como conta manual
simples com `saldo_manual` (ver item 8). Ativo e um modulo separado, sem
relacao com Conta.

---

## Pendencias a definir

- (v2) Rate limit dos endpoints do Pierre (testar com a key real).
- (v2) Paginacao do get-transactions (confirmar se ha cursor ou se vem tudo).
- Tratamento de PENDING vs POSTED no painel (mostrar separado?).
- Import da fatura Nubank (item 12): definir dedup sem `pierre_txn_id`
  (sugestao: hash de `data + valor + descricao`, ou so importar linhas apos a
  data da ultima importacao). A linha "Pagamento recebido" do CSV da fatura
  NAO e compra — ignorar ou tratar como estorno.
- Ciclo da fatura: como capturar `data_fechamento` e `data_vencimento` do cartao
  (fixo por cartao ou lido do import).
- (item 8) Sparkline por ativo e "% no mes" do total (presentes no mockup)
  exigem historico de snapshots de `valor_atual` ao longo do tempo — nao
  existe tabela de historico na v1. Ativo nasce mostrando evolucao "desde a
  compra" (item 8.1), sem serie temporal. Decidir se entra em v1.2 ou v2.
