# Tasks — Modulo Cartao de Credito (v1)

Gerado por killua a partir de `regra-de-negocio.md` item 12 (+ itens 2, 3, 9, 10)
e `schema.dbml` (tabelas conta, categoria, lancamento, transferencia, fatura).

Decisoes tomadas pelo usuario antes de fechar esta lista (registradas em
`context/stack.md` e `context/regra-de-negocio.md`):
- Stack e .NET 10 (nao .NET 8 — `stack.md` corrigido).
- Compra de cartao nasce sempre com `status = PAGO` (nao passa pela maquina de
  conciliacao do item 5 — clarificado no item 12 da regra-de-negocio.md).

Fora de escopo v1, nao tarefado: import de fatura Nubank e dedup sem
`pierre_txn_id` (item 12 / Pendencias) — fica para v2.

Formato: STATUS (PENDENTE | CONCLUIDA | BLOQUEADA).

---

### TASK-001 — Setup de infraestrutura (EF Core + projeto de testes)
STATUS: CONCLUIDA (EF Core/Npgsql em 10.0.9/10.0.2, alinhado ao net10.0 do csproj; build OK)
AGENT: levi
ESCOPO: Adicionar pacotes EF Core/Npgsql ao projeto, criar `AppDbContext` vazio, configurar connection string e criar o projeto de testes xUnit referenciando o projeto principal.
ARQUIVOS: MyFinances/MyFinances/MyFinances.csproj, MyFinances/MyFinances/Program.cs, MyFinances/MyFinances/appsettings.json, MyFinances/MyFinances/appsettings.Development.json, MyFinances/MyFinances/Data/AppDbContext.cs, MyFinances/MyFinances.Tests/MyFinances.Tests.csproj (novo), MyFinances/MyFinances.sln
DEPENDENCIAS: nenhuma
CONTEXTO A LER: stack.md completo (ORM, banco, convencoes)

### TASK-002 — Entidades EF Core do modulo cartao (Conta, Categoria stub, Lancamento, Transferencia, Fatura)
STATUS: CONCLUIDA (mapping revisado; corrigidos Fatura<->Transferencia para 1:1 e filtro do indice unico de PierreTxnId para sintaxe PostgreSQL)
AGENT: levi
ESCOPO: Criar as classes de entidade e o Fluent API mapping no AppDbContext refletindo exatamente o schema.dbml (tipos, enums como string, nullability, FKs, indice unico em pierre_txn_id quando nao nulo).
ARQUIVOS: MyFinances/MyFinances/Models/Conta.cs, Models/Categoria.cs, Models/Lancamento.cs, Models/Transferencia.cs, Models/Fatura.cs, Data/AppDbContext.cs
DEPENDENCIAS: TASK-001
CONTEXTO A LER: schema.dbml (tabelas conta, categoria, lancamento, transferencia, fatura); regra-de-negocio.md itens 2, 3, 10, 12

### TASK-003 — Migration inicial
STATUS: CONCLUIDA (InitialCreate gerada; indices/FKs conferidos, build OK)
AGENT: levi
ESCOPO: Gerar e aplicar a migration EF Core com as tabelas do TASK-002.
ARQUIVOS: MyFinances/MyFinances/Migrations/
DEPENDENCIAS: TASK-002
CONTEXTO A LER: stack.md (convencao "Migrations versionadas pelo EF")

### TASK-004 — Endpoint criar conta CARTAO
STATUS: CONCLUIDA (POST /api/contas, validacao condicional CARTAO; 1 ciclo de correcao via style — nomes de metodo enganosos + comentario acentuado)
AGENT: levi
ESCOPO: POST /api/contas aceitando tipo CARTAO com validacao condicional obrigando dia_fechamento e dia_vencimento.
ARQUIVOS: MyFinances/MyFinances/Controllers/ContasController.cs, Services/ContaService.cs
DEPENDENCIAS: TASK-003
CONTEXTO A LER: regra-de-negocio.md item 12 (conta CARTAO); schema.dbml tabela conta

