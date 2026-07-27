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

**Regra:** a conta fixa e um molde (`CONTA_FIXA`) com `dia_vencimento` e
`periodicidade`. Ao CRIAR ou REATIVAR (`ativa` false->true) uma ContaFixa, o
sistema gera automaticamente um LANCAMENTO PENDENTE para a ocorrencia atual e
um para a PROXIMA ocorrencia da periodicidade configurada (2 ocorrencias),
vinculado por `conta_fixa_id`. Nao ha sync/job separado na v1 (item 11 e v2)
— a geracao acontece SO nesses dois gatilhos. DECISAO CONFIRMADA COM O
USUARIO EM 2026-07-20 (regra original); periodicidade adicionada em
2026-07-27.

**Periodicidade (revisao de 2026-07-27, DECISAO CONFIRMADA COM O USUARIO).**
Campo `periodicidade` no cadastro, valores suportados: `MENSAL` (padrao) e
`ANUAL`. `SEMANAL` avaliado e descartado nesta rodada — contas fixas
domesticas (aluguel, internet, assinaturas) recorrem em base mensal ou
anual; recorrencia semanal e atipica para o dominio e pode ser adicionada
depois se surgir caso de uso real, mesmo espirito do campo `periodo` de
`limite_gasto` (item 14: "pronto para extensao futura"). Registros
existentes de `conta_fixa` recebem `periodicidade = MENSAL` por default
(migracao aditiva — preserva o comportamento hoje ja implementado, que
sempre foi mensal).

A "proxima ocorrencia" depende da periodicidade: `MENSAL` soma 1 mes a data
de vencimento; `ANUAL` soma 1 ano. A geracao continua criando exatamente 2
lancamentos por vez (ocorrencia atual + proxima), preservando a idempotencia
abaixo — so muda a unidade de tempo somada entre uma ocorrencia e outra.

**Idempotencia (obrigatoria):** antes de gerar o Lancamento de uma
ocorrencia (ano/mes de vencimento) para uma ContaFixa, o sistema verifica se
ja existe um Lancamento com aquele `conta_fixa_id` + mes/ano de vencimento.
Se existir, nao duplica. Rodar a geracao duas vezes para a mesma
ContaFixa/ocorrencia e uma operacao segura (no-op na segunda vez).

**Dia da geracao:** usa `dia_vencimento` da ContaFixa; se o mes tiver menos
dias que esse valor (ex: 31 em abril, ou fevereiro), a data e ajustada para
o ultimo dia do mes — mesmo padrao ja usado por
`FaturaCicloService.CriarDataValida` para o ciclo do cartao.

**Tipo do lancamento gerado:** sempre DEBIT (conta fixa e sempre despesa
recorrente, mesma familia do item 5 "contas a pagar"). Nao existe conta fixa
de recebimento (CREDIT) na v1. Status PENDENTE, `Manual = true`.

**Categoria vinculada.** Cadastro de ContaFixa tem campo `categoria_id` (FK
opcional para `categoria`). Todo Lancamento PENDENTE gerado automaticamente
(criacao ou reativacao) herda essa categoria. Registros existentes sem
categoria: campo fica `null`, sem quebra.

