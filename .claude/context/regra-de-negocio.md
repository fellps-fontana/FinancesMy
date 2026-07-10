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

## 8. Cofrinho, investimentos e acoes

Nao classificar por nome de transacao. Cada um e uma CONTA MANUAL separada:
- Cofrinho Mercado Pago -> conta manual propria.
- Investimentos XP -> conta manual propria.
- Carteira de acoes -> conta manual propria.

Cada conta de investimento opera em um dos dois modos abaixo. Sao mutuamente
exclusivos ao longo do tempo (ver regra de transicao):

**Modo simples:** saldo e o campo `saldo_manual`, editado direto pelo usuario.
Sem ativo, sem ticker. Usado quando o usuario nao quer detalhar posicao (ex:
cofrinho, valor generico).

**Modo carteira de ativos (v1):** em vez de editar um saldo, o usuario registra
COMPRA e VENDA de ativos individuais (ticker, quantidade, preco unitario,
data). O saldo da conta deixa de ser `saldo_manual` e passa a ser CALCULADO:
soma de `quantidade x preco_atual` de cada ativo ativo na carteira.

- **Transicao:** uma vez que a conta recebe seu primeiro ativo, ela fica
  PERMANENTEMENTE no modo carteira (nunca volta a `saldo_manual`, mesmo que
  toda a posicao seja vendida e o saldo va a zero).
- **Preco medio:** recalculado a cada compra do mesmo ticker, ponderado pela
  quantidade e preco medio ja existentes na posicao. O preco da nova compra
  passa a valer pra toda a posicao, nao so pra leva comprada.
- **Venda:** reduz a quantidade; preco medio NUNCA muda numa venda. Venda nao
  gera lancamento nem transferencia em nenhuma outra conta — fora de escopo v1
  ligar a venda do ativo a uma entrada de caixa em outra conta.
- **Cotacao atual:** via API externa (ex: Brapi), fora do escopo do Pierre;
  consultada sob demanda (chamada direta), sem persistir nem sincronizar em
  background.
- **Grafico de rentabilidade:** cada ativo mostra visualmente a DIFERENCA entre
  o preco medio pago e o preco atual (ganho/perda da posicao) — nao e so um
  historico de cotacao, e a comparacao contra o preco medio do usuario.

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
- **v2:** so a integracao real com Pierre (Open Finance) — sync polling, dedup
  por `pierre_txn_id`, conciliacao automatica com transacao OF real, exclusao
  soft-delete de lancamento OF. Modulo isolado, sem mexer no que ja funciona
  na v1.

**Modulo de investimento detalhado ENTRA NA v1 — decisao revista em
2026-07-07, substitui a decisao anterior de 2026-07-05 abaixo.**

Racional da reversao: saldo agregado (`saldo_manual`) sozinho nao basta nem na
v1 — o usuario quer comprar/vender ativo por ativo, com preco medio
recalculado, cotacao atual e grafico de diferenca (preco medio x preco atual)
desde a primeira versao. Regra completa em item 8, modo "carteira de ativos".

O unico motivo pelo qual isso ainda ficava fora era acoplamento com o
fluxo de caixa — e nao ha: `ativo`/`movimentacao_ativo` nao tocam
`lancamento`/`transferencia`, entao o modulo continua isolado do restante do
dominio mesmo entrando na v1. O que segue fora da v1 e so o que depende da API
real do Pierre (ver lista acima) — nao tem relacao com investimento.

Historico (decisao original de 2026-07-05, mantida como registro): a v1 devia
ter so `saldo_manual` porque investimento gira em torno de POSICAO (nao
LANCAMENTO) e depende de fonte externa de cotacao — julgado, na epoca, um
risco de contaminar o schema de fluxo de caixa. Esse racional de isolamento de
schema segue valido (por isso as tabelas nao se cruzam), mas deixou de ser
motivo pra adiar pra v2.

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