### TASK-005 — Revisao TASK-004
STATUS: CONCLUIDA (APROVADO apos correcao; regra de negocio ok desde a 1a versao — pendencia registrada: saldo_manual obrigatorio ou nao na criacao de conta BANCO/INVESTIMENTO, nao decidida)
AGENT: style
ESCOPO: Validar que criacao de conta CARTAO sem os dois campos de ciclo e rejeitada e que saldo_manual nao e aceito para CARTAO (saldo e calculado, item 10).
ARQUIVOS: os mesmos do TASK-004 (leitura + veredito)
DEPENDENCIAS: TASK-004
CONTEXTO A LER: regra-de-negocio.md itens 10, 12

### TASK-006 — Servico de ciclo de fatura (fatura vigente / criacao sob demanda)
STATUS: CONCLUIDA (calculo de ciclo com virada de mes/ano corrigido apos revisao — bug real de rollover sobre data clampada; indice unico parcial no banco adicionado, elimina race condition; FaturaStatusConstants movido p/ Domain/)
AGENT: levi
ESCOPO: Dado uma conta CARTAO e uma data, resolver a fatura ABERTA correspondente ao ciclo (dia_fechamento -> dia_vencimento), criando-a se nao existir.
ARQUIVOS: MyFinances/MyFinances/Services/FaturaCicloService.cs
DEPENDENCIAS: TASK-004
CONTEXTO A LER: regra-de-negocio.md item 12 (paragrafo "Fatura"); schema.dbml tabela fatura

### TASK-007 — Revisao TASK-006
STATUS: CONCLUIDA (PRECISA CORRIGIR -> corrigido: bug de rollover de data, constraint de unicidade, organizacao de constantes, docstring incorreto. Duas lacunas de regra confirmadas pelo usuario e documentadas na regra-de-negocio.md)
AGENT: style
ESCOPO: Checar calculo de datas de ciclo (virada de mes, ano) e que nunca existam duas faturas ABERTA simultaneas para a mesma conta.
ARQUIVOS: os do TASK-006
DEPENDENCIAS: TASK-006
CONTEXTO A LER: regra-de-negocio.md item 12

### TASK-008 — Testes TASK-006
STATUS: CONCLUIDA (11/11 testes passando: ciclo normal, virada mes curto->longo, virada de ano, reaproveitamento, dia exato do fechamento, validacoes de erro)
AGENT: mike
ESCOPO: Cobrir maquina de estado de resolucao/criacao de fatura (ciclo normal, virada de ano, chamada concorrente nao duplicando fatura ABERTA).
ARQUIVOS: MyFinances/MyFinances.Tests/FaturaCicloServiceTests.cs
DEPENDENCIAS: TASK-007
CONTEXTO A LER: regra-de-negocio.md item 12

### TASK-009 — Endpoint criar/editar compra
STATUS: CONCLUIDA (POST/PUT /api/cartoes/{contaId}/compras; FaturaId resolvido pela data da compra via novo metodo ResolverFaturaParaLancamentoAsync — rejeita fatura PAGA, aceita FECHADA retroativa, cria ABERTA se nao existir; regra confirmada pelo usuario e documentada. Testes existentes (TASK-008) continuam passando 11/11)
AGENT: levi
ESCOPO: POST/PUT /api/cartoes/{contaId}/compras cria lancamento com categoria_id, data, valor, tipo=DEBIT, manual=true, status=PAGO (fixo — ver clarificacao no item 12), associado a fatura resolvida pelo TASK-006.
ARQUIVOS: MyFinances/MyFinances/Controllers/CartaoComprasController.cs, Services/CompraCartaoService.cs
DEPENDENCIAS: TASK-006
CONTEXTO A LER: regra-de-negocio.md item 12 (paragrafo "Compra" + clarificacao "Status do lancamento de compra") e item 2 (regra de sinal)

