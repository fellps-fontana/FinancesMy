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

---

## 3. Transferencias de mesma titularidade

Movimentacao entre contas do proprio usuario nao e gasto nem receita — apenas
muda dinheiro de lugar.

- **Open Finance:** transacoes com categoria "mesma titularidade" aparecem
  duplicadas (saida numa conta, entrada noutra). DEVEM ser excluidas do calculo
  de gasto e de receita.
- **Manual:** transferencia entre contas manuais e registrada explicitamente
  pelo usuario e tambem nao conta como gasto/receita.

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

---

## Pendencias a definir

- Rate limit dos endpoints do Pierre (testar com a key real).
- Paginacao do get-transactions (confirmar se ha cursor ou se vem tudo).
- Quantos meses a frente a conta fixa gera.
- Tratamento de PENDING vs POSTED no painel (mostrar separado?).
