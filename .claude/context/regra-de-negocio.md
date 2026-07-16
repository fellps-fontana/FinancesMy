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
- Quantos meses a frente a conta fixa gera.
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