### TASK-010 — Revisao TASK-009
STATUS: CONCLUIDA (APROVADO apos 1 ciclo de correcao — bug critico: edicao de valor/descricao numa compra com fatura ja PAGA passava sem validacao quando a data nao mudava; corrigido para revalidar sempre. Tambem: validacao duplicada unificada, constantes movidas p/ Domain/)
AGENT: style
ESCOPO: Garantir que a compra nunca e marcada como transferencia, nunca entra em endpoint de fluxo de caixa por engano, e que status e sempre PAGO fixo (nunca PENDENTE/SUGERIDO).
ARQUIVOS: os do TASK-009
DEPENDENCIAS: TASK-009
CONTEXTO A LER: regra-de-negocio.md item 12

### TASK-011 — Testes TASK-009
STATUS: CONCLUIDA (30/30 testes: criar/editar compra, fatura PAGA/FECHADA/ABERTA em ambos os fluxos, incluindo regressao do bug corrigido na TASK-010)
AGENT: mike
ESCOPO: Cobrir classificacao da compra (nunca some no fluxo de caixa, sempre soma na visao categorica), vinculo correto de fatura_id e status sempre PAGO.
ARQUIVOS: MyFinances/MyFinances.Tests/CompraCartaoServiceTests.cs
DEPENDENCIAS: TASK-010
CONTEXTO A LER: regra-de-negocio.md item 12

### TASK-012 — Endpoint estorno
STATUS: CONCLUIDA (POST /api/cartoes/{contaId}/estornos, Lancamento com valor negativo, mesma regra de fatura da compra. 1 ciclo de correcao: extraido ValidacaoCartaoService compartilhado para remover acoplamento entre EstornoCartaoService e CompraCartaoService)
AGENT: levi
ESCOPO: POST /api/cartoes/{contaId}/estornos cria lancamento de compra negativa vinculado a fatura correspondente.
ARQUIVOS: MyFinances/MyFinances/Controllers/CartaoComprasController.cs, Services/EstornoCartaoService.cs
DEPENDENCIAS: TASK-009
CONTEXTO A LER: regra-de-negocio.md item 12 (paragrafo "Estorno")

### TASK-013 — Revisao TASK-012
STATUS: CONCLUIDA (APROVADO apos 1 ciclo de correcao — regra de negocio ja estava correta desde a 1a versao; achado foi de design/acoplamento)
AGENT: style
ESCOPO: Confirmar que estorno reduz saldo/fatura corretamente e nao e confundido com pagamento.
ARQUIVOS: os do TASK-012
DEPENDENCIAS: TASK-012
CONTEXTO A LER: regra-de-negocio.md item 12

### TASK-014 — Testes TASK-012
STATUS: CONCLUIDA (12 testes novos, 42/42 total: sinal invertido, validacoes, fatura PAGA/FECHADA/sem-fatura)
AGENT: mike
ESCOPO: Cobrir que estorno reduz o saldo do cartao e a soma categorica corretamente, sem afetar fluxo de caixa.
ARQUIVOS: MyFinances/MyFinances.Tests/EstornoCartaoServiceTests.cs
DEPENDENCIAS: TASK-013
CONTEXTO A LER: regra-de-negocio.md item 12

### TASK-015 — Fechar fatura (transicao ABERTA -> FECHADA)
STATUS: CONCLUIDA (transicao lazy: fecha a fatura ABERTA mais antiga no momento em que uma nova precisa ser criada; GET /api/cartoes/{contaId}/faturas para listagem)
AGENT: levi
ESCOPO: Rotina/endpoint que transiciona fatura de ABERTA para FECHADA quando data_fechamento e atingida, impedindo novas compras na fatura fechada (a compra seguinte abre a proxima).
ARQUIVOS: MyFinances/MyFinances/Services/FaturaCicloService.cs, Controllers/FaturasController.cs
DEPENDENCIAS: TASK-006
CONTEXTO A LER: regra-de-negocio.md item 12 (paragrafo "Fatura")

### TASK-016 — Revisao TASK-015
STATUS: CONCLUIDA (APROVADO apos 1 ciclo — achado critico: crash de constraint nao tratado em compra retroativa colidindo com fatura ABERTA mais recente; corrigido com rejeicao controlada. Tambem: entity vazando no controller (DTO criado), duplicacao extraida. Regra confirmada pelo usuario e documentada: rejeicao e definitiva)
AGENT: style
ESCOPO: Validar transicao de estado e que compra nao entra em fatura ja FECHADA.
ARQUIVOS: os do TASK-015
DEPENDENCIAS: TASK-015
CONTEXTO A LER: regra-de-negocio.md item 12

