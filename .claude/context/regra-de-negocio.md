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

## 8. Cofrinho, investimentos e ativos

Nao classificar por nome de transacao. Investimento e sempre uma CONTA MANUAL
(tipo INVESTIMENTO), mas existem dois formatos dentro dela:

- **Conta simples** (cofrinho Mercado Pago, XP sem detalhe de ativo): saldo
  atualizado pelo usuario via `saldo_manual`, igual qualquer conta manual
  (item 10). Sem ativo, sem ticker.
- **Conta com carteira de ativos** (carteira de acoes/ETFs/etc): a conta
  INVESTIMENTO passa a conter N ATIVOS (ticker, quantidade, preco medio, preco
  atual — ver 8.1 a 8.4). Quando a conta tem ao menos um ativo, o saldo dela
  deixa de vir de `saldo_manual` e passa a ser CALCULADO como a soma dos
  ativos (ver item 10, atualizado).

Grafico de historico de cotacao (candles/serie temporal) e consultado sob
demanda direto numa API externa (ex: Brapi) quando o usuario abre a tela do
ativo. NAO e armazenado no banco, NAO tem sync/polling — e so uma chamada de
leitura no momento da consulta, sem relacao com o calculo de patrimonio.

### 8.1 Preco atual (usado no calculo do patrimonio)

O `preco_atual` do ativo NAO vem de cotacao de mercado buscada automaticamente
e NAO e o custo de aquisicao. E definido MANUALMENTE pelo usuario, no
momento em que ele registra uma nova compra do mesmo ativo: o preco informado
naquela compra passa a valer para TODA a quantidade que o usuario possui do
ativo (nao so a leva nova comprada).

```
valor_do_ativo = quantidade_atual x preco_atual
```

Isso e o mesmo espirito do `saldo_manual` de conta manual (item 10): o usuario
e a fonte da verdade, so que agora no nivel do ativo, nao da conta inteira.

**Suposicao em aberto (Killua):** a regra so descreve atualizacao de
`preco_atual` via nova COMPRA. Nao ha mecanismo descrito para o usuario so
atualizar a cotacao (marcar a mercado) sem comprar mais — ex: ele nao compra
nada esse mes, mas quer atualizar quanto a posicao vale hoje. Ver
"Pendencias a definir".

### 8.2 Preco medio (custo historico, NAO usado no patrimonio)

`preco_medio` e um numero DIFERENTE de `preco_atual`: e "quanto paguei em
media" (referencia de custo), enquanto `preco_atual` e "quanto vale agora"
(usado no calculo de patrimonio). Os dois coexistem no ativo e nunca se
confundem.

**Recalculo (custo medio ponderado, padrao B3/Receita Federal):** o
`preco_medio` recalcula SOMENTE em COMPRA, usando a quantidade ATUAL em
carteira (pos-venda, se houver) como peso do preco medio anterior:

```
preco_medio_novo = (preco_medio_atual x quantidade_atual + preco_compra_nova x quantidade_nova)
                   / (quantidade_atual + quantidade_nova)
```

VENDA nunca altera `preco_medio` (so reduz `quantidade`) — vender nao muda o
custo medio de quem ficou com a posicao, so a proxima compra recalcula.

**Por que nao e soma simples de todas as compras historicas:** somar todas as
compras que ja existiram, ignorando vendas no meio do caminho, produz numero
errado (conta acoes ja vendidas no denominador). O recalculo incremental
acima, usando a quantidade atual como peso, e o metodo correto.

**Implicacao de modelagem:** para recalcular corretamente apos venda, o
sistema precisa saber, a cada operacao, a quantidade e o preco_medio vigentes
antes da operacao. Por isso o Ativo mantem um HISTORICO de movimentacoes
(compra/venda) — serve tanto de extrato/auditoria quanto de base para
recomputar em caso de bug.

### 8.3 Venda de ativo

**Suposicao explicita (nao e pergunta, e declaracao de escopo minimo):** venda
e uma operacao INTERNA da carteira. Ela reduz a `quantidade` do ativo (venda
parcial) ou desativa o ativo quando a quantidade chega a zero (venda total).
Venda NAO gera lancamento em nenhuma outra conta — nao afeta saldo de conta
corrente, cartao ou qualquer outra CONTA. E apenas o registro "eu tinha X,
vendi Y, agora tenho X-Y". Se o usuario quiser que a venda gere entrada de
dinheiro em outra conta (ex: transferencia do valor vendido para a conta
corrente), isso e pedido futuro, fora de v1.

Quando `quantidade` chega a zero: o ativo e desativado (`ativa = false`),
seguindo o MESMO padrao de soft-delete ja usado no resto do dominio
(`conta.ativa`, `categoria.arquivada`, `lancamento.oculto` — item 4). Nao ha
hard-delete. Se o usuario comprar o mesmo ticker de novo depois de zerar a
posicao, nasce um ATIVO NOVO (nao reaproveita o antigo, que fica historico
inativo) — isso evita herdar preco_medio de um ciclo de investimento
encerrado.

