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

O usuario pode ocultar um lancamento vindo do Open Finance.

**Regra:** exclusao e SOFT-DELETE. Marca `oculto = true`. O sync deve verificar
o `pierre_txn_id` e NUNCA re-importar um lancamento ja marcado como oculto.
Nao deletar fisicamente — o sync traria de volta.

---

## 5. Conciliacao (conta a pagar -> pagamento real)

Contas a pagar nascem como lancamento PENDENTE. O fechamento depende da origem
da conta de pagamento:

- **Conta de pagamento Open Finance:** o sistema NAO marca como paga sozinho.
  No sync, busca uma transacao OF real que bata com a conta pendente:
  - mesmo `valor`
  - data da transacao dentro de +/- 1 dia do vencimento
  Se achar -> status vira SUGERIDO e o sistema PROPOE o vinculo.
  O usuario CONFIRMA -> status vira PAGO e os dois lancamentos sao vinculados
  (`conciliado_com`). Se nao achar -> permanece PENDENTE.

- **Conta de pagamento manual:** ao marcar como paga, sai automatico. O usuario
  e a fonte da verdade, nao ha o que conferir.

Estados do lancamento: PENDENTE -> SUGERIDO -> PAGO.
(Manual pula SUGERIDO: PENDENTE -> PAGO direto.)

---

## 6. Conta fixa

O usuario pode marcar/editar um lancamento como conta fixa.

**Regra:** a conta fixa e um molde (CONTA_FIXA) com `dia_vencimento`. A cada mes
ela origina um LANCAMENTO novo com status PENDENTE, vinculado por
`conta_fixa_id`. Definir no codigo quantos meses a frente sao gerados
(sugestao: gerar o mes corrente + proximo, regenerar no sync mensal).

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

## 8. Cofrinho, investimentos e acoes

Nao classificar por nome de transacao. Cada um e uma CONTA MANUAL separada:
- Cofrinho Mercado Pago -> conta manual propria, saldo atualizado pelo usuario.
- Investimentos XP -> conta manual propria.
- Carteira de acoes -> conta manual; cotacao atual via API externa (ex: Brapi),
  fora do escopo do Pierre.

---

## 9. Projecao do mes (dashboard)

Projecao NAO e estimativa futura. E o balanco real do mes corrente:

```
saldo_projetado = total_recebido_no_mes - (total_pago + total_a_pagar)
```

Considera todas as contas do mes, de PENDENTE ate PAGO, e todo valor recebido
no mes.

O cartao de credito entra na projecao como UMA conta a pagar = total da fatura
atual do mes, com status pago / nao pago (ver item 12). As compras individuais
do cartao NAO entram na projecao — sao competencia e aparecem apenas no
relatorio por categoria.

---

## 10. Saldo de conta

- **Conta Open Finance:** saldo e CALCULADO somando os lancamentos
  (respeitando a regra de sinal). Nao armazenar saldo fixo — evita
  desatualizacao.
- **Conta manual:** saldo e o campo `saldo_manual`, definido pelo usuario.

---

## 11. Sincronizacao (sync)

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

**Calculo do ciclo:** `data_vencimento` pode cair no MESMO mes do fechamento
quando `dia_vencimento` (numerico) for maior que `dia_fechamento` (ex: fecha
dia 10, vence dia 20 -> mesmo mes); caso contrario cai no mes seguinte. Uma
compra feita EXATAMENTE no dia do fechamento ja cai na proxima fatura (o
fechamento daquele dia e considerado consumado, nao aceita mais compra no
mesmo dia).

**Saldo do cartao** = compras - pagamentos - estornos. CALCULADO, nao armazenado
(mesma logica do item 10).

**Compra em fatura ja fechada/paga:** uma compra (nova ou editada) cuja data
cai no ciclo de uma fatura:
- **PAGA:** REJEITADA. Fatura paga nao aceita mais lancamento.
- **FECHADA (nao paga):** ACEITA, mesmo sendo retroativa — vincula normalmente
  a essa fatura ja fechada.
- **Sem fatura ainda para aquele ciclo (ciclo atual ou futuro):** resolve/cria
  a fatura normalmente com status ABERTA (comportamento padrao de resolucao
  de ciclo).
- **Sem fatura ainda para um ciclo MUITO retroativo (anterior a uma fatura
  ABERTA mais recente ja existente para a mesma conta):** REJEITADA de forma
  definitiva. So pode existir uma fatura ABERTA por conta; nao ha suporte a
  compra retroativa alem do ciclo ainda aberto mais antigo. Usuario recebe
  erro claro e ajusta a data.

**Pagamento x fatura (revisado):** o pagamento pode ser ANTECIPADO (fatura
ainda ABERTA) e PARCIAL (varios pagamentos ate quitar). Uma fatura passa a
ter VARIAS Transferencias associadas (1:N, nao mais 1:1) — `transferencia.fatura_id`
em vez de `fatura.transferencia_id`.

- **Valor do pagamento:** informado pelo client (nao mais calculado
  automaticamente como o total). Validado contra o saldo pendente da fatura
  (`saldo_pendente = total_lancamentos_da_fatura - soma_dos_pagamentos_ja_feitos`).
  Pagamento com valor > saldo_pendente e REJEITADO (overpayment). Pagamento
  com valor <= 0 e REJEITADO.