### TASK-017 — Testes TASK-015
STATUS: CONCLUIDA (7 testes novos, 50/50 total: mecanica de transicao ABERTA->FECHADA, reutilizacao sem duplicar, rejeicao de ciclo muito retroativo via ambos os metodos, listagem via DTO)
AGENT: mike
ESCOPO: Cobrir transicao de estado ABERTA -> FECHADA e bloqueio de compra em fatura fechada.
ARQUIVOS: MyFinances/MyFinances.Tests/FaturaFechamentoTests.cs
DEPENDENCIAS: TASK-016
CONTEXTO A LER: regra-de-negocio.md item 12

### TASK-018 — Endpoint pagamento de fatura
STATUS: CONCLUIDA (POST ~/api/faturas/{id}/pagamento; valor calculado server-side, transferencia de 2 pernas, fatura vira PAGA. 1 ciclo de correcao: faltava validar tipo BANCO na conta origem, rota de listagem mudada sem necessidade, semantica REST do retorno)
AGENT: levi
ESCOPO: POST /api/faturas/{id}/pagamento cria Transferencia com duas pernas (saida conta corrente / entrada conta CARTAO), vincula a fatura, muda status para PAGA. Pagamento fecha a fatura inteira, nunca compra a compra.
ARQUIVOS: MyFinances/MyFinances/Controllers/FaturasController.cs, Services/PagamentoFaturaService.cs
DEPENDENCIAS: TASK-015
CONTEXTO A LER: regra-de-negocio.md itens 3 (transferencia duas pernas) e 12 (paragrafo "Pagamento de fatura" + "Pagamento x fatura")

### TASK-019 — Revisao TASK-018
STATUS: CONCLUIDA (APROVADO apos 1 ciclo — achado critico: conta origem sem validar tipo BANCO, permitia pagar fatura com outro cartao ou com a propria conta)
AGENT: style
ESCOPO: Confirmar que o pagamento nao gera categoria de despesa, que as duas pernas compartilham transferencia_id, e que fatura PAGA nao aceita novo pagamento.
ARQUIVOS: os do TASK-018
DEPENDENCIAS: TASK-018
CONTEXTO A LER: regra-de-negocio.md itens 3, 12

### TASK-020 — Testes TASK-018
STATUS: CONCLUIDA (10 testes novos, 60/60 total: sucesso com soma correta, ABERTA/PAGA/inexistente rejeitados, tipo de conta origem, origem==destino, fatura sem lancamentos)
AGENT: mike
ESCOPO: Cobrir que o pagamento nao duplica com as compras (regra de sinal/dupla contagem) e que fecha o saldo da fatura como um todo.
ARQUIVOS: MyFinances/MyFinances.Tests/PagamentoFaturaServiceTests.cs
DEPENDENCIAS: TASK-019
CONTEXTO A LER: regra-de-negocio.md itens 2, 3, 12

### TASK-021 — Saldo calculado do cartao
STATUS: CONCLUIDA (GET /api/contas/{id}/saldo = SUM compras/estornos por FaturaId - SUM pagamentos CREDIT por TransferenciaId. 1 ciclo de correcao: duplicacao de validacao extraida pra ValidacaoCartaoService, contrato de erro padronizado)
AGENT: levi
ESCOPO: GET /api/contas/{id}/saldo para conta CARTAO retornando compras - pagamentos - estornos, calculado em tempo real, nunca armazenado.
ARQUIVOS: MyFinances/MyFinances/Services/SaldoCartaoService.cs, Controllers/ContasController.cs
DEPENDENCIAS: TASK-018 (precisa de compra, pagamento e estorno ja existentes)
CONTEXTO A LER: regra-de-negocio.md itens 10, 12 (paragrafo "Saldo do cartao")