**Edicao propaga para lancamentos PENDENTE ja gerados.** Editar valor,
`dia_vencimento`, `periodicidade` ou `categoria_id` de uma ContaFixa
atualiza os Lancamentos vinculados (`conta_fixa_id`) que ainda estao
`Status = Pendente`. Lancamentos `Status = Pago` NUNCA sao alterados (fato
historico, dinheiro ja saiu — mesmo principio do item 13 "valor_total nunca
muda apos registro"). Mudanca de `periodicidade` em edicao NAO regenera o
par de lancamentos ja existente — `[REVISAR: se o usuario editar
periodicidade de MENSAL pra ANUAL com um lancamento PENDENTE ja gerado pro
mes seguinte (que nao deveria mais existir sob a nova periodicidade), esse
lancamento fica como esta ate ser pago/a conta ser desativada; nenhuma
limpeza automatica do "excesso" de ocorrencias geradas sob a periodicidade
antiga esta definida — confirmar com o usuario se e aceitavel ou se editar
periodicidade deveria disparar uma regeracao]`.

**Desativar cancela os lancamentos PENDENTE ja gerados.** Ao desativar
(`ativa = true -> false`) uma ContaFixa, os Lancamentos vinculados com
`Status = Pendente` sao excluidos (hard delete, mesma regra de exclusao de
lancamento manual do item 5/12). Lancamentos `Status = Pago` permanecem
intocados. Reativar volta a gerar as 2 ocorrencias (atual + proxima,
conforme periodicidade) do zero, respeitando a idempotencia acima.
DECISOES CONFIRMADAS COM O USUARIO EM 2026-07-20 (regra original) e
2026-07-27 (periodicidade).

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
  RENDA_VARIAVEL), `instituicao` (texto livre, ex: "Nubank"), `quantidade`
  (total de unidades/cotas em carteira, soma de todos os aportes),
  `valor_investido` (soma monetaria de todos os aportes, NUNCA editado
  diretamente apos o cadastro — so muda por novo aporte, ver 8.1),
  `data_compra` (data do primeiro aporte), `valor_atual`.

**Decisao de 2026-07-12 (substitui a decisao de 2026-07-06, ver "Escopo: v1
vs v2"):** o modulo anterior de ativo por ticker (compra/venda, preco medio,
cotacao via Brapi sob demanda) foi REMOVIDO do codigo. Investimento detalhado
na v1 NAO tem conexao com nenhuma API de bolsa/cotacao, em nenhuma fase.
`valor_atual` e 100% manual. **Revisao de 2026-07-27 (ver 8.1 abaixo):** o
conceito de `quantidade` e preco medio VOLTA ao modelo, mas sem nenhuma
cotacao externa — e so aritmetica sobre o que o usuario digita em cada
aporte, nao tem relacao com o modelo por ticker+Brapi removido em 2026-07-12.

### 8.1 Aportes e preco medio (DECISAO CONFIRMADA COM O USUARIO EM 2026-07-27)

Comprar mais de um ativo que ja existe na carteira NUNCA sobrescreve o
registro. Toda compra — inclusive a primeira, no cadastro do Ativo — e um
APORTE: gera um registro imutavel em `ativo_aporte` com `data`, `quantidade`
e `preco_unitario` daquele aporte especifico. Cadastrar um Ativo novo E, na
pratica, registrar o primeiro aporte dele (o formulario de cadastro passa a
pedir `quantidade` + `preco_unitario` em vez de `valor_investido` direto).

Preco medio, recalculado a cada aporte por MEDIA PONDERADA:

```
preco_medio_novo = (preco_medio_atual * qtd_atual + preco_aporte * qtd_aporte)
                    / (qtd_atual + qtd_aporte)
```

`preco_medio` e CALCULADO sob demanda (`valor_investido / quantidade`),
NUNCA armazenado como campo proprio — mesmo espirito de `evolucao_percentual`
(8.2). `Ativo.quantidade` e `Ativo.valor_investido` SAO armazenados e
incrementados a cada aporte (nao recalculados varrendo o historico inteiro a
cada leitura), e permanecem sempre consistentes com essa formula.