### 8.4 Patrimonio

Ver item 10 (saldo de conta) para como o valor dos ativos entra no saldo da
conta INVESTIMENTO, e "Escopo: v1 vs v2" para o que fica de fora (cotacao
automatica em tempo real, rentabilidade calculada automaticamente,
dividendos).

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
- **Conta manual sem ativos:** saldo e o campo `saldo_manual`, definido pelo
  usuario.
- **Conta manual INVESTIMENTO com ativos (item 8):** saldo NAO usa
  `saldo_manual`. E CALCULADO como a soma de `quantidade x preco_atual` de
  cada ativo `ativa = true` da conta. `saldo_manual` fica `null` nesse caso
  (mesmo tratamento ja dado a Open Finance/CARTAO no schema — saldo calculado
  nao e armazenado).

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

**Pagamento x fatura:** o pagamento fecha o saldo da fatura como um todo, NUNCA
compra a compra (igual Organizze).

**Projecao:** o cartao entra na projecao do mes como UMA linha = total da fatura
atual, com status pago / nao pago, tratado como conta a pagar (ver item 9). As
compras individuais nao entram na projecao.

**Origem das compras:** manual por enquanto; futuramente via import da fatura
Nubank (ver Pendencias). O de-para de categoria (item 7) roda sobre a
`descricao` da compra em vez da `category` do Pierre.

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

- **v1:** so contas MANUAL. Sem sync (item 11), sem exclusao/conciliacao
  Open Finance (itens 4 e 5, branch OF), sem endpoint de integracao com
  Pierre. Pendencias de rate limit/paginacao do Pierre (ver "Pendencias a
  definir") saem da v1 tambem — so voltam a importar quando a integracao
  entrar.
- **v2:** integracao Pierre completa (sync polling, dedup por
  `pierre_txn_id`, conciliacao automatica com transacao OF real, exclusao
  soft-delete de lancamento OF) entra como modulo isolado, sem mexer no que
  ja funciona na v1.

**Modulo de investimento detalhado: PARCIALMENTE em v1 — decisao revisada em
2026-07-06.** A decisao original (investimento detalhado inteiro em v2)
mudou: o essencial de ativo (ticker, quantidade, preco medio, preco atual,
compra, venda) entra em v1. So o que depende de fonte externa em TEMPO REAL
ou de calculo automatico continuo fica em v2.

- **v1 (entra agora):**
  - Conta INVESTIMENTO com carteira de ativos (item 8): ticker, quantidade,
    preco medio (calculado no back, item 8.2), preco atual (informado
    manualmente pelo usuario a cada compra, item 8.1).
  - Registrar compra (nova ou incremento de ativo existente).
  - Registrar venda (reduz quantidade ou desativa o ativo, item 8.3 — sem
    gerar lancamento em outra conta).
  - Grafico de historico de cotacao via API externa (ex: Brapi), consultado
    SOB DEMANDA (o usuario abre a tela e o sistema busca na hora) — nao e
    sync, nao e armazenado, nao afeta patrimonio.
- **v2 (continua de fora):**
  - Cotacao automatica em tempo real / atualizacao periodica de `preco_atual`
    sem intervencao do usuario.
  - Rentabilidade calculada automaticamente (ganho/perda vs preco medio,
    percentual, serie historica de rentabilidade).
  - Dividendos/proventos.

**Na v1**, cofrinho, XP (sem detalhe de ativo) continuam como conta manual
simples com `saldo_manual` (ver item 8). Isso convive com a conta INVESTIMENTO
com carteira de ativos — sao dois formatos da mesma CONTA tipo INVESTIMENTO,
diferenciados pela existencia (ou nao) de linhas em `ativo`.

---

## Pendencias a definir

- (v2) Rate limit dos endpoints do Pierre (testar com a key real).
- (v2) Paginacao do get-transactions (confirmar se ha cursor ou se vem tudo).
- Quantos meses a frente a conta fixa gera.
- Tratamento de PENDING vs POSTED no painel (mostrar separado?).
- Import da fatura Nubank (item 12): definir dedup sem `pierre_txn_id`
  (sugestao: hash de `data + valor + descricao`, ou so importar linhas apos a
  data da ultima importacao). A linha "Pagamento recebido" do CSV da fatura
  NAO e compra — ignorar ou tratar como estorno.
- Ciclo da fatura: como capturar `data_fechamento` e `data_vencimento` do cartao
  (fixo por cartao ou lido do import).
- (item 8.1) O usuario pode atualizar `preco_atual` do ativo SEM registrar uma
  compra nova (so "marcar a mercado")? A regra so descreve atualizacao via
  compra. Se sim, precisa de endpoint proprio (ex: PATCH preco_atual) fora do
  fluxo de compra.