### TASK-022 — Revisao TASK-021
STATUS: CONCLUIDA (APROVADO apos 1 ciclo — formula correta desde a 1a versao; achado de clean-code corrigido. Pendencia registrada, nao bloqueante: ContasController.CriarConta ainda retorna entity Conta crua, precisa de DTO proprio numa task futura)
AGENT: style
ESCOPO: Confirmar que o calculo nao soma valor cru (item 2) e que nenhuma linha e lida duas vezes (compra + estorno da mesma compra, pagamento).
ARQUIVOS: os do TASK-021
DEPENDENCIAS: TASK-021
CONTEXTO A LER: regra-de-negocio.md itens 2, 10, 12

### TASK-023 — Testes TASK-021
STATUS: CONCLUIDA (7 testes novos, 67/67 total: saldo zero, so compras, compras+estorno, compras+pagamento historico, conta invalida/nao-CARTAO, cenario combinado com 2 faturas)
AGENT: mike
ESCOPO: Cobrir o calculo do saldo com combinacoes de compra/pagamento/estorno.
ARQUIVOS: MyFinances/MyFinances.Tests/SaldoCartaoServiceTests.cs
DEPENDENCIAS: TASK-022
CONTEXTO A LER: regra-de-negocio.md itens 10, 12

### TASK-024 — Endpoint visao fluxo de caixa (CAIXA)
STATUS: CONCLUIDA (GET /api/lancamentos?visao=caixa; exclui compras FaturaId!=null, transferencia mostra so a perna DEBIT. 1 ciclo de correcao: filtro de Oculto faltando)
AGENT: levi
ESCOPO: GET /api/lancamentos?visao=caixa — lista lancamentos gerais mostrando o pagamento de fatura como saida, excluindo as compras individuais do cartao.
ARQUIVOS: MyFinances/MyFinances/Controllers/LancamentosController.cs, Services/FluxoCaixaService.cs
DEPENDENCIAS: TASK-018
CONTEXTO A LER: regra-de-negocio.md item 12 (paragrafo "Duas visoes" + "Lancamento geral / fluxo de caixa")

### TASK-025 — Endpoint visao categorica (COMPETENCIA)
STATUS: CONCLUIDA (GET /api/relatorios/categorias?mes=YYYY-MM; agrupa compras FaturaId!=null por CategoriaId, filtra por Lancamento.Data (competencia). 1 ciclo de correcao: clean-code + bug de NullReferenceException introduzido no fix, corrigido pelo Kira com ternario em expression tree)
AGENT: levi
ESCOPO: GET /api/relatorios/categorias?mes= — soma compras do cartao por categoria, ignorando pagamento/transferencia.
ARQUIVOS: MyFinances/MyFinances/Controllers/RelatoriosController.cs, Services/RelatorioCategoriaService.cs
DEPENDENCIAS: TASK-009
CONTEXTO A LER: regra-de-negocio.md item 12 (paragrafo "Duas visoes" + "Categorico / gasto por categoria"), item 7 (categoria)

### TASK-026 — Revisao conjunta TASK-024 + TASK-025 (nucleo do modelo)
STATUS: CONCLUIDA (nucleo critico CAIXA/COMPETENCIA correto desde a 1a versao, sem sobreposicao nem dupla contagem. Achado real: Oculto nunca era filtrado em nenhuma query do projeto)
AGENT: style
ESCOPO: Validar lado a lado as duas queries — nenhuma compra pode aparecer no CAIXA, nenhum pagamento pode aparecer no COMPETENCIA. Regra critica do item 12; revisao isolada de cada endpoint nao basta.
ARQUIVOS: os do TASK-024 e TASK-025
DEPENDENCIAS: TASK-024, TASK-025
CONTEXTO A LER: regra-de-negocio.md item 12 inteiro