Aporte individual e registro historico IMUTAVEL — sem edicao nem exclusao
isolada (mesmo principio de fato historico que nao muda depois de registrado,
ja usado em `lancamento.valor`/item 13 "valor_total nunca muda apos
registro"). Historico completo de aportes fica consultavel por ativo — base
do grafico de aportes por ativo (tela "Investimentos").

### 8.2 Valor atual e evolucao (100% manual)

`valor_atual` e definido pelo usuario — mesmo espirito do `saldo_manual` de
conta manual (item 10), so que no nivel do ativo em vez da conta inteira. No
cadastro, `valor_atual` nasce IGUAL a `valor_investido` do primeiro aporte
(evolucao = 0). So muda quando o usuario edita explicitamente — independente
de aportes: aportar mais NAO altera `valor_atual` automaticamente, e o
usuario quem atualiza.

```
evolucao_percentual = (valor_atual - valor_investido) / valor_investido
```

Calculada sob demanda para exibicao, NUNCA armazenada.

### 8.3 Exclusao de ativo

Soft-delete (`ativa = false`), mesmo padrao ja usado no resto do dominio
(`conta.ativa`, `categoria.arquivada`, `lancamento.oculto` — item 4). Sem
hard-delete. Desativar o Ativo NAO apaga o historico de aportes (mesmo
principio de fato historico imutavel do item 8.1).

### 8.4 Rendimento (dividendo e valorizacao) — DECISAO CONFIRMADA COM O USUARIO EM 2026-07-27

Rendimento e SEMPRE vinculado a um Ativo especifico — nunca solto/agregado
sem ativo. Dois tipos, com origem MUITO diferente:

- **DIVIDENDO:** cadastro MANUAL pelo usuario (form: `valor` + `data`,
  vinculado ao ativo). E o unico tipo que o usuario lanca diretamente.
- **VALORIZACAO:** NUNCA lancado manualmente — e derivado AUTOMATICAMENTE
  pelo sistema, conforme os gatilhos abaixo.

**Entidade `rendimento`:** `ativo_id` (FK, not null), `tipo` (`DIVIDENDO` |
`VALORIZACAO`), `origem` (`MANUAL` | `AUTOMATICO`), `valor` (sempre > 0 se
`DIVIDENDO`; pode ser negativo se `VALORIZACAO` — desvalorizacao, mesmo
espirito de `evolucao_percentual`, item 8.2, que ja aceita negativo),
`data`.

**Criacao de DIVIDENDO (manual):** valida `valor > 0` e `ativo` existente E
`ativa = true`. `origem = MANUAL`.

**Criacao AUTOMATICA de VALORIZACAO — gatilho (b): edicao de `valor_atual`
(item 8.2).** Ao atualizar `valor_atual` de `V_anterior` para `V_novo`, o
sistema cria `Rendimento(tipo=VALORIZACAO, origem=AUTOMATICO, valor=V_novo
- V_anterior, data=hoje)`. Se `V_novo == V_anterior`, NENHUM registro e
criado (delta zero nao e evento). O valor pode ser negativo — nao ha piso
em zero, desvalorizacao e informacao real, nao erro. Este mecanismo
TAMBEM resolve a pendencia historica de falta de tabela de historico de
`valor_atual` (ver "Pendencias a definir") — cada edicao vira um registro
datado e auditavel.

**Criacao AUTOMATICA de VALORIZACAO — gatilho (a): novo APORTE (item
8.1) — `[REVISAR: pendente de confirmacao do usuario]`.** A regra 8.2 e
explicita: "aportar mais NAO altera `valor_atual` automaticamente". Logo,
um aporte isolado, por definicao, nao move `valor_atual` — e a unica
formula honesta de VALORIZACAO e um delta de `valor_atual` (gatilho b).
Gerar um registro de R$0,00 a cada aporte e ruido sem informacao; calcular
uma "desvalorizacao por diluicao" (porque `valor_investido` sobe e
`evolucao_percentual` cai) e matematicamente incoerente e economicamente
falso — aportar capital novo nao e perda, e mostrar isso como
desvalorizacao enganaria o usuario no grafico. RECOMENDACAO (nao decisao
final): o gatilho (a) NAO gera nenhum registro de `Rendimento` — o aporte
ja fica auditado por `ativo_aporte` (item 8.1), nenhuma informacao se
perde. Fica como NO-OP explicito ate confirmacao do usuario.

**Rendimento NAO entra em nenhum calculo de saldo.** Nao entra em
`saldo_projetado` (item 9), nem em saldo de Conta (item 10), nem em
`gasto_realizado_no_mes` (item 14). E puramente informativo — grafico/
widget do dashboard (rendimento por tipo) e tela "Investimentos". Sem
relacao com `Lancamento`/fluxo de caixa.

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

### Estorno de compra parcelada — decisao registrada em 2026-07-20

Regra estava omissa (DEMANDA-006, Killua sinalizou) e foi decidida agora,
complementando o estorno de compra a vista ja existente ("Estorno: compra
negativa dentro do cartao", acima) e a regra de Parcelamento (subsecao
anterior).

**Acao unica sobre a compra inteira.** Estornar uma compra parcelada e UMA
UNICA acao disparada sobre `compra_parcelada_id` — nao existe estorno de uma
parcela especifica isolada (ex: so a parcela 5 de 10, mantendo as demais
intactas). Nao e escolha parcela-por-parcela do usuario.

**Cancela todas as parcelas restantes ainda nao pagas.** Essa acao unica
cancela TODAS as parcelas que ainda restam da compra (as N-k que faltam),
nunca so a proxima parcela isolada.

**Alcanca retroativamente parcelas ja pagas.** O estorno NAO fica restrito a
parcelas em fatura ABERTA/FECHADA. Ele tambem alcanca parcelas cuja fatura
ja esta PAGA (dinheiro ja saiu): o estorno gera um lancamento de estorno
(compra negativa, mesma mecanica ja descrita para estorno de compra a
vista) dentro dessa fatura ja paga, em vez de apenas remover/anular
lancamentos futuros ainda nao vencidos.

**Relacao com o pagamento parcial (mesmo item 12):** o pagamento ja fechou
o SALDO da fatura como um todo, nunca uma compra especifica — por isso
estornar uma parcela de uma fatura ja paga NAO desfaz o pagamento em si (o
pagamento permanece registrado como esta). O que muda e o total de compras
da fatura, que diminui com o lancamento de estorno; como os pagamentos ja
cobriam o total anterior, o saldo pendente dessa fatura (total das compras
menos pagamentos) deixa de ser zero e passa a NEGATIVO — um credito em
favor do usuario relativo a essa fatura.

**Credito abate a proxima fatura — decidido em 2026-07-20.** A fatura
estornada MANTEM o status PAGA (nao existe estado "PAGA com credito"). O
saldo negativo gerado pelo estorno retroativo e automaticamente descontado
do total da PROXIMA fatura em aberto do mesmo cartao, reduzindo o valor que
o usuario precisa pagar naquele ciclo seguinte. Nao ha acao manual do
usuario nem mudanca de status na fatura ja paga.

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

## 14. Limite de Gasto por Categoria

Alerta de orcamento por categoria. NENHUM bloqueio de lancamento.

**O que e:** o usuario define um `valor_limite` mensal para uma categoria de
tipo DESPESA (`limite_gasto`, 1:1 com `categoria` — uma categoria tem no
maximo um limite). Categoria tipo RECEITA nao pode ter limite; o conceito e
orcamento de gasto, nao de entrada.

**Hierarquia (categoria/subcategoria, item 7):** o gasto de uma subcategoria
soma TAMBEM no limite da categoria-pai, alem de poder ter seu proprio limite
independente. Ou seja, se a categoria-pai tem limite cadastrado, o gasto
realizado dela e a soma dos lancamentos da propria categoria MAIS os
lancamentos de todas as suas subcategorias diretas — mesmo que uma
subcategoria tambem tenha limite proprio (os dois calculos coexistem, cada
um comparado ao seu proprio `valor_limite`).

**Gasto realizado no periodo:** soma de todos os lancamentos DEBIT
vinculados a categoria (e suas subcategorias diretas, se for categoria-pai),
dentro do mes calendario (mesmo recorte usado na projecao do mes, item 9).
Conta tanto lancamento avulso (conta banco) quanto compra de cartao de
credito daquela categoria — regime de COMPETENCIA: o lancamento conta assim
que registrado na categoria, independente do status (PENDENTE ou PAGO),
mesma filosofia do item 12 (a compra de cartao conta na hora, nao so quando
a fatura e paga). Lancamento oculto (item 4) nao entra na soma.

```
gasto_realizado_no_mes(categoria) = soma(lancamento.valor)
  onde lancamento.categoria_id IN (categoria.id, subcategorias_diretas(categoria).id)
    e lancamento.tipo = DEBIT
    e lancamento.oculto = false
    e lancamento.data dentro do mes calendario consultado
```

**Estourar o limite:** `gasto_realizado_no_mes > valor_limite`.

**Efeito: SOMENTE alerta visual.** O lancamento e salvo normalmente, sem
bloqueio, mesmo ultrapassando o limite. A categoria so fica sinalizada como
estourada nas superficies abaixo — nenhum fluxo de escrita (criar
lancamento, compra de cartao) e impedido por causa do limite.

**Onde aparece:**
- Dashboard/resumo geral: indicador de progresso gasto/limite por categoria.
- Tela de lancamento: aviso ao selecionar/criar lancamento numa categoria
  perto ou acima do limite.
- Relatorio por categoria: comparativo limite vs. realizado.
- Tela de categoria: cadastro/edicao do `valor_limite` fica embutido no
  form/lista de categoria (nao ha tela separada de "Limites").

**Periodo:** so MENSAL nesta versao (`periodo` default `'MENSAL'`, campo
pronto para extensao futura — nenhuma outra opcao de periodo esta
implementada).

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
  (ver item 8 — aporte com preco medio calculado localmente, sem cotacao
  externa, ver 8.1). Sem sync (item 11), sem exclusao/conciliacao
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
  - Ativo standalone (item 8): nome, tipo, instituicao, quantidade, valor
    investido (soma dos aportes), data da compra, valor atual (editavel
    manualmente pelo usuario).
  - Listar, criar (= registrar o primeiro aporte), registrar novo aporte
    (media ponderada, item 8.1), consultar historico de aportes, atualizar
    valor atual, desativar.
  - Resumo por tipo (renda fixa vs renda variavel) para a tela
    "Investimentos".
  - **Rendimento vinculado a Ativo (item 8.4, decisao de 2026-07-27):**
    dividendo cadastrado manualmente pelo usuario e valorizacao derivada
    automaticamente da edicao de `valor_atual`. Puramente informativo
    (grafico/widget), sem entrar em `saldo_projetado` nem em saldo de
    conta. SEM cotacao externa em nenhum ponto — mesma restricao do resto
    do modulo (gatilho automatico via aporte segue `[REVISAR]`, ver 8.4).
- **v2 (fora por enquanto):**
  - Qualquer cotacao via API externa (Brapi ou outra), em qualquer
    modalidade (sob demanda ou automatica).
  - Rentabilidade/serie historica automatica, sparkline com base em
    snapshots de valor (nao ha tabela de historico na v1 — ver "Pendencias a
    definir").

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
  — **RESOLVIDO PARCIALMENTE em 2026-07-27 pelo item 8.4.** Toda edicao de
  `valor_atual` agora gera `Rendimento(VALORIZACAO)` com delta e data — o
  historico que faltava passa a existir a partir dai. Ressalva: a serie so
  comeca a crescer a partir do primeiro registro gerado apos a
  implementacao (TASK-155 em diante); edicoes de `valor_atual` anteriores
  a essa data nao sao reconstruidas retroativamente. O historico de
  APORTES (item 8.1) continua a parte, alimentando a serie de aportes.
  Gatilho de VALORIZACAO por APORTE (item 8.4, gatilho a) segue
  `[REVISAR]` — pendente de confirmacao do usuario, nao afeta o gatilho
  por edicao de `valor_atual`, que ja esta fechado.
