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
STATUS: PENDENTE
AGENT: levi
ESCOPO: GET /api/lancamentos?visao=caixa — lista lancamentos gerais mostrando o pagamento de fatura como saida, excluindo as compras individuais do cartao.
ARQUIVOS: MyFinances/MyFinances/Controllers/LancamentosController.cs, Services/FluxoCaixaService.cs
DEPENDENCIAS: TASK-018
CONTEXTO A LER: regra-de-negocio.md item 12 (paragrafo "Duas visoes" + "Lancamento geral / fluxo de caixa")

### TASK-025 — Endpoint visao categorica (COMPETENCIA)
STATUS: PENDENTE
AGENT: levi
ESCOPO: GET /api/relatorios/categorias?mes= — soma compras do cartao por categoria, ignorando pagamento/transferencia.
ARQUIVOS: MyFinances/MyFinances/Controllers/RelatoriosController.cs, Services/RelatorioCategoriaService.cs
DEPENDENCIAS: TASK-009
CONTEXTO A LER: regra-de-negocio.md item 12 (paragrafo "Duas visoes" + "Categorico / gasto por categoria"), item 7 (categoria)

### TASK-026 — Revisao conjunta TASK-024 + TASK-025 (nucleo do modelo)
STATUS: PENDENTE
AGENT: style
ESCOPO: Validar lado a lado as duas queries — nenhuma compra pode aparecer no CAIXA, nenhum pagamento pode aparecer no COMPETENCIA. Regra critica do item 12; revisao isolada de cada endpoint nao basta.
ARQUIVOS: os do TASK-024 e TASK-025
DEPENDENCIAS: TASK-024, TASK-025
CONTEXTO A LER: regra-de-negocio.md item 12 inteiro

### TASK-027 — Testes conjuntos TASK-024 + TASK-025
STATUS: PENDENTE
AGENT: mike
ESCOPO: Cenario com N compras + 1 pagamento no mes: comprovar que a soma do CAIXA e so o pagamento e a soma do COMPETENCIA e so as compras, sem sobreposicao de valor total.
ARQUIVOS: MyFinances/MyFinances.Tests/VisoesCartaoTests.cs
DEPENDENCIAS: TASK-026
CONTEXTO A LER: regra-de-negocio.md item 12

### TASK-028 — Endpoint projecao do mes (cartao como 1 linha)
STATUS: PENDENTE
AGENT: levi
ESCOPO: GET /api/projecao?mes= — incorpora a fatura atual da conta CARTAO como uma unica linha (total + status pago/nao pago) dentro do calculo saldo_projetado = recebido - (pago + a_pagar); compras individuais nao entram.
ARQUIVOS: MyFinances/MyFinances/Controllers/ProjecaoController.cs, Services/ProjecaoService.cs
DEPENDENCIAS: TASK-015, TASK-018
CONTEXTO A LER: regra-de-negocio.md itens 9 e 12 (paragrafo "Projecao")

### TASK-029 — Revisao TASK-028
STATUS: PENDENTE
AGENT: style
ESCOPO: Confirmar que so a fatura atual entra (nao faturas passadas/futuras) e que compras nao vazam para a projecao.
ARQUIVOS: os do TASK-028
DEPENDENCIAS: TASK-028
CONTEXTO A LER: regra-de-negocio.md itens 9, 12

### TASK-030 — Testes TASK-028
STATUS: PENDENTE
AGENT: mike
ESCOPO: Cobrir que a projecao do mes soma o cartao como exatamente 1 linha independente de quantas compras existam na fatura.
ARQUIVOS: MyFinances/MyFinances.Tests/ProjecaoServiceTests.cs
DEPENDENCIAS: TASK-029
CONTEXTO A LER: regra-de-negocio.md itens 9, 12

### TASK-031 — Scaffold do projeto frontend (Vite + React + TS)
STATUS: PENDENTE
AGENT: hanzo
ESCOPO: Criar o projeto Vite/React/TypeScript na raiz do repo, estrutura de pastas por feature, camada de dados via React Query (ou hook proprio) conforme stack.md.
ARQUIVOS: frontend/ (novo — package.json, vite.config.ts, tsconfig.json, src/)
DEPENDENCIAS: nenhuma (paralelo ao backend, mas telas de cartao dependem dos endpoints)
CONTEXTO A LER: stack.md (secao Frontend React)

### TASK-032 — Tela conta cartao
STATUS: PENDENTE
AGENT: hanzo
ESCOPO: Criar/listar conta CARTAO e exibir saldo calculado (GET /api/contas/{id}/saldo).
ARQUIVOS: frontend/src/features/cartao/ContaCartaoPage.tsx, frontend/src/features/cartao/api.ts
DEPENDENCIAS: TASK-031, TASK-004, TASK-021
CONTEXTO A LER: regra-de-negocio.md item 12; identidade-visual.md se existir

### TASK-033 — Tela lancar compra
STATUS: PENDENTE
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