### TASK-027 — Testes conjuntos TASK-024 + TASK-025
STATUS: CONCLUIDA (9 testes novos, 76/76 total: cenario central sem dupla contagem, Oculto, transferencia so DEBIT, estorno, sem categoria sem crash, fora do mes, filtro por conta)
AGENT: mike
ESCOPO: Cenario com N compras + 1 pagamento no mes: comprovar que a soma do CAIXA e so o pagamento e a soma do COMPETENCIA e so as compras, sem sobreposicao de valor total.
ARQUIVOS: MyFinances/MyFinances.Tests/VisoesCartaoTests.cs
DEPENDENCIAS: TASK-026
CONTEXTO A LER: regra-de-negocio.md item 12

### TASK-028 — Endpoint projecao do mes (cartao como 1 linha)
STATUS: CONCLUIDA (GET /api/cartoes/{contaId}/projecao?mes=YYYY-MM; escopo reduzido pra so a fatia do cartao. 1 ciclo de correcao: strings magicas, nome em espanhol, e uma lacuna real registrada como log — mais de uma fatura vencendo no mesmo mes pode esconder saldo, decisao de produto pendente)
AGENT: levi
ESCOPO: REVISADO (decisao do usuario 2026-07-05): o endpoint completo de
projecao (saldo_projetado = recebido - pago - a_pagar) depende de dados
que este modulo nao possui (lancamento avulso, conta fixa — parece ser
responsabilidade de outro modulo/sessao, ha indicio de um worktree
"lancamento-geral-tasks" cuidando disso). Escopo reduzido: implementar
GET /api/cartoes/{contaId}/projecao?mes=YYYY-MM que devolve SO a
contribuicao do cartao pra projecao daquele mes — a fatura cujo
data_vencimento cai dentro do mes pedido (0 ou 1 fatura), com valor e
status pago/nao pago (usando FaturaSaldoCalculator: pago se
ValorPendente<=0, senao "a pagar" com o ValorPendente restante, nao o
ValorTotal original, ja que pagamento parcial existe agora). Compras
individuais nao entram (regra ja garantida por FaturaId!=null nao
aparecer aqui). Nao implementa o saldo_projetado completo, so a fatia do
cartao — outro modulo consome isso.
ARQUIVOS: MyFinances/MyFinances/Controllers/ProjecaoController.cs, Services/ProjecaoService.cs
DEPENDENCIAS: TASK-015, TASK-018, TASK-038
CONTEXTO A LER: regra-de-negocio.md itens 9 e 12 (paragrafo "Projecao") e 12 ("Pagamento x fatura (revisado)")

### TASK-029 — Revisao TASK-028
STATUS: CONCLUIDA (APROVADO apos 1 ciclo — achado real: multiplas faturas vencendo no mesmo mes eram descartadas silenciosamente, agora logado como lacuna pendente de decisao do usuario. Tambem: strings magicas e nome em espanhol)
AGENT: style
ESCOPO: Confirmar que so a fatura atual entra (nao faturas passadas/futuras) e que compras nao vazam para a projecao.
ARQUIVOS: os do TASK-028
DEPENDENCIAS: TASK-028
CONTEXTO A LER: regra-de-negocio.md itens 9, 12

### TASK-030 — Testes TASK-028
STATUS: CONCLUIDA (7 testes novos, 89/89 total: pendente/parcial/quitada, sem fatura no mes, conta invalida/nao-CARTAO, compras nao aparecem isoladas)
AGENT: mike
ESCOPO: Cobrir que a projecao do mes soma o cartao como exatamente 1 linha independente de quantas compras existam na fatura.
ARQUIVOS: MyFinances/MyFinances.Tests/ProjecaoServiceTests.cs
DEPENDENCIAS: TASK-029
CONTEXTO A LER: regra-de-negocio.md itens 9, 12

### TASK-031 — Scaffold do projeto frontend (Vite + React + TS)
STATUS: CONCLUIDA (Vite+React+TS strict, axios, React Query, react-router, tokens de tema em CSS vars fiel ao identidade-visual.md, rotas placeholder. Ajustei porta do backend no fallback da baseURL. Pendencia anotada: campo "limite de credito" do mockup nao existe no backend, nao implementado)
AGENT: hanzo
ESCOPO: Criar o projeto Vite/React/TypeScript na raiz do repo, estrutura de pastas por feature, camada de dados via React Query (ou hook proprio) conforme stack.md.
ARQUIVOS: frontend/ (novo — package.json, vite.config.ts, tsconfig.json, src/)
DEPENDENCIAS: nenhuma (paralelo ao backend, mas telas de cartao dependem dos endpoints)
CONTEXTO A LER: stack.md (secao Frontend React)