- **Faturas pagaveis:** ABERTA ou FECHADA, desde que `saldo_pendente > 0`.
  Fatura com `saldo_pendente <= 0` (ja quitada) REJEITA novo pagamento,
  independente do Status rotulado.
- **Transicao de Status apos pagamento:**
  - Se a fatura estava FECHADA e o pagamento zera o saldo pendente -> Status
    vira PAGA.
  - Se a fatura estava ABERTA e o pagamento zera o saldo pendente -> Status
    CONTINUA ABERTA (nao pula pra PAGA). Continua aceitando compra normalmente
    ate o ciclo fechar.
  - Quando o ciclo fecha (transicao lazy ABERTA->FECHADA, ver acima): se a
    fatura ja estiver com saldo_pendente <= 0 nesse momento (quitada
    antecipadamente), o Status vai direto pra PAGA em vez de FECHADA.

**Projecao:** o cartao entra na projecao do mes como UMA linha = a fatura cujo
`data_vencimento` cai no mes pedido, com status pago / nao pago, tratado como
conta a pagar (ver item 9). Com pagamento parcial (ver acima), "nao pago" usa
o `saldo_pendente` restante (nao o total original) e "pago" so quando
`saldo_pendente <= 0`. As compras individuais nao entram na projecao.

**Escopo entre modulos:** o endpoint completo de projecao (saldo_projetado =
recebido - pago - a_pagar) depende de dados que o modulo de cartao nao possui
(lancamento avulso, conta fixa — de outro modulo). O modulo de cartao expoe
so a sua fatia (GET /api/cartoes/{contaId}/projecao); o modulo responsavel
pelo lancamento geral consome isso pra montar o saldo_projetado completo.

**Origem das compras:** manual por enquanto; futuramente via import da fatura
Nubank (ver Pendencias). O de-para de categoria (item 7) roda sobre a
`descricao` da compra em vez da `category` do Pierre.

**Status do lancamento de compra:** a maquina de estado PENDENTE/SUGERIDO/PAGO
(item 5) e para conciliacao de conta a pagar e nao se aplica a compra
individual de cartao — quem e conta a pagar e a fatura inteira (ver acima e
item 9). Toda compra de cartao nasce com `status = PAGO`, fixo, desde a
criacao.

---

## Escopo: v1 vs v2

**Modulo de investimento detalhado fica para a v2 — decisao consciente, nao
esquecimento.**

Racional: o investimento detalhado (acoes individuais, cotacao ao vivo via API
externa, rentabilidade, preco medio, dividendos) e de natureza diferente dos
demais modulos. Os outros giram em torno de `lancamento` e fluxo de caixa
(entrou/saiu, pago/pendente). O investimento gira em torno de POSICAO (X ativos
a um preco medio cujo valor flutua sem nenhum lancamento ocorrer) e depende de
fonte externa de cotacao. Misturar isso na v1 contaminaria o schema de fluxo de
caixa.

**Na v1**, investimento e representado como CONTA MANUAL (ver item 8): cofrinho,
XP e carteira entram com saldo atualizado pelo usuario. Isso ja cobre o
essencial — ver o total investido no patrimonio.

**Na v2**, entra o modulo dedicado: tabela de ativos (ticker, qtd, preco medio),
integracao com API de cotacao (ex: Brapi), aba de carteira com rentabilidade.
Entra como modulo isolado, sem mexer no que ja funciona na v1.

**A integracao Open Finance (Pierre) tambem fica para a v2 — decisao consciente,
nao esquecimento.**

Na v1, o app opera SOMENTE com contas e lancamentos manuais. Os itens 1, 4, 5,
7 e 11 (fonte Open Finance, exclusao de lancamento OF, conciliacao com conta OF,
de-para de categoria Pierre, sincronizacao) descrevem a REGRA de como o sistema
deve se comportar quando a integracao existir, mas nao sao construidos na v1 —
sem Pierre conectado, nao ha o que sincronizar nem categoria de origem externa
para vincular.

Consequencia direta na v1: toda CONTA tem `origem = MANUAL`, todo LANCAMENTO
tem `manual = true`. A tela de de-para de categoria (item 7) so entra em pauta
quando a integracao Pierre for implementada, junto com sync, conciliacao
automatica (item 5) e exclusao soft-delete de lancamento OF (item 4).

---

## Pendencias a definir

- Rate limit dos endpoints do Pierre (testar com a key real).
- Paginacao do get-transactions (confirmar se ha cursor ou se vem tudo).
- Quantos meses a frente a conta fixa gera.
- Tratamento de PENDING vs POSTED no painel (mostrar separado?).
- Import da fatura Nubank (item 12): definir dedup sem `pierre_txn_id`
  (sugestao: hash de `data + valor + descricao`, ou so importar linhas apos a
  data da ultima importacao). A linha "Pagamento recebido" do CSV da fatura
  NAO e compra — ignorar ou tratar como estorno.
- Ciclo da fatura: como capturar `data_fechamento` e `data_vencimento` do cartao
  (fixo por cartao ou lido do import).
