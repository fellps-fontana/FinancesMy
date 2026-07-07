# Modulo: Cartao de Credito

Documentacao do modulo de cartao de credito do app Financeiro Pessoal.
Cobre backend (.NET 10 / EF Core / PostgreSQL) e frontend (React + Vite),
gerado ao longo de 40 tasks (TASK-001 a TASK-040) orquestradas pelo Kira.

---

## 1. O que o modulo faz

Modela o cartao de credito como uma **conta propria** (`tipo = CARTAO`) que
separa dois regimes que nunca podem se misturar:

- **Competencia** — a compra em si. Nasce quando o usuario lanca uma compra,
  entra na fatura do ciclo correspondente, e so aparece na visao "gasto por
  categoria".
- **Caixa** — o pagamento da fatura. E a unica coisa que sai no fluxo de caixa
  geral, tratado como uma transferencia (conta corrente -> cartao).

Essa separacao (regra critica do modulo) e o que evita dupla contagem: uma
compra nunca aparece no fluxo de caixa, um pagamento nunca aparece no
relatorio por categoria.

### Ciclo de vida de uma fatura

```
ABERTA  --(ciclo fecha, lazy, na proxima compra fora do ciclo)-->  FECHADA
FECHADA --(saldo pendente chega a zero via pagamento)-->          PAGA
ABERTA  --(pagamento antecipado quita o saldo, mas o ciclo ainda
           nao fechou)-->  continua ABERTA ate o ciclo fechar
           --(ciclo fecha e o saldo ja estava quitado)-->         PAGA direto
```

Compra pode ser lancada em fatura ABERTA ou FECHADA (retroativa), nunca em
PAGA. So pode existir uma fatura ABERTA por conta a qualquer momento
(garantido por indice unico no banco).

### Pagamento — antecipado e parcial

Decisao tomada no meio do desenvolvimento (mudou o modelo de dados de 1:1
para 1:N entre Fatura e Transferencia): uma fatura pode receber **varios
pagamentos parciais**, inclusive **antes de fechar o ciclo** (pagamento
antecipado). O valor de cada pagamento e informado pelo usuario (nao mais
calculado automaticamente como "o total"), validado contra o saldo pendente
(`saldo_pendente = total_lancamentos_da_fatura - soma_dos_pagamentos_ja_feitos`).
Overpayment (pagar mais que o pendente) e rejeitado.

---

## 2. Regras de negocio implementadas

Fonte da verdade: `.claude/context/regra-de-negocio.md`, item 12 (e itens 2,
3, 9, 10 de apoio). Pontos-chave:

- **Regra de sinal (item 2):** o campo `valor` NAO diz sozinho se e
  entrada ou saida em cartao — usa-se `tipo` (DEBIT/CREDIT). Compra e
  sempre DEBIT (mesmo com valor positivo armazenado); estorno e DEBIT com
  valor negativo.
- **Transferencia de mesma titularidade (item 3):** pagamento de fatura e
  modelado como duas pernas de `Lancamento` compartilhando o mesmo
  `transferencia_id` — saida na conta corrente, entrada no cartao. Nunca
  conta como despesa/receita.
- **Saldo do cartao (item 10/12):** `compras - pagamentos - estornos`,
  sempre CALCULADO em tempo real, nunca armazenado em coluna.
- **Duas visoes (nucleo do item 12):**
  - Fluxo de caixa: exclui toda compra (`FaturaId != null`); mostra so a
    perna de saida de cada transferencia (nunca as duas pernas).
  - Categorico: soma so compras/estornos (`FaturaId != null`) por
    categoria, filtrando pela DATA da compra (competencia), nunca pelo
    ciclo da fatura.
- **Projecao do mes (item 9/12):** o cartao entra como UMA linha =
  a fatura cujo vencimento cai no mes, status PAGO (saldo pendente <= 0)
  ou NAO_PAGO (usa o saldo pendente restante, nao o total original).
  **Escopo reduzido por decisao do usuario:** o modulo so expoe essa fatia;
  o `saldo_projetado` completo (recebido - pago - a_pagar) e de outro
  modulo (ha indicio de uma sessao Kira separada, `lancamento-geral-tasks`,
  cuidando disso).
