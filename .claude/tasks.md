# Tasks — Modulo Lancamento Geral (v1)

Gerado por killua a partir de `regra-de-negocio.md` itens 1, 2 (CRITICA), 3, 4
e `schema.dbml` (tabelas conta, categoria, lancamento, transferencia, fatura —
todas ja existentes no codigo, herdadas da branch cartao-credito-tasks).

Escopo desta lista: a camada GERAL de servico sobre entidades que ja existem.
NAO redesenha Conta/Categoria/Lancamento/Transferencia/Fatura. NAO inclui UI
(hanzo) — o dispatch restringe explicitamente este modulo a endpoints
backend que cartao, investimentos e conta-corrente/sync vao consumir.
NAO duplica a logica de compra/estorno/fatura de cartao (ja fechada no
modulo cartao-credito-tasks).

## Decisoes de modelagem (Killua)

- **Classificacao de lancamento vira funcao pura e nomeada**, sem acesso a
  banco: `ClassificacaoLancamentoService.Classificar(Lancamento)` retornando
  um enum `ClassificacaoLancamento { Entrada, Saida, Transferencia,
  CompetenciaCartao }`. Regra (item 2, CRITICA):
  ```
  se lancamento.TransferenciaId != null  -> Transferencia   (cobre pagamento de fatura)
  senao se lancamento.FaturaId != null   -> CompetenciaCartao (compra/estorno cartao)
  senao (Tipo == DEBIT ? Saida : Entrada)
  ```
  Nunca le `Valor`. Esta funcao e a peca que os modulos cartao (saldo
  calculado, fluxo de caixa, relatorio categorico — TASK-021/024/025 la) e
  sync vao reaproveitar. Construida e testada aqui, isolada, para nao virar
  logica duplicada/inconsistente em cada modulo consumidor.
- **Valor de lancamento manual sempre magnitude positiva.** `Tipo`
  (DEBIT|CREDIT) e a UNICA fonte de sinal — nunca inferido do valor. Isso e
  diferente da convencao interna do modulo cartao (estorno usa valor
  negativo dentro do proprio DEBIT), que e um caso fechado e fora de escopo
  aqui.
- **Transferencia manual exige as DUAS contas com `Origem = MANUAL`.** Conta
  Open Finance e dado imutavel (item 1, "o sistema apenas le, nunca edita")
  — criar uma perna de Lancamento sintetica dentro dela conflitaria com essa
  regra. Ver duvida em aberto sobre pagamento de fatura partindo de conta OF.
- **Exclusao de lancamento manual = HARD DELETE**, diferente do soft-delete
  do item 4 (que e exclusivo de Open Finance, cujo racional explicito e
  "nao ser reimportado pelo sync" — nao se aplica a lancamento manual, que
  nao tem fonte externa para reimportar). Bloqueia delete se
  `TransferenciaId`, `FaturaId` ou `ConciliadoCom` estiverem preenchidos
  (protege integridade de dados de outros modulos que apontam pra essa
  linha).
- **Status aceito na criacao/edicao manual: PENDENTE ou PAGO apenas.**
  `SUGERIDO` (item 5) e exclusivo da maquina de conciliacao automatica —
  setar isso manualmente quebraria a invariante de que SUGERIDO sempre
  vem acompanhado de uma proposta de vinculo do sync.
- **GET de lancamento (listagem) e cru, sem "visao".** Retorna os campos da
  entidade, filtravel por `contaId`/`manual`/`status`. NAO aplica a
  classificacao CAIXA/COMPETENCIA (isso e relatorio, tarefado nos modulos
  consumidores — cartao TASK-024/025). Este modulo so fornece o dado e a
  funcao de classificacao; quem monta a "visao" e outro modulo.
- **Contrato para o modulo de sync (fora de escopo aqui):** antes de
  inserir um lancamento por `pierre_txn_id`, o sync deve checar se ja existe
  um lancamento com esse `pierre_txn_id` e `Oculto = true` — se existir, NAO
  reinserir. Este modulo garante o soft-delete e o campo; a checagem no
  fluxo de sync e responsabilidade de outro modulo.

Fora de escopo v1, nao tarefado: UI (hanzo), cancelamento/estorno de
transferencia, endpoint de "desocultar", checagem de reimport no sync.

Formato: STATUS (PENDENTE | CONCLUIDA | BLOQUEADA).

---