### TASK-032 — Tela conta cartao
STATUS: CONCLUIDA (ContaCartaoPage: form de criacao + saldo calculado exibido. Lacuna real de backend anotada: nao existe GET /api/contas nem GET /api/contas/{id}, front usa localStorage como workaround temporario ate esse endpoint existir)
AGENT: hanzo
ESCOPO: Criar/listar conta CARTAO e exibir saldo calculado (GET /api/contas/{id}/saldo).
ARQUIVOS: frontend/src/features/cartao/ContaCartaoPage.tsx, frontend/src/features/cartao/api.ts
DEPENDENCIAS: TASK-031, TASK-004, TASK-021
CONTEXTO A LER: regra-de-negocio.md item 12; identidade-visual.md se existir

### TASK-033 — Tela lancar compra
STATUS: CONCLUIDA (LancarCompraForm como modal integrado a ContaCartaoPage; invalida saldo apos sucesso. Categoria desabilitada com aviso — backend nao tem endpoint de listagem de categorias)
AGENT: hanzo
ESCOPO: Formulario de compra (categoria, data, valor, descricao), mostrando a fatura vigente a qual a compra vai pertencer.
ARQUIVOS: frontend/src/features/cartao/LancarCompraForm.tsx
DEPENDENCIAS: TASK-031, TASK-009
CONTEXTO A LER: regra-de-negocio.md item 12

### TASK-034 — Tela fechar/ver fatura
STATUS: PENDENTE
AGENT: hanzo
ESCOPO: Listar faturas da conta CARTAO com status (ABERTA/FECHADA/PAGA) e detalhe das compras de cada fatura.
ARQUIVOS: frontend/src/features/cartao/FaturaPage.tsx
DEPENDENCIAS: TASK-031, TASK-015
CONTEXTO A LER: regra-de-negocio.md item 12

### TASK-035 — Acao marcar fatura como paga
STATUS: PENDENTE
AGENT: hanzo
ESCOPO: Botao/fluxo de pagamento de fatura, escolhendo a conta corrente de origem, chamando POST /api/faturas/{id}/pagamento.
ARQUIVOS: frontend/src/features/cartao/PagarFaturaModal.tsx
DEPENDENCIAS: TASK-031, TASK-018
CONTEXTO A LER: regra-de-negocio.md itens 3, 12

### TASK-036 — Tela visao por categoria do cartao
STATUS: PENDENTE
AGENT: hanzo
ESCOPO: Relatorio categorico consumindo GET /api/relatorios/categorias, sem misturar com a visao de fluxo de caixa.
ARQUIVOS: frontend/src/features/cartao/RelatorioCategoriaPage.tsx
DEPENDENCIAS: TASK-031, TASK-025
CONTEXTO A LER: regra-de-negocio.md item 12

### TASK-037 — Revisao conjunta do frontend cartao
STATUS: PENDENTE
AGENT: style
ESCOPO: Garantir que nenhuma tela recalcula saldo/classificacao no cliente e que a UI nao mistura visao CAIXA com COMPETENCIA (item 12 e critico tambem na apresentacao, nao so no backend).
ARQUIVOS: frontend/src/features/cartao/ (todos os arquivos das tasks 032-036)
DEPENDENCIAS: TASK-032, TASK-033, TASK-034, TASK-035, TASK-036
CONTEXTO A LER: regra-de-negocio.md item 12

---

## Rework: pagamento antecipado/parcial de fatura