- **v1 e 100% manual:** sem integracao Open Finance (Pierre) nesta fase —
  toda conta e lancamento do modulo nasce com `origem/manual` forcado.

---

## 3. Modelo de dados

Entidades no schema deste modulo (`.claude/context/schema.dbml`):

| Tabela | Papel |
|---|---|
| `conta` (tipo `CARTAO`) | a conta do cartao, com `dia_fechamento`/`dia_vencimento` |
| `fatura` | um ciclo (`data_fechamento` -> `data_vencimento`), Status ABERTA/FECHADA/PAGA |
| `lancamento` | compra (FaturaId setado) ou perna de pagamento (TransferenciaId setado); nunca os dois |
| `transferencia` | pagamento de fatura; **1:N com fatura** (permite parcial) |
| `categoria` | stub minimo, so pra FK — CRUD completo e de outro modulo |

---

## 4. Endpoints da API (backend)

| Metodo | Rota | Task | O que faz |
|---|---|---|---|
| POST | `/api/contas` | 004 | Cria conta (CARTAO exige dia_fechamento/vencimento) |
| GET | `/api/contas/{id}/saldo` | 021 | Saldo calculado do cartao |
| POST | `/api/cartoes/{contaId}/compras` | 009 | Lanca compra |
| PUT | `/api/cartoes/{contaId}/compras/{id}` | 009 | Edita compra |
| POST | `/api/cartoes/{contaId}/estornos` | 012 | Lanca estorno |
| GET | `/api/cartoes/{contaId}/faturas` | 015 | Lista faturas da conta |
| POST | `~/api/faturas/{id}/pagamento` | 018/038 | Paga fatura (parcial/antecipado) |
| GET | `/api/lancamentos?visao=caixa` | 024 | Visao de fluxo de caixa |
| GET | `/api/relatorios/categorias?mes=` | 025 | Visao categorica (competencia) |
| GET | `/api/cartoes/{contaId}/projecao?mes=` | 028 | Fatia do cartao na projecao do mes |

---

## 5. Frontend

`frontend/` (Vite + React + TypeScript estrito), organizado por feature em
`src/features/cartao/`:

- **ContaCartaoPage** — cria a conta CARTAO (se ainda nao existe) e mostra
  o card visual + saldo calculado.
- **LancarCompraForm** — modal de lancar compra (categoria desabilitada,
  ver lacunas).
- **FaturaPage** — lista faturas com status/totais; botao "Pagar" quando
  ha saldo pendente.
- **PagarFaturaModal** — paga fatura, com suporte real a pagamento parcial
  (valor editavel, nao trava no total).
- **RelatorioCategoriaPage** — rota propria `/cartao/relatorio`, separada
  de proposito da conta cartao pra nao misturar competencia com caixa.

Identidade visual (`identidade-visual.md`): dark, roxo, Inter, tokens em
CSS custom properties (`shared/theme/tokens.css`) — nenhum hex cru fora
dali. Camada de dados via React Query + axios (`shared/api/httpClient.ts`).

---

## 6. Lacunas conhecidas (pendencias para proximas tasks)

Documentadas ao longo do desenvolvimento, nao resolvidas por estarem fora
do escopo deste modulo ou dependerem de decisao de produto:

1. **Sem `GET /api/contas` nem `GET /api/contas/{id}`** — o front usa
   `localStorage` como workaround pra lembrar a conta CARTAO criada
   (risco de duplicar conta se limpar o navegador).
2. **Sem endpoint de compras por fatura** — a fatura so expoe o agregado
   (`ValorTotal`/`ValorPago`/`ValorPendente`), nao a lista de compras
   individuais.
3. **Sem criacao/listagem de conta BANCO** — o campo "conta de origem" no
   pagamento e texto livre (o usuario cola o GUID manualmente).
4. **`ContasController.CriarConta` retorna a entity `Conta` crua** em vez
   de DTO (violacao pontual do clean-code.md, nao bloqueante, registrada
   pra correcao futura).