### TASK-001 — Servico de classificacao de lancamento (regra de sinal, item 2 CRITICA)
STATUS: PENDENTE
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: nenhuma
CONTEXTO A LER: regra-de-negocio.md item 2 (regra de sinal, CRITICA), item 3 (transferencia), item 12 (paragrafo "Duas visoes"); clean-code.md secao "Organizacao (.NET)" (regra de negocio em funcao nomeada e testavel)
ESCOPO: criar `ClassificacaoLancamentoService` com metodo puro `Classificar(Lancamento lancamento)` retornando enum `ClassificacaoLancamento { Entrada, Saida, Transferencia, CompetenciaCartao }`, usando SEMPRE `Tipo` (DEBIT/CREDIT) + os vinculos `TransferenciaId`/`FaturaId` — nunca o sinal cru de `Valor`. `TransferenciaId` preenchido classifica como Transferencia independente do `Tipo` (cobre a excecao de pagamento de fatura citada no item 2). `FaturaId` preenchido classifica como CompetenciaCartao (nunca entra no fluxo de caixa geral, item 12).
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Services/ClassificacaoLancamentoService.cs (novo), MyFinances/MyFinances/Domain/ClassificacaoLancamento.cs (novo, enum)
NAO FAZER: nao ler ou somar `Valor` para decidir sinal; nao implementar aqui os endpoints de fluxo de caixa/saldo/relatorio que VAO CONSUMIR esta funcao (pertencem a outros modulos, ja tarefados no worktree cartao-credito-tasks); nao alterar a convencao de valor negativo do estorno de cartao (modulo ja fechado, fora de escopo).
RETORNO ESPERADO: funcao pura, sem dependencia de `AppDbContext`, testavel isoladamente, cobrindo os 4 casos de classificacao.

### TASK-002 — Revisao TASK-001
STATUS: PENDENTE
AGENT: style
FLUXO: Implementacao
DEPENDENCIAS: TASK-001
CONTEXTO A LER: regra-de-negocio.md item 2 (CRITICA)
ESCOPO: confirmar que a classificacao nunca le o sinal de `Valor`, que transferencia (incluindo pagamento de fatura) nunca cai em Entrada/Saida, e que compra/estorno de cartao (FaturaId preenchido) nunca aparece como Entrada/Saida.
ARQUIVOS PERMITIDOS: os mesmos do TASK-001 (leitura + veredito)
NAO FAZER: nao editar codigo — devolver tarefa de correcao no esquema padrao se reprovar.
RETORNO ESPERADO: veredito (APROVADO | PRECISA CORRIGIR) + tarefa de correcao se reprovado.

### TASK-003 — Testes TASK-001
STATUS: PENDENTE
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-002 (aprovado)
CONTEXTO A LER: regra-de-negocio.md item 2
ESCOPO: cobrir os 4 casos (DEBIT sem vinculo = Saida; CREDIT sem vinculo = Entrada; TransferenciaId preenchido = Transferencia independente do Tipo; FaturaId preenchido = CompetenciaCartao); incluir caso de valor com sinal "errado" (ex.: CREDIT com Valor negativo) provando que o sinal cru e ignorado.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances.Tests/ClassificacaoLancamentoServiceTests.cs (novo)
NAO FAZER: nao alterar o servico pra fazer o teste passar sem reportar — bug de codigo volta pro levi.
RETORNO ESPERADO: testes passando; relatorio estruturado se falhar por bug de codigo.

### TASK-004 — Servico + endpoint CRUD de lancamento manual
STATUS: PENDENTE
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-001
CONTEXTO A LER: regra-de-negocio.md item 1 (conta manual e fonte da verdade; OF imutavel), item 2 (regra de sinal), item 5 (paragrafo "Conta de pagamento manual", para os status aceitos)
ESCOPO: `LancamentoManualService` com Criar/Editar/Listar/Excluir para lancamento em conta `Origem=MANUAL`; `LancamentosController` com POST/PUT/GET/DELETE em `/api/lancamentos`. Criar/editar exige `Tipo` (DEBIT|CREDIT) explicito no request (nunca inferido do sinal de `Valor`, sempre armazenado como magnitude positiva); `Status` aceita apenas PENDENTE ou PAGO. Excluir e HARD DELETE, bloqueado se `TransferenciaId`, `FaturaId` ou `ConciliadoCom` estiverem preenchidos.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Services/LancamentoManualService.cs (novo), MyFinances/MyFinances/Controllers/LancamentosController.cs (novo), MyFinances/MyFinances/Dtos/CriarLancamentoRequest.cs (novo), MyFinances/MyFinances/Dtos/EditarLancamentoRequest.cs (novo)
NAO FAZER: nao permitir criar/editar lancamento em conta com `Origem=OPEN_FINANCE` (item 1); nao permitir setar `Status=SUGERIDO` nem preencher `ConciliadoCom`/`TransferenciaId`/`FaturaId` via este endpoint (geridos por outros fluxos); nao permitir mover lancamento entre contas na edicao.
RETORNO ESPERADO: contrato de API dos 4 endpoints (rota, verbo, body, retorno) + servico testavel isoladamente.