Decisao do usuario (2026-07-05): pagamento de fatura pode ser ANTECIPADO
(fatura ainda ABERTA) e PARCIAL (varios pagamentos ate quitar). Isso
substitui o comportamento da TASK-018 original (so fatura FECHADA, valor
sempre o total, 1 pagamento por fatura). Fatura-Transferencia muda de 1:1
para 1:N. Regra atualizada em regra-de-negocio.md ("Pagamento x fatura
(revisado)") e schema.dbml.

### TASK-038 — Rework do pagamento de fatura (antecipado + parcial)
STATUS: CONCLUIDA (Fatura-Transferencia 1:1->1:N, valor vem do request, saldo_pendente calculado, rejeita overpayment/quitada. Interrompida por limite de sessao no meio, retomada pelo Kira: corrigiu 7 testes com ordem de validacao errada e um bug real de "fatura sem lancamento virando PAGA")
AGENT: levi
ESCOPO: Mudar Fatura-Transferencia de 1:1 para 1:N (Transferencia.FaturaId,
migration nova). PagamentoFaturaService: valor vem do client (nao mais
calculado auto), calcula saldo_pendente = total_lancamentos - soma_pagamentos,
rejeita valor <= 0 e valor > saldo_pendente (overpayment), rejeita se
saldo_pendente <= 0 (ja quitada). Permite pagar ABERTA ou FECHADA. Se
FECHADA e zera saldo -> PAGA. Se ABERTA e zera saldo -> continua ABERTA.
FaturaCicloService: ao fechar ciclo (ABERTA->FECHADA), se saldo_pendente
ja for <= 0 nesse momento, vai direto pra PAGA em vez de FECHADA. Precisa
atualizar tambem os testes existentes que quebrarem de compilar por causa
da mudanca de shape (FaturaCicloServiceTests, PagamentoFaturaServiceTests,
FaturaTransicaoEstadoTests, SaldoCartaoServiceTests, CompraCartaoServiceTests,
EstornoCartaoServiceTests — qualquer um que construa Fatura/Transferencia
direto).
ARQUIVOS: Models/Fatura.cs, Models/Transferencia.cs, Data/AppDbContext.cs,
Migrations/ (nova), Services/PagamentoFaturaService.cs,
Services/FaturaCicloService.cs, Dtos/PagarFaturaRequest.cs,
Dtos/FaturaResponseDto.cs (se precisar expor saldo_pendente),
Controllers/FaturasController.cs, arquivos de teste existentes (so ajuste
de compilacao, nao mudar o que os testes verificam)
DEPENDENCIAS: TASK-018, TASK-021
CONTEXTO A LER: regra-de-negocio.md item 12 ("Pagamento x fatura
(revisado)"), item 10, schema.dbml tabelas fatura/transferencia

### TASK-039 — Revisao TASK-038
STATUS: CONCLUIDA (APROVADO apos 1 ciclo — achado real: fatura com compra+estorno cancelando exatamente virava "fatura zumbi" presa em FECHADA para sempre; formula de saldo duplicada em 4 lugares, extraida para FaturaSaldoCalculator; teste morto com "diario de bordo" da sessao interrompida removido)
AGENT: style
ESCOPO: Validar saldo_pendente calculado corretamente, rejeicao de
overpayment, transicoes de status (FECHADA+quitada->PAGA,
ABERTA+quitada->continua ABERTA, fechamento de ciclo com quitacao
antecipada->PAGA direto), e que nenhum teste existente foi perdido/alterado
alem do necessario pra compilar.
DEPENDENCIAS: TASK-038
CONTEXTO A LER: regra-de-negocio.md item 12

### TASK-040 — Testes TASK-038
STATUS: CONCLUIDA (6 testes novos, 82/82 total: pagamento parcial FECHADA e ABERTA, overpayment, fatura ja quitada, fluxos combinados de fechamento de ciclo com quitacao parcial/integral)
AGENT: mike
ESCOPO: Cobrir pagamento antecipado (ABERTA), pagamento parcial (2+
pagamentos ate quitar), rejeicao de overpayment, rejeicao de pagamento
apos quitada, e a transicao correta de status nos 3 cenarios (FECHADA
quitada, ABERTA quitada continua aberta, fechamento de ciclo com fatura
ja quitada antecipadamente vira PAGA direto).
DEPENDENCIAS: TASK-039
CONTEXTO A LER: regra-de-negocio.md item 12