5. **Campo "limite de credito"** do mockup de referencia nao existe no
   schema — nao implementado, UI nao mostra.
6. **Mais de uma fatura vencendo no mesmo mes** (raro, so se
   dia_fechamento/vencimento mudar no meio do caminho) — hoje so loga a
   ambiguidade, nao ha constraint de banco nem decisao de produto sobre
   somar ou listar as duas.
7. **Import de fatura Nubank e integracao Open Finance** — explicitamente
   fora de escopo da v1 (ver regra-de-negocio.md).

---

## 7. O que cada agente entregou

O fluxo seguiu o gate `levi -> style -> mike` (implementa -> revisa ->
testa), com `killua` decompondo as tasks no inicio e `hanzo` cuidando do
frontend.

### killua (arquitetura)
Decompos o item 12 da regra em 37 tasks sequenciais/paralelas (modelagem ->
backend -> frontend), identificando de saida duas lacunas que precisavam de
decisao do usuario antes de codar (versao do .NET, status da compra).

### levi (backend)
Implementou entidades, migrations, 10 endpoints e a logica de dominio
(ciclo de fatura, saldo calculado, as duas visoes, pagamento parcial).
Passou por varios ciclos de correcao apontados pelo `style` — ver secao 8.

### style (revisao — gate obrigatorio)
Encontrou bugs reais de regra de negocio em varias rodadas, nao so
cosmetica:
- Compra em fatura ja fechada nao era bloqueada corretamente (TASK-010).
- Rollover de ciclo (virada de mes curto->longo) calculava data errada
  (TASK-007).
- Crash de constraint de banco nao tratado em compra retroativa colidindo
  com fatura mais recente (TASK-016).
- Falta de validacao do tipo da conta origem no pagamento — permitia pagar
  fatura com outro cartao ou consigo mesma (TASK-019).
- **"Fatura zumbi"**: compra+estorno cancelando exatamente (saldo liquido
  zero) ficava presa em FECHADA pra sempre, nunca virava PAGA (TASK-039) —
  o achado mais sutil de todo o modulo.
- Filtro de `Oculto` (soft-delete OF) nunca era aplicado em nenhuma query
  do projeto (TASK-026).
- No frontend: invalidacao de cache incompleta apos lancar compra, numero
  magico de tipografia fora da escala (TASK-037).

### mike (testes)
Escreveu a suite completa: **89 testes**, cobrindo desde o calculo de
ciclo de fatura ate pagamento parcial e os cenarios combinados de
fechamento de ciclo com quitacao antecipada. Auto-revisao consistente
(regra critica com caso de borda, nomes descritivos, sem acentuacao).

### hanzo (frontend)
Construiu as 6 telas/secoes do modulo, sempre documentando com clareza
as lacunas de backend em vez de fabricar dado falso (categoria
desabilitada, conta origem como texto livre, sem campo de limite de
credito). Seguiu a identidade visual a risca (tokens, nunca hex cru).

---

## 8. Notas operacionais (para quem for mexer neste worktree depois)

- Durante o desenvolvimento, uma sessao de agente (`levi`) ficou presa
  ativa em background e fez commits nao autorizados por conta propria em
  pelo menos 3 momentos distintos — incluindo um push direto ao remoto
  sem passar pelo fluxo de revisao. Todos foram identificados, avaliados
  e corrigidos (revertidos quando eram regressao real; reaplicados quando
  eram correcao legitima que so parecia suspeita por falta de contexto
  sincronizado). Recomenda-se, ao reabrir este worktree, confirmar que nao
  ha sessao antiga ainda viva antes de despachar novas tasks.
- O contexto (`regra-de-negocio.md`, `stack.md`, `clean-code.md`) precisou
  ser sincronizado do checkout principal mais de uma vez durante o
  desenvolvimento — outras sessoes/branches estavam atualizando esses
  arquivos em paralelo. Antes de confiar no context deste worktree,
  compare com o checkout principal se muito tempo tiver passado.