### TASK-005 — Revisao TASK-004
STATUS: PENDENTE
AGENT: style
FLUXO: Implementacao
DEPENDENCIAS: TASK-004
CONTEXTO A LER: regra-de-negocio.md itens 1, 2, 5
ESCOPO: confirmar bloqueio de conta OPEN_FINANCE, validacao de `Tipo` explicito (nunca sinal cru), bloqueio de delete quando ha vinculo (transferencia/fatura/conciliacao), e que `Status` nunca aceita SUGERIDO via este endpoint.
ARQUIVOS PERMITIDOS: os do TASK-004
RETORNO ESPERADO: veredito + tarefa de correcao se reprovado.

### TASK-006 — Testes TASK-004
STATUS: PENDENTE
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-005 (aprovado)
CONTEXTO A LER: regra-de-negocio.md itens 1, 2, 5
ESCOPO: cobrir criar/editar/listar/excluir em conta MANUAL; rejeicao em conta OPEN_FINANCE; rejeicao de `Status=SUGERIDO`; bloqueio de delete com `TransferenciaId`/`FaturaId`/`ConciliadoCom` preenchidos.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances.Tests/LancamentoManualServiceTests.cs (novo)
NAO FAZER: nao alterar servico/controller para o teste passar sem reportar.
RETORNO ESPERADO: testes passando; relatorio se bug de codigo.

### TASK-007 — Servico + endpoint transferencia entre contas manuais
STATUS: PENDENTE
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-001
CONTEXTO A LER: regra-de-negocio.md item 3 (transferencias de mesma titularidade, modelo de duas pernas)
ESCOPO: `TransferenciaService.CriarAsync` cria um registro `Transferencia` + duas `Lancamento` (saida DEBIT na conta origem, entrada CREDIT na conta destino) compartilhando `TransferenciaId`, `Manual=true`, `Status=PAGO`, mesma magnitude de valor nas duas pernas, criadas atomicamente (mesma transacao); `TransferenciasController` com POST `/api/transferencias`. Validar: ambas as contas existem, `Origem=MANUAL` nas duas, contas diferentes entre si, valor > 0.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Services/TransferenciaService.cs (novo), MyFinances/MyFinances/Controllers/TransferenciasController.cs (novo), MyFinances/MyFinances/Dtos/CriarTransferenciaRequest.cs (novo)
NAO FAZER: nao aceitar conta com `Origem=OPEN_FINANCE` em nenhuma perna (ver duvida em aberto sobre pagamento de fatura); nao implementar cancelamento/estorno de transferencia; nao duplicar a logica de pagamento de fatura do modulo cartao (aquele modulo pode reaproveitar este servico quando as duas contas forem MANUAL, mas a decisao e dele).
RETORNO ESPERADO: contrato do endpoint + servico testavel; garantia de atomicidade das duas pernas.

### TASK-008 — Revisao TASK-007
STATUS: PENDENTE
AGENT: style
FLUXO: Implementacao
DEPENDENCIAS: TASK-007
CONTEXTO A LER: regra-de-negocio.md item 3
ESCOPO: confirmar que as duas pernas sempre compartilham `TransferenciaId`, tipos opostos corretos (DEBIT origem / CREDIT destino), valor identico, atomicidade (nunca uma perna sem a outra), e que a classificacao da TASK-001 aplicada a ambas resulta em Transferencia.
ARQUIVOS PERMITIDOS: os do TASK-007
RETORNO ESPERADO: veredito + correcao se reprovado.

### TASK-009 — Testes TASK-007
STATUS: PENDENTE
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-008 (aprovado)
CONTEXTO A LER: regra-de-negocio.md item 3
ESCOPO: cobrir criacao das duas pernas; rejeicao de conta OPEN_FINANCE; rejeicao de conta origem==destino; rejeicao de valor<=0; classificacao das duas pernas resultando em Transferencia (nunca Entrada/Saida).
ARQUIVOS PERMITIDOS: MyFinances/MyFinances.Tests/TransferenciaServiceTests.cs (novo)
RETORNO ESPERADO: testes passando; relatorio se bug de codigo.

### TASK-010 — Servico + endpoint ocultar lancamento Open Finance (soft-delete, item 4)
STATUS: PENDENTE
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-004 (mesmo arquivo Controllers/LancamentosController.cs — serializar)
CONTEXTO A LER: regra-de-negocio.md item 4 (exclusao de lancamento OF)
ESCOPO: `LancamentoOcultacaoService.OcultarAsync` marca `Oculto=true` em lancamento com `Manual=false`, nunca hard-delete; `PATCH /api/lancamentos/{id}/ocultar` no `LancamentosController`. Bloqueia a acao se `Manual=true` (esses usam o DELETE da TASK-004).
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Services/LancamentoOcultacaoService.cs (novo), MyFinances/MyFinances/Controllers/LancamentosController.cs (estender)
NAO FAZER: nao apagar fisicamente a linha em nenhuma hipotese; nao implementar aqui a checagem do sync contra reimport (responsabilidade do modulo de sync, que deve consumir `Oculto`+`PierreTxnId`); nao criar endpoint de "desocultar" (nao solicitado).
RETORNO ESPERADO: contrato do endpoint; garantia de que `Oculto=true` e a UNICA mudanca de estado.

### TASK-011 — Revisao TASK-010
STATUS: PENDENTE
AGENT: style
FLUXO: Implementacao
DEPENDENCIAS: TASK-010
CONTEXTO A LER: regra-de-negocio.md item 4
ESCOPO: confirmar que a acao e sempre soft-delete (nenhum caminho de codigo remove a linha), que so lancamento OF (`Manual=false`) pode ser ocultado, e que nenhum outro campo e alterado.
ARQUIVOS PERMITIDOS: os do TASK-010
RETORNO ESPERADO: veredito + correcao se reprovado.

### TASK-012 — Testes TASK-010
STATUS: PENDENTE
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-011 (aprovado)
CONTEXTO A LER: regra-de-negocio.md item 4
ESCOPO: cobrir ocultar lancamento OF (`Oculto` vira true, linha continua existindo); rejeicao de ocultar lancamento `Manual=true`; nenhum outro campo alterado pela operacao.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances.Tests/LancamentoOcultacaoServiceTests.cs (novo)
RETORNO ESPERADO: testes passando; relatorio se bug de codigo.

---

## Duvida em aberto para o usuario

1. **Delete de lancamento manual (TASK-004).** Assumi HARD DELETE porque o
   racional do soft-delete do item 4 e explicitamente "nao ser reimportado
   pelo sync" — nao existe risco equivalente pra lancamento manual. Mas a
   regra e omissa sobre isso especificamente (so fala de exclusao de OF).
   Se voce quiser auditoria/historico tambem pra lancamento manual excluido,
   isso muda a task (viraria soft-delete generico, nao so pra OF).

2. **Status na criacao manual (PENDENTE|PAGO) e o item 5/6.** Modelei o CRUD
   generico assumindo que "conta a pagar" (item 5) e a MESMA tabela
   `lancamento` com `Status=PENDENTE`, e que este CRUD e o caminho de
   criacao que o futuro modulo de conciliacao/conta-fixa vai usar. Se esse
   modulo (ainda nao arquitetado) precisar de um fluxo/tabela proprios em
   vez de reaproveitar este CRUD generico, a TASK-004 precisa ser revista.

3. **Transferencia envolvendo conta Open Finance.** O pagamento de fatura de
   cartao (item 12) e modelado como transferencia de duas pernas partindo da
   conta corrente. Se essa conta corrente for Open Finance (sincronizada via
   Pierre), criar uma perna de `Lancamento` manual sintetica dentro dela
   conflita com a regra de imutabilidade do item 1 ("o sistema apenas le,
   nunca edita"). Esta lista restringe o endpoint de transferencia a
   MANUAL-MANUAL exatamente para nao decidir isso sozinha. Fica pro modulo
   cartao (`PagamentoFaturaService`, ja tarefado como TASK-018 no worktree
   cartao-credito-tasks) resolver — mas o usuario precisa decidir a regra:
   o pagamento cria uma perna sintetica no lado do banco OF mesmo assim, ou
   o modelo so registra a perna do lado do cartao e deixa o lado do banco
   ser refletido organicamente pelo proprio sync (evitando dado sintetico
   dentro de uma conta que deveria ser somente leitura)?
