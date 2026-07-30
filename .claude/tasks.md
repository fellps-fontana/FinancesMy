# Tasks — Modulo Contas a Receber (v1)

Escopo confirmado: item 13 da regra-de-negocio.md. Duas variantes da MESMA
entidade `conta_receber` (RECEBIVEL sem vinculo de conta/origem, EMPRESTIMO com
saida via transferencia de perna unica). Codebase NAO e greenfield: `Domain/`,
`Repositories/`, `Services/`, `Controllers/`, `DTOs/` ja existem para
Conta/Lancamento/Transferencia/Fatura/Ativo. Este modulo ALTERA duas tabelas
existentes (`transferencia`, `lancamento`) alem de criar `conta_receber`.

Regra CRITICA deste modulo: calculo de `saldo_pendente`/`status` (item 13,
bloco "Estados") e a transferencia de perna unica do EMPRESTIMO. Segue ciclo
TDD RED->GREEN completo (killua esqueleto -> mike RED -> levi GREEN -> mike
confirma -> style), conforme CLAUDE.md global secao 5.

---

## TASK-001 — Enums TipoContaReceber/StatusContaReceber + Entidade ContaReceber + migration

STATUS: CONCLUIDA (build limpo, migration AddContaReceberEntity gerada e conferida contra schema.dbml; navegacoes inversas Recebimentos/Transferencia ficam para TASK-002, que adiciona os campos de FK em Lancamento/Transferencia)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: nenhuma
CONTEXTO A LER: schema.dbml tabela `conta_receber`; regra-de-negocio.md item 13 INTEIRO
ESCOPO: criar enum `TipoContaReceber` (Recebivel, Emprestimo) e `StatusContaReceber` (Pendente, Parcial, Recebido), com `ToStorageValue`/`FromStorageValue` seguindo EXATAMENTE o padrao ja usado em `TipoConta.cs`/`StatusFatura.cs` (storage value em MAIUSCULO snake, ex: `RECEBIVEL`, `EMPRESTIMO`, `PENDENTE`, `PARCIAL`, `RECEBIDO`). Criar entidade `ContaReceber` com todos os campos do schema.dbml (`Id`, `Tipo`, `Descricao`, `Pessoa` nullable, `ValorTotal`, `DataRegistro`, `DataPrevista` nullable, `CategoriaId` nullable, `Status`) e relacionamentos (`Categoria?`, `ICollection<Lancamento> Recebimentos`, `Transferencia? Transferencia` — populado so quando `Tipo=Emprestimo`). Criar `ContaReceberConfiguration : IEntityTypeConfiguration<ContaReceber>` (`ToTable("conta_receber")`, mapeamento de cada coluna, conversion dos dois enums). Registrar `DbSet<ContaReceber>` no `MyFinancesDbContext` e gerar a migration.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Domain/ContaReceber.cs` (novo), `MyFinances/MyFinances/Domain/TipoContaReceber.cs` (novo), `MyFinances/MyFinances/Domain/StatusContaReceber.cs` (novo), `MyFinances/MyFinances/Infrastructure/Configurations/ContaReceberConfiguration.cs` (novo), `MyFinances/MyFinances/Data/MyFinancesDbContext.cs`, `MyFinances/MyFinances/Migrations/**`
NAO FAZER: nao criar Repository/Service ainda (TASK-003/004); nao adicionar CHECK de banco para "`Pessoa` obrigatorio se `Tipo=Emprestimo`" — essa validacao e do Service (TASK-006), nao do schema; nao mexer em `Transferencia`/`Lancamento` aqui (TASK-002).
RETORNO ESPERADO: migration aplicavel; tabela `conta_receber` criada no Postgres com campos e tipos do schema.dbml.

---

## TASK-002 — Alteracao em Transferencia (ContaDestinoId nullable + ContaReceberId) e Lancamento (ContaReceberId)

STATUS: CONCLUIDA (build limpo, migration AddContaReceberIdAndMakeContaDestinoIdNullable so com ALTER/ADD; Kira corrigiu inline um desvio de escopo do levi — PagamentoResponse.ContaDestinoId tinha virado Guid? no DTO publico do cartao, revertido pra Guid nao-nulo com !.Value na atribuicao, conforme instruido)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-001
CONTEXTO A LER: schema.dbml tabela `transferencia` (nota do campo `conta_destino_id` e `conta_receber_id`) e `lancamento` (`conta_receber_id`); regra-de-negocio.md item 13 paragrafo "Emprestimo: saida como transferencia de perna unica"; item 3 (padrao duas pernas) para contraste
ESCOPO: alterar `Transferencia.ContaDestinoId` de `Guid` para `Guid?`; adicionar `Transferencia.ContaReceberId` (`Guid?`) e navegacao `ContaReceber?`; adicionar `Lancamento.ContaReceberId` (`Guid?`) e navegacao `ContaReceber?`. Atualizar `TransferenciaConfiguration.cs` (remover `.IsRequired()` da property `ContaDestinoId`, tornar o relacionamento `HasOne(t => t.ContaDestino)` opcional, adicionar mapeamento de `ContaReceberId` com `OnDelete(DeleteBehavior.SetNull)`). Atualizar `LancamentoConfiguration.cs` adicionando `ContaReceberId` com `HasOne(l => l.ContaReceber).WithMany(cr => cr.Recebimentos).OnDelete(DeleteBehavior.SetNull)`. Gerar migration de ALTERACAO (nao recriar as tabelas).

**RISCO DE REGRESSAO — leia antes de codar:** `Transferencia.ContaDestinoId` hoje e `Guid` nao-nulo e `TransferenciaConfiguration.cs` linhas 31-33 tem `.IsRequired()`. `PagamentoFaturaService.cs` linha 64 (`ContaDestinoId = fatura.ContaId`) SEMPRE atribui um valor — tornar a propriedade `Guid?` NAO quebra esse fluxo em compilacao nem em runtime (atribuicao `Guid` -> `Guid?` e implicita e valida). O risco real: com a coluna nullable no banco, nada no schema impede que um erro FUTURO em qualquer service que cria `Transferencia` deixe `ContaDestinoId=null` por engano — a obrigatoriedade e CONDICIONAL (nulo so no fluxo de emprestimo) e nao e representavel por CHECK/FK limpo do EF, fica responsabilidade de cada Service. Nenhuma mudanca de logica em `PagamentoFaturaService.cs`, `CompraCartaoService.cs` ou `EstornoCartaoService.cs` e necessaria NESTA task — rode o build apos a migration para confirmar que nenhum desses arquivos usa `.Value` em `ContaDestinoId` (quebraria compilacao, seria pego na hora).
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Domain/Transferencia.cs`, `MyFinances/MyFinances/Domain/Lancamento.cs`, `MyFinances/MyFinances/Infrastructure/Configurations/TransferenciaConfiguration.cs`, `MyFinances/MyFinances/Infrastructure/Configurations/LancamentoConfiguration.cs`, `MyFinances/MyFinances/Migrations/**`
NAO FAZER: nao alterar `PagamentoFaturaService.cs`/`CompraCartaoService.cs`/`EstornoCartaoService.cs` — eles continuam obrigados a setar `ContaDestinoId`; se o build quebrar por causa dessa mudanca em algum desses arquivos, reportar como achado, nao corrigir sem avisar o Kira. Nao alterar `ContaOrigemId` (continua obrigatorio em todo fluxo, inclusive emprestimo).
RETORNO ESPERADO: migration de alteracao aplicavel; build passando sem regressao de compilacao em `PagamentoFaturaService`, `CompraCartaoService`, `EstornoCartaoService` ou qualquer outro consumidor de `Transferencia.ContaDestinoId`.

---

## TASK-003 — Repository de ContaReceber

STATUS: CONCLUIDA (build limpo, so os 3 arquivos permitidos tocados; conferido)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-001
CONTEXTO A LER: regra-de-negocio.md item 13; schema.dbml tabela `conta_receber`; `IFaturaRepository.cs`/`FaturaRepository.cs` como padrao de estilo
ESCOPO: criar `IContaReceberRepository`/`ContaReceberRepository` com: `Adicionar(ContaReceber)`, `ObterPorId(Guid)` (Include `Recebimentos`, `Transferencia`, `Categoria`), `Listar(StatusContaReceber? statusFiltro = null)` (todas, com filtro opcional), `Atualizar(ContaReceber)`, `Salvar()`. Registrar no DI (`Program.cs`).
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Repositories/IContaReceberRepository.cs` (novo), `MyFinances/MyFinances/Repositories/ContaReceberRepository.cs` (novo), `MyFinances/MyFinances/Program.cs`
NAO FAZER: nao implementar calculo de saldo pendente/status aqui (isso e `ContaReceberSaldoCalculator`, TASK-004); nao expor a entity fora da camada de dados; nao adicionar ainda o metodo de query da projecao do mes (TASK-010 adiciona quando o contrato do endpoint estiver decidido).
RETORNO ESPERADO: repository testavel, metodos nomeados por intencao.

---

## TASK-004 — Esqueleto de assinatura: ContaReceberService + ContaReceberSaldoCalculator (regra critica)

STATUS: CONCLUIDA (Kira materializou os 6 arquivos, todos com corpo NotImplementedException; build limpo. Registro DI de ContaReceberService fica pra TASK-006, quando a implementacao real entrar)
AGENT: killua
FLUXO: Implementacao
DEPENDENCIAS: TASK-002, TASK-003
CONTEXTO A LER: regra-de-negocio.md item 13 INTEIRO (bloco "Estados" e paragrafo "Emprestimo: saida como transferencia de perna unica" sao o nucleo); `FaturaSaldoCalculator.cs`/`PagamentoFaturaService.cs` como padrao arquitetural (calculadora estatica de saldo + service que orquestra Transferencia+Lancamento)
ESCOPO: entregar o esqueleto de assinatura COMPILAVEL (corpo `NotImplementedException`, sem logica real) para que `mike` escreva o teste RED antes de `levi` implementar. Kira cria os arquivos: `Domain/ContaReceberSaldoCalculator.cs` (metodo estatico `Calcular(ContaReceber)` retornando `record ContaReceberSaldo(decimal ValorTotal, decimal ValorRecebido, decimal SaldoPendente, StatusContaReceber Status)`), `Services/IContaReceberService.cs` e `Services/ContaReceberService.cs` (metodos `RegistrarRecebivel`, `RegistrarEmprestimo`, `RegistrarRecebimento`, `ObterPorId`, `Listar`), `Exceptions/ContaReceberNaoEncontradaException.cs`, `Exceptions/PessoaObrigatoriaParaEmprestimoException.cs`, `Exceptions/ValorRecebimentoExcedeSaldoPendenteException.cs` (novo — CONFIRMADO pelo usuario: recebimento que excede o saldo pendente e REJEITADO, nunca aceito com saldo negativo).
ARQUIVOS PERMITIDOS: nenhum (killua nao escreve arquivo — Kira cria os 6 arquivos a partir do esqueleto que killua devolveu)
NAO FAZER: nao implementar logica real em nenhum metodo (todo corpo lanca `NotImplementedException`).
RETORNO ESPERADO: Kira cria os 6 arquivos; projeto deve COMPILAR (nenhuma logica, so assinatura) antes de despachar mike.

---

## TASK-005 — Testes RED: regra critica de ContaReceber (saldo pendente, status, perna unica, recebimento)

STATUS: CONCLUIDA (16 testes, RED confirmado por NotImplementedException. Kira achou e corrigiu um gap estrutural durante a revisao: ContaReceber nao tinha a navegacao Recebimentos — adicionada em Domain/ContaReceber.cs + LancamentoConfiguration.cs, sem migration nova. mike corrigiu 6 testes que dependiam dessa navegacao, incluindo um teste de overpayment que fabricava saldo pendente sem setup real)
AGENT: mike
FLUXO: Implementacao (rodada RED — testes devem FALHAR por `NotImplementedException`, nunca por erro de compilacao)
DEPENDENCIAS: TASK-004
CONTEXTO A LER: regra-de-negocio.md item 13 INTEIRO
ESCOPO: escrever testes cobrindo: (a) `RegistrarRecebivel` cria `ContaReceber` com `Status=Pendente`, sem `Transferencia` associada; (b) `RegistrarEmprestimo` cria `ContaReceber` + `Transferencia` com `ContaDestinoId=null` e `ContaReceberId` preenchido + exatamente UM `Lancamento` Debit status Pago (nao dois); (c) `RegistrarEmprestimo` sem `pessoa` lanca `PessoaObrigatoriaParaEmprestimoException`; (d) `RegistrarRecebimento` gera `Lancamento` Credit status Pago vinculado via `ContaReceberId` na conta escolhida no momento; (e) `ContaReceberSaldoCalculator.Calcular` retorna `Pendente` quando nada foi recebido, `Parcial` quando `0 < saldo < valor_total`, `Recebido` quando `saldo <= 0`; (f) `valor_total` nunca muda entre registro e recebimentos; (g) `RegistrarRecebimento` com `categoriaId` sobrescreve a categoria sugerida do `ContaReceber` pai no lancamento gerado; (h) `ObterPorId`/`Listar` lancam/filtram corretamente; (i) `RegistrarRecebimento` com valor MAIOR que o `saldo_pendente` atual lanca `ValorRecebimentoExcedeSaldoPendenteException`, SEM criar o `Lancamento` e SEM alterar `Status`/saldo (confirmado pelo usuario: overpayment e rejeitado, nunca aceito). Rodar e CONFIRMAR RED (falha por `NotImplementedException`).
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances.Tests/Services/ContaReceberServiceTests.cs` (novo), `MyFinances/MyFinances.Tests/Domain/ContaReceberSaldoCalculatorTests.cs` (novo)
NAO FAZER: nao implementar nenhuma logica em `ContaReceberService`/`ContaReceberSaldoCalculator` para fazer o teste passar — isso e trabalho do levi na TASK-006. Nao marcar como bug uma falha por `NotImplementedException` (isso e o RED esperado).
RETORNO ESPERADO: suite de testes compilando e falhando (RED) por ausencia de logica, nunca por erro de compilacao; relatorio confirmando RED caso a caso.

---

## TASK-006 — ContaReceberService: implementacao da regra critica (GREEN contra o RED de mike)

STATUS: CONCLUIDA + APROVADA PELO STYLE apos 3 rodadas (22/22 testes GREEN no final). Rodada 1: Kira corrigiu inline 5 chamadas de Adicionar sem await (CS4014). Rodada 2 (style): achou 2 bugs reais — Status nunca transicionava apos recebimento (ficava travado em Pendente), e falta de validacao de contaOrigemId/contaDestinoId antes de persistir; mike escreveu 4 testes RED, levi corrigiu, 20/20 GREEN. Rodada 3 (style): achou um 3o bug — ContaReceberRepository.ObterPorId sem Include(Recebimentos), fazendo o calculo de saldo ignorar recebimentos anteriores em producao (overpayment passava, status errado); mike escreveu teste de integracao SQLite in-memory RED, Kira aplicou o fix de uma linha, 22/22 GREEN. APROVADO na 3a rodada.
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-005
CONTEXTO A LER: regra-de-negocio.md item 13 INTEIRO; os arquivos de teste da TASK-005 (LEITURA, nunca escrita)
ESCOPO: implementar `ContaReceberSaldoCalculator.Calcular` e todos os metodos de `ContaReceberService` contra os testes RED da TASK-005, ate ficarem GREEN. Pontos que a implementacao PRECISA cobrir: `RegistrarEmprestimo` cria `Transferencia` com `ContaDestinoId=null`/`ContaReceberId=this` e gera UM SO `Lancamento` (Debit, Pago) — nao dois, ao contrario do padrao de duas pernas do item 3; `RegistrarRecebivel` nao cria `Transferencia` nem `Lancamento` no momento do registro (so no recebimento); `RegistrarRecebimento` CALCULA o `saldo_pendente` ANTES de criar o lancamento e REJEITA (`ValorRecebimentoExcedeSaldoPendenteException`) se `valor > saldo_pendente` atual, sem criar nada; caso contrario cria `Lancamento` (Credit, Pago) vinculado via `ContaReceberId`, atualiza `ContaReceber.Status` via `ContaReceberSaldoCalculator` apos o novo lancamento; validar `pessoa` obrigatoria quando `Tipo=Emprestimo` (`PessoaObrigatoriaParaEmprestimoException`); validar existencia de conta/ContaReceber.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Domain/ContaReceberSaldoCalculator.cs`, `MyFinances/MyFinances/Services/ContaReceberService.cs`, `MyFinances/MyFinances/Services/IContaReceberService.cs` (so se precisar ajustar assinatura por incompatibilidade real com o teste — reportar se isso acontecer), `MyFinances/MyFinances/Exceptions/*.cs` (novas excecoes so se o teste exigir e nao existir ainda), `MyFinances/MyFinances/Program.cs` (registro DI)
NAO FAZER: nao alterar nenhum arquivo em `MyFinances.Tests/**` (arquivos de teste sao leitura, nunca escrita); nao gerar duas pernas de Lancamento no emprestimo (isso reintroduziria o bug que o item 13 explicitamente resolve).
RETORNO ESPERADO: `ContaReceberService`/`ContaReceberSaldoCalculator` implementados; todos os testes da TASK-005 GREEN (roda local antes de devolver).

---

## TASK-007 — Confirmar GREEN dos testes de regra critica (mike)

STATUS: CONCLUIDA (16/16 GREEN confirmado por mike, ja verificado por Kira antes tambem. Segue pro style antes da TASK-008, conforme ciclo TDD da secao 5 do CLAUDE.md global)
AGENT: mike
FLUXO: Implementacao (rodada GREEN — so RODA os testes existentes, nao reescreve)
DEPENDENCIAS: TASK-006
CONTEXTO A LER: nenhum (so roda a suite da TASK-005)
ESCOPO: rodar `ContaReceberServiceTests`/`ContaReceberSaldoCalculatorTests` e confirmar GREEN.
ARQUIVOS PERMITIDOS: nenhum (so execucao; se algum teste falhar por bug de codigo, reportar arquivo+linha, sem editar nada)
NAO FAZER: nao reescrever teste para forcar passagem; nao editar `ContaReceberService`.
RETORNO ESPERADO: GREEN confirmado, ou relatorio estruturado de bug (arquivo+linha) devolvido ao Kira para redespachar levi.

---

## TASK-008 — Controller REST de ContaReceber

STATUS: CONCLUIDA + APROVADA PELO STYLE apos 2 rodadas (225/225 testes GREEN no final). Kira corrigiu proativamente o mesmo bug de Include ausente (agora em Listar, nao so ObterPorId). Rodada 1 do style: achou que RegistrarEmprestimo nunca setava Lancamento.TransferenciaId (bug critico — quebrava a exclusao de gasto/receita do item 3/13, emprestimo apareceria como despesa real), catch morto de ContaNaoEncontradaException em RegistrarRecebivel, e nome do controller fora do padrao plural do projeto; mike escreveu teste RED, levi corrigiu os 3 pontos. Rodada 2: APROVADO. Controller renomeado para ContasReceberController (rotas HTTP inalteradas).
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-007
CONTEXTO A LER: clean-code.md secao "Organizacao (.NET)"; `AtivosController.cs` como padrao de estilo (excecao tipada -> status HTTP)
ESCOPO: criar `ContaReceberController` com `POST /api/contas-receber/recebiveis`, `POST /api/contas-receber/emprestimos`, `POST /api/contas-receber/{id}/recebimentos`, `GET /api/contas-receber` (filtro opcional `?status=`), `GET /api/contas-receber/{id}`. DTOs de entrada/saida (nunca a entity): `RegistrarRecebivelRequest`, `RegistrarEmprestimoRequest`, `RegistrarRecebimentoRequest`, `ContaReceberResponse` (incluindo `SaldoPendente` calculado via `ContaReceberSaldoCalculator`), `RecebimentoResponse`. Traducao de excecoes: `ContaReceberNaoEncontradaException`->404, `ContaNaoEncontradaException`->404, `PessoaObrigatoriaParaEmprestimoException`->422, `ValorRecebimentoExcedeSaldoPendenteException`->422, `ValorInvalidoException`->400 (reaproveitar a excecao ja existente para valor<=0).
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Controllers/ContaReceberController.cs` (novo), `MyFinances/MyFinances/DTOs/ContaReceber/*.cs` (novo)
NAO FAZER: nao colocar regra de negocio no controller — so orquestra Service+DTO; nao expor `Status`/`SaldoPendente` como campo editavel de entrada (sempre calculado).
RETORNO ESPERADO: contrato de API documentado (rota, verbo, body de entrada, shape de retorno) para os 5 endpoints.

---

## TASK-009 — Testes de integracao HTTP do ContaReceberController

STATUS: CONCLUIDA (12/12 GREEN, suite completa 237/237. Testes de overpayment e transicao PARCIAL/RECEBIDO passam pelo pipeline HTTP real, exercitando os fixes de Include ja aprovados na TASK-008. Nao precisou de nova rodada de style — sem codigo de producao novo)
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-008
CONTEXTO A LER: regra-de-negocio.md item 13
ESCOPO: testes HTTP cobrindo: criar recebivel (201, sem transferencia); criar emprestimo (201, valida shape com `contaOrigemId`); emprestimo sem `pessoa` -> 422; registrar recebimento parcial -> `status=PARCIAL`, `saldoPendente` correto; recebimentos ate zerar -> `status=RECEBIDO`; recebimento com valor MAIOR que o saldo pendente -> 422, sem alterar o estado (confirmado: overpayment rejeitado); `GET` com filtro de status; `id` inexistente -> 404.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances.Tests/Controllers/ContaReceberControllerTests.cs` (novo)
NAO FAZER: nao alterar controller/service para fazer teste passar sem reportar.
RETORNO ESPERADO: testes passando; relatorio estruturado se achar bug de codigo.

---

## TASK-010 — Total a receber esperado no mes (fatia da projecao, item 9)

STATUS: CONCLUIDA + APROVADA PELO STYLE apos 3 rodadas (247/247 testes GREEN no final). Logica sempre esteve correta (confirmada rodada 1), mas nasceu sem nenhum teste — regra critica de calculo sem prova automatizada. mike escreveu 12 testes (6 service + 6 integracao SQLite in-memory), levi extraiu duplicacao de Include num metodo privado. Rodada 2 (style): achou 2 testes duplicados disfarcados de diferentes + comentarios acentuados; mike consolidou. Rodada 3: sobrou travessao em 3 titulos de #region; Kira corrigiu. Rodada 4: APROVADO.
AGENT: levi
FLUXO: Implementacao (NAO e extensao — nenhum endpoint de projecao/dashboard existe no codebase; ver "Duvida em aberto")
DEPENDENCIAS: TASK-006
CONTEXTO A LER: regra-de-negocio.md item 9 (formula completa e o paragrafo especifico de Contas a Receber) e item 13 bloco "Projecao do mes"
ESCOPO: adicionar `Task<decimal> CalcularTotalAReceberEsperadoNoMes(int ano, int mes)` em `IContaReceberService`/`ContaReceberService`, somando `SaldoPendente` (via `ContaReceberSaldoCalculator`, NUNCA `ValorTotal`) de todo `ContaReceber` com `Status=Pendente` E `DataPrevista` dentro do mes/ano informado, OU `Status=Parcial` (sem filtro de `DataPrevista` — entra todo mes corrente ate zerar, conforme item 9). Adicionar metodo de query correspondente no repository (`IContaReceberRepository.ListarParaProjecaoDoMes(int ano, int mes)`, filtrando no banco por status). Expor via `GET /api/contas-receber/total-esperado-mes?ano=&mes=`.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Services/IContaReceberService.cs`, `MyFinances/MyFinances/Services/ContaReceberService.cs`, `MyFinances/MyFinances/Repositories/IContaReceberRepository.cs`, `MyFinances/MyFinances/Repositories/ContaReceberRepository.cs`, `MyFinances/MyFinances/Controllers/ContaReceberController.cs`, `MyFinances/MyFinances/DTOs/ContaReceber/TotalAReceberEsperadoResponse.cs` (novo)
NAO FAZER: NAO tentar montar `saldo_projetado` completo (item 9) — isso soma `total_recebido_no_mes`/`total_pago_no_mes`/`total_a_pagar_no_mes`, que dependem de agregadores de `lancamento`/`fatura`/`conta_fixa` que NAO existem ainda como servico unificado (ver "Duvida em aberto"). Escopo aqui e SO a fatia de contas a receber, mesmo padrao estrito usado em Investimentos (TASK-006 antiga: "total investido != patrimonio total").
RETORNO ESPERADO: endpoint retornando `{ totalAReceberEsperadoNoMes: decimal }` para o par ano/mes informado; funcao de calculo isolada e nomeada.

---

## TASK-011 — Testes do total a receber esperado no mes

STATUS: CONCLUIDA (absorvida pela TASK-010 apos o style apontar falta de cobertura — os 4 cenarios exigidos aqui, incluindo a protecao contra dupla contagem, ja estao provados pelos 12 testes escritos e aprovados na TASK-010)
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-010
CONTEXTO A LER: regra-de-negocio.md item 9 e item 13 bloco "Projecao do mes"
ESCOPO: testar que o total soma `SaldoPendente` (nao `ValorTotal`) de `ContaReceber` `Status=Pendente` com `DataPrevista` no mes/ano informado; soma TODO `ContaReceber` `Status=Parcial` do mes corrente independente de `DataPrevista`; ignora `Status=Recebido`; retorna zero sem registros; nao soma o `ValorTotal` de um `ContaReceber` que ja teve recebimento parcial (evitando dupla contagem, conforme item 9 explicito).
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances.Tests/Services/ContaReceberServiceTests.cs`
NAO FAZER: nao alterar `ContaReceberService` para fazer teste passar sem reportar.
RETORNO ESPERADO: testes passando; relatorio estruturado se achar bug.

---

## TASK-012 — Camada de dados no front: types/api/hooks de Contas a Receber

STATUS: CONCLUIDA (build do frontend limpo, sem `any`; invalidacao de cache cruzada — lista, porId, totalEsperadoMes — conferida)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-008, TASK-010
CONTEXTO A LER: stack.md secao "Frontend (React)"; clean-code.md "Organizacao (React)"
ESCOPO: criar `features/contas-receber/{types.ts,api.ts,query-keys.ts}` e hooks (`useContasReceber`, `useCriarRecebivel`, `useCriarEmprestimo`, `useRegistrarRecebimento`, `useTotalAReceberEsperadoNoMes`), seguindo exatamente o padrao ja usado em `features/investimentos/`.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/contas-receber/types.ts` (novo), `MyFinanceFrontEnd/src/features/contas-receber/api.ts` (novo), `MyFinanceFrontEnd/src/features/contas-receber/query-keys.ts` (novo), `MyFinanceFrontEnd/src/features/contas-receber/hooks/*.ts` (novo)
NAO FAZER: nao colocar fetch solto em componente; nao renderizar UI aqui (TASK-013 a TASK-015).
RETORNO ESPERADO: hooks tipados (sem `any`), com invalidacao de cache cruzada apos criar/receber (saldo pendente muda).

---

## TASK-013 — UI: listar contas a receber com status e saldo pendente

STATUS: CONCLUIDA (build do frontend limpo. Hanzo achou uma divergencia real entre identidade-visual.md e o tema shadcn: token --accent do projeto NAO e o roxo, e sim uma superficie neutra escura; o roxo real esta em --primary. Badge PARCIAL usa bg-primary/15 text-primary em vez do accent literal, decisao documentada em comentario no componente)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-012
CONTEXTO A LER: identidade-visual.md (se existir); regra-de-negocio.md item 13
ESCOPO: tela listando `ContaReceber` (tipo, descricao, pessoa quando emprestimo, valor total, saldo pendente, status com indicacao visual PENDENTE/PARCIAL/RECEBIDO).
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/contas-receber/ListaContasReceber.tsx` (novo), `MyFinanceFrontEnd/src/features/contas-receber/components/ContaReceberItem.tsx` (novo)
NAO FAZER: nao implementar logica de calculo de saldo/status no componente — vem pronto do backend via hook.
RETORNO ESPERADO: componente de apresentacao consumindo `useContasReceber`.

---

## TASK-014 — UI: formulario de criar recebivel/emprestimo

STATUS: CONCLUIDA (build limpo. Gap real resolvido: backend nao tem endpoint de listagem de contas combinando todos os tipos - form busca banco+investimento em paralelo e combina, excluindo cartao, pragmatico de UX. Estado mantido dentro do proprio componente, sem container separado, por nao haver lista a coordenar. useQuery de contas de origem ficou inline no componente, nao extraido pra hooks/, por restricao de arquivos permitidos da task - candidato a limpeza futura se quiser. Nao integrado em ListaContasReceber.tsx ainda, deliberado)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-012
CONTEXTO A LER: identidade-visual.md; regra-de-negocio.md item 13
ESCOPO: formulario com toggle RECEBIVEL/EMPRESTIMO — RECEBIVEL pede descricao/valor/data prevista/categoria; EMPRESTIMO adiciona `pessoa` (obrigatorio) e `contaOrigemId` (select de conta existente).
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/contas-receber/FormRegistrarContaReceber.tsx` (novo), `MyFinanceFrontEnd/src/features/contas-receber/lib/validarContaReceber.ts` (novo)
NAO FAZER: nao permitir editar `valorTotal` depois de criado (item 13: fixo, sem juros/correcao — isso e regra de backend, mas o form nao deve nem oferecer edicao de valor total em tela de recebimento).
RETORNO ESPERADO: componente chamando `useCriarRecebivel`/`useCriarEmprestimo` conforme o toggle.

---

## TASK-015 — UI: acao de registrar recebimento (parcial ou total)

STATUS: CONCLUIDA (build limpo. Extraiu hook useContasParaSelecao compartilhado entre este form e FormRegistrarContaReceber, eliminando a duplicacao registrada como pendente na TASK-014. Botao "Registrar recebimento" some quando status=RECEBIDO. Campo categoriaId deliberadamente omitido do form - nao ha combobox de categoria pronto no projeto, e um input de texto livre pra UUID cru seria pior que nao ter o campo; decisao documentada, categoriaId continua opcional na request)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-012, TASK-013
CONTEXTO A LER: identidade-visual.md; regra-de-negocio.md item 13 paragrafo "Parcelas / recebimento incremental"
ESCOPO: acao a partir do item da lista para registrar um recebimento (valor, data, conta destino, categoria opcional sobrescrevendo a sugerida), com validacao client-side de valor > 0 e feedback quando o back rejeitar.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/contas-receber/FormRegistrarRecebimento.tsx` (novo), `MyFinanceFrontEnd/src/features/contas-receber/lib/validarRecebimento.ts` (novo), `MyFinanceFrontEnd/src/features/contas-receber/components/ContaReceberItem.tsx`
NAO FAZER: nao travar no client o caso de recebimento que excede o saldo pendente alem de validacao basica de UX (a regra de negocio de aceitar/rejeitar overpayment nao esta decidida — ver "Duvida em aberto"; nao assumir nenhum dos dois lados na UI).
RETORNO ESPERADO: componente chamando `useRegistrarRecebimento`; invalidacao de cache atualiza saldo pendente/status na lista.

---

## Decisoes de modelagem (Killua)

- **`ComprasParceladasService` novo, nao extensao de `CompraCartaoService`.**
  Tradeoff avaliado: estender `CompraCartaoService.CriarCompraAsync` pra
  aceitar N parcelas quebraria a assinatura (retorna 1 `Lancamento`, precisa
  retornar N + o agrupador) e misturaria dois fluxos com aggregate roots
  diferentes (1 Lancamento vs 1 CompraParcelada + N Lancamento) no mesmo
  metodo — exatamente o tipo de "funcao que faz duas coisas" que
  clean-code.md probe. Custo da separacao: mais uma classe de Service e um
  Controller a mais; ganho: cada Service continua com uma unica
  responsabilidade, e `CompraCartaoService`/`EditarCompraAsync` (compra a
  vista) fica intocado, sem risco de regressao. Mesmo padrao de sub-recurso
  ja usado por `AtivosController`/`AtivoService` dentro de `Conta`.
- **`CompraParcelada` nao guarda `ContaId`.** Fiel ao schema.dbml (a tabela
  so tem `descricao`/`valor_total`/`quantidade_parcelas`/`data_compra`) — a
  conta e resolvida via os `Lancamento` filhos, todos da mesma conta por
  construcao (uma compra parcelada nasce de uma unica chamada de API com um
  `contaId` de rota). Se precisar filtrar `CompraParcelada` por conta no
  futuro (ex: listagem), a query passa por `Lancamentos.ContaId`, nao por
  campo proprio.
- **`OnDelete(SetNull)` na FK `Lancamento.CompraParceladaId`**, mesmo padrao
  ja usado por `Fatura`/`Transferencia` em `Lancamento` — nenhuma FK
  financeira faz cascade-delete de historico no dominio. Como estorno/edicao
  de parcelada estao fora desta leva, essa FK na pratica nunca e exercitada
  em delete ainda — a escolha e so consistencia de padrao, nao urgencia.
- **Algoritmo de split: truncar em 2 casas para as N-1 primeiras parcelas,
  resto na ultima.** Alternativa descartada: `Math.Round` com banker's
  rounding em cada parcela e ajustar a ultima por diferenca — funciona, mas
  e mais dificil de auditar (o "porque" da ultima parcela ser diferente fica
  implicito no resultado de arredondamento, nao explicito no truncamento).
  Truncamento deixa a regra "resto vai pra ultima" auditavel por construcao,
  batendo com a redacao literal da regra-de-negocio.md.
- **Cada parcela resolve sua propria FATURA andando ciclo a ciclo, nao soma
  meses corridos na data — DECISAO CONFIRMADA COM O USUARIO EM 2026-07-12.**
  Alternativa descartada: `data_compra.AddMonths(i-1)` direto. Problema dela:
  desalinha do ciclo real do cartao perto da virada do fechamento (ex:
  comprar 2 dias antes do fechamento faria a parcela 1 "pular" um ciclo
  inteiro em relacao a uma compra a vista feita no mesmo dia). A solucao
  adotada encadeia `FaturaCicloService.ResolverFaturaParaLancamentoAsync` N
  vezes (parcela 1 pela data da compra; parcela `i>1` por um dia dentro do
  ciclo seguinte ao da parcela anterior — `DataVencimento.AddDays(1)`), sem
  nenhuma logica de ciclo nova. Ver algoritmo completo na TASK-034, item 4.

## Duvidas em aberto para o usuario

1. **Estorno de compra parcelada** — cancelar todas as parcelas futuras,
   so a proxima, ou nenhuma automaticamente (usuario estorna parcela por
   parcela manualmente, igual compra a vista)? Regra omissa. Fora desta leva.
2. **Edicao de compra parcelada existente** — mudar `quantidade_parcelas`
   depois de criada reabre o calculo de todas as parcelas futuras (as ja
   vinculadas a fatura PAGA ficam intocadas)? Regra omissa. Fora desta leva.
3. **Teto de `quantidade_parcelas`** — regra-de-negocio.md nao define
   limite superior. `ComprasParceladasService` (TASK-034) so valida
   `>= 2`, sem teto. Se o usuario quiser um limite (ex: 12x, 24x), e
   decisao de produto a confirmar antes de travar no codigo.

---

# Modulo Lancamento Geral (DEMANDA-001) — porte para arquitetura atual

Gerado por killua em 2026-07-19, worktree `lancamento-geral-porte`.

Este NAO e um modulo greenfield. A DEMANDA-001 ja foi implementada por
inteiro uma vez, numa branch nunca mergeada (`worktree-lancamento-geral-tasks`,
ainda em disco em `.claude/worktrees/lancamento-geral-tasks`), que divergiu do
main ANTES do rework do Cartao (commit 158bb57) e da unificacao de DbContext
(`Models/`+`AppDbContext` -> `Domain/`+`MyFinancesDbContext`).

Killua confirmou (via Glob) que parte da infraestrutura ja foi portada pelo
proprio rework do Cartao: `Domain/Lancamento.cs`, `Domain/Transferencia.cs`,
`LancamentoConfiguration`, `TransferenciaConfiguration`, `DbSet<Lancamento>`,
`DbSet<Transferencia>`, `ILancamentoRepository`/`LancamentoRepository`,
`ITransferenciaRepository`/`TransferenciaRepository` ja existem. Falta so
Service/Controller/DTO — nao existe ainda `LancamentoManualService`,
`TransferenciaService`, `FluxoCaixaService`, `LancamentosController` nem
`TransferenciasController`.

As tasks abaixo portam a logica ja validada (regra de sinal, transferencia,
exclusao, status) para a forma atual (Repository em vez de DbContext direto,
enum em vez de string constants, retorno em tupla `(bool, T?, string?)` igual
ao Cartao) — nao redesenham a regra do zero.

## Decisoes de modelagem (Killua)

- **Regra de sinal (item 2, CRITICA) preservada 1:1**, so muda de pasta:
  `Domain/ClassificacaoLancamentoService.Classificar(Lancamento)`, sempre
  `Tipo` + `TransferenciaId`/`FaturaId`, nunca `Valor`. Precedencia:
  Transferencia > CompetenciaCartao > Tipo.
- **Exclusao de lancamento manual = HARD DELETE**, bloqueada se
  `TransferenciaId`, `FaturaId` ou `ConciliadoCom` estiverem preenchidos.
- **Escrita manual (criacao/edicao) so aceita Status PENDENTE ou PAGO.**
  SUGERIDO e exclusivo da conciliacao automatica (fora de escopo v1).
- **Transferencia manual exige as duas contas com `Origem = MANUAL`.**
  Cria 2 `Lancamento` (Debit origem / Credit destino), `Status=Pago`,
  `Manual=true`, mesmo `TransferenciaId`, atomicamente — mesma forma que
  `PagamentoFaturaService` (Cartao) ja usa, ja testada em producao.
- **Rotas: split em vez de bifurcacao por querystring.** O branch antigo
  usava `GET /api/lancamentos?visao=caixa` (o proprio doc antigo chamava
  isso de "colisao de rota resolvida" — um workaround). Aqui:
  `LancamentosController` (`api/lancamentos`, GET, visao caixa cross-conta),
  `ContaLancamentosController` (`api/contas/{contaId}/lancamentos`, CRUD
  manual, mesmo padrao de `CartaoComprasController`), `TransferenciasController`
  (`api/transferencias`, POST).
- **DTOs flat em `DTOs/`**, sem sufixo `Dto` (`LancamentoResponse`, nao
  `LancamentoResponseDto`) — segue o precedente real de `CompraResponse`/
  `EstornoResponse`/`PagamentoResponse` ja flat na pasta.

## Corte de escopo (nao e ajuste de arquitetura, e decisao de regra)

`LancamentoOcultacaoService` (soft-delete de lancamento Open Finance,
`PATCH /ocultar`) NAO foi portado. `regra-de-negocio.md` item 4 marca esse
comportamento como **FORA DE ESCOPO v1** — decisao tomada depois que a
branch antiga foi escrita (que tinha isso pronto e testado). Regra vence
sobre codigo antigo. Ver "Pendencias" no fim desta secao — precisa
confirmacao do usuario.

---

## TASK-038 — Esqueleto ClassificacaoLancamentoService

STATUS: CONCLUIDA (arquivos criados por Kira)
AGENT: killua
FLUXO: Implementacao
DEPENDENCIAS: nenhuma
CONTEXTO A LER: regra-de-negocio.md item 2 (CRITICA)
ESCOPO: esqueleto compilavel do enum `ClassificacaoLancamento` e do metodo `Classificar` com `NotImplementedException`.
CRITERIO DE ACEITE:
1. Projeto compila com `NotImplementedException` no corpo.
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances\Domain\ClassificacaoLancamento.cs` (novo)
`MyFinances\MyFinances\Domain\ClassificacaoLancamentoService.cs` (novo)
NAO FAZER: nao implementar logica real no corpo do metodo.
RETORNO ESPERADO: esqueleto compilavel, sem logica.

---

## TASK-039 — [REGRA CRITICA] RED: testes de ClassificacaoLancamentoService

STATUS: CONCLUIDA (feito diretamente pelo usuario fora da fila do Kira, commit 6e032f3 em 2026-07-19; tasks.md nao havia sido atualizado. Reconciliado em 2026-07-20 apos verificacao: decisions.md tem TASK-039 APROVADO, 7 testes cobrindo os 6 casos obrigatorios + 1 extra de precedencia dupla, suite GREEN)
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-038
CONTEXTO A LER: regra-de-negocio.md item 2 (CRITICA) inteiro; branch antiga `D:\Estudos\MyFinances\.claude\worktrees\lancamento-geral-tasks\MyFinances\MyFinances.Tests\ClassificacaoLancamentoServiceTests.cs` (6 casos de referencia, adaptar de `TipoLancamentoConstants`/string para `TipoLancamento`/`StatusLancamento` enum e de `MyFinances.Models` para `MyFinances.Domain`)
ESCOPO: escrever testes cobrindo: Debit sem vinculo -> Saida; Credit sem vinculo -> Entrada; TransferenciaId preenchido com Debit -> Transferencia; TransferenciaId preenchido com Credit -> Transferencia (prova que TransferenciaId ignora Tipo); FaturaId preenchido -> CompetenciaCartao; Credit com Valor negativo -> Entrada (prova que Valor nunca e lido).
CRITERIO DE ACEITE:
1. 6 testes escritos, projeto compila, todos falham por `NotImplementedException` (nunca erro de compilacao).
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances.Tests\Services\ClassificacaoLancamentoServiceTests.cs` (novo)
NAO FAZER: nao alterar `ClassificacaoLancamentoService.cs`.
RETORNO ESPERADO: confirmacao de RED + lista dos 6 casos cobertos.

---

## TASK-040 — [REGRA CRITICA] GREEN: implementar Classificar

STATUS: CONCLUIDA (feito diretamente pelo usuario fora da fila do Kira, commit 6edf3e7 em 2026-07-19. decisions.md tem TASK-040 APROVADO)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-039
CONTEXTO A LER: regra-de-negocio.md item 2; `ClassificacaoLancamentoServiceTests.cs` (leitura, nunca escrita)
ESCOPO: implementar `Classificar` com precedencia Transferencia > CompetenciaCartao > Tipo, sem ler `Valor` em nenhum ponto.
CRITERIO DE ACEITE:
1. Implementacao pronta para rodar os testes de TASK-039.
2. Nenhuma leitura de `lancamento.Valor` no corpo do metodo.
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances\Domain\ClassificacaoLancamentoService.cs`
NAO FAZER: nao tocar no arquivo de teste.
RETORNO ESPERADO: implementacao completa.

---

## TASK-041 — [REGRA CRITICA] Confirmar GREEN: ClassificacaoLancamentoService

STATUS: CONCLUIDA (feito diretamente pelo usuario fora da fila do Kira. decisions.md tem TASK-041 APROVADO)
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-040
CONTEXTO A LER: nenhum novo — so rodar a suite de TASK-039
ESCOPO: rodar `ClassificacaoLancamentoServiceTests.cs` contra a implementacao de TASK-040. Nao reescrever testes.
CRITERIO DE ACEITE: 6/6 passando.
ARQUIVOS PERMITIDOS: nenhum (so execucao)
NAO FAZER: nao reescrever teste; se falhar por bug, reportar arquivo+linha, nao corrigir.
RETORNO ESPERADO: confirmacao GREEN ou relatorio de bug.

---

## TASK-042 — Style: revisao ClassificacaoLancamentoService

STATUS: CONCLUIDA (feito diretamente pelo usuario fora da fila do Kira. decisions.md tem TASK-042 APROVADO)
AGENT: style
FLUXO: Implementacao
DEPENDENCIAS: TASK-041
CONTEXTO A LER: regra-de-negocio.md item 2; clean-code.md
ESCOPO: validar que a precedencia esta correta e que nao ha leitura de `Valor` em nenhum caminho.
CRITERIO DE ACEITE: veredito (APROVADO ou tarefa de correcao no esquema padrao).
ARQUIVOS PERMITIDOS: nenhum (style nao edita)
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + (se houver) tarefa de correcao, redespachada a levi.

---

## TASK-043 — Extensao de ILancamentoRepository (Remover + fluxo caixa)

STATUS: CONCLUIDA (feito diretamente pelo usuario fora da fila do Kira, commit e331e8e em 2026-07-19. decisions.md tem TASK-043 APROVADO. Bug de filtro corrigido depois na rodada TASK-050: perna CREDIT de transferencia estava sendo descartada de ListarParaFluxoCaixa)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: nenhuma
CONTEXTO A LER: stack.md secao "Organizacao de pastas (Backend)" (Repositories); branch antiga `Services/FluxoCaixaService.cs` (a query original, so como referencia da forma do filtro — nao copiar acesso a DbContext, so a logica: `FaturaId==null`, `TransferenciaId==null || Tipo==Debit`, `!Oculto`)
ESCOPO: adicionar `Task Remover(Lancamento lancamento)` e `Task<IEnumerable<Lancamento>> ListarParaFluxoCaixa(Guid? contaId)` em `ILancamentoRepository`/`LancamentoRepository`, seguindo exatamente o padrao ja usado por `IFaturaRepository.ObterFaturaAbertaPorConta` (metodo de repositorio nomeado por intencao de negocio).
CRITERIO DE ACEITE:
1. Projeto compila.
2. `ListarParaFluxoCaixa` exclui compras de cartao (`FaturaId != null`), lancamentos ocultos, e mantem so uma perna (Debit) de cada transferencia.
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances\Repositories\ILancamentoRepository.cs`
`MyFinances\MyFinances\Repositories\LancamentoRepository.cs`
NAO FAZER: nao adicionar logica de classificacao aqui (isso e Domain); nao remover metodos existentes.
RETORNO ESPERADO: 2 metodos novos, compilando.

---

## TASK-044 — LancamentoManualService + DTOs

STATUS: CONCLUIDA (feito diretamente pelo usuario fora da fila do Kira, commit 69f8cf7 em 2026-07-19. decisions.md tem TASK-044 APROVADO. Desvio de escopo real: interface `ILancamentoManualService` foi criada, contrariando o "NAO FAZER" original — sem impacto de regra, so estilo; nao revertido nesta reconciliacao)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-043
CONTEXTO A LER: regra-de-negocio.md itens 1 (conta MANUAL e fonte da verdade) e 5 (status PENDENTE/PAGO na escrita manual, nunca SUGERIDO); branch antiga `Services/LancamentoManualService.cs` e `DTOs/CriarLancamentoRequest.cs`/`EditarLancamentoRequest.cs`/`LancamentoResponseDto.cs` (forma, adaptar de `AppDbContext`+string constants para `ILancamentoRepository`/`IContaRepository`+enum); `Services/CompraCartaoService.cs` (padrao de convencao atual: DI por Repository, retorno em tupla); `Services/ValidacaoCartaoService.cs` (padrao de validacao de `conta.Ativa` — decisao do usuario em 2026-07-19: exigir conta ativa tambem aqui, mesmo a regra-de-negocio.md nao dizendo explicitamente)
ESCOPO: criar `LancamentoManualService` com `CriarLancamentoAsync`, `EditarLancamentoAsync`, `ListarLancamentosAsync` (filtro opcional por status), `ExcluirLancamentoAsync` (hard delete, bloqueado se `TransferenciaId`/`FaturaId`/`ConciliadoCom` preenchido), todos validando `conta.Origem == OrigemConta.Manual` E `conta.Ativa == true`; validar `Tipo` (DEBIT/CREDIT), `Status` (PENDENTE/PAGO, nunca SUGERIDO) e `Valor > 0` na entrada.
CRITERIO DE ACEITE:
1. Excluir lancamento vinculado a transferencia/fatura/conciliacao retorna erro sem apagar.
2. Criar/editar em conta `origem=OPEN_FINANCE` retorna erro.
3. Criar/editar em conta `Ativa=false` retorna erro.
4. `Status=SUGERIDO` rejeitado na criacao/edicao.
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances\Services\LancamentoManualService.cs` (novo)
`MyFinances\MyFinances\DTOs\CriarLancamentoRequest.cs` (novo)
`MyFinances\MyFinances\DTOs\EditarLancamentoRequest.cs` (novo)
`MyFinances\MyFinances\DTOs\LancamentoResponse.cs` (novo)
`MyFinances\MyFinances\Program.cs`
NAO FAZER: nao acessar `MyFinancesDbContext` direto (so via Repository); nao aceitar `Status=SUGERIDO`; nao implementar ocultacao/soft-delete OF (item 4, fora de escopo v1); nao criar interface `ILancamentoManualService`.
RETORNO ESPERADO: service + DTOs, compilando, registrado em `Program.cs`.

---

## TASK-045 — TransferenciaService + DTOs

STATUS: CONCLUIDA (feito diretamente pelo usuario fora da fila do Kira, commit d5cde3d em 2026-07-19. decisions.md tem TASK-045 APROVADO. Duplicacao com PagamentoFaturaService eliminada na rodada TASK-050 via TransferenciaLancamentoHelper)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: nenhuma
CONTEXTO A LER: regra-de-negocio.md item 3 (transferencias de mesma titularidade, branch manual) inteiro; branch antiga `Services/TransferenciaService.cs` e `DTOs/CriarTransferenciaRequest.cs` (forma, adaptar de `AppDbContext` para repositories e de string constants para enum); `Services/PagamentoFaturaService.cs` (mesma estrutura de 2 pernas — Debit origem/Credit destino, mesmo `TransferenciaId` — ja implementada e testada nesta arquitetura, usar como modelo direto); `DTOs/PagamentoResponse.cs` (padrao de DTO factory `FromTransferencia` a replicar); `Services/ValidacaoCartaoService.cs` (padrao de validacao de `conta.Ativa` — decisao do usuario em 2026-07-19: exigir conta ativa nas duas pontas)
ESCOPO: criar `TransferenciaService.CriarAsync` que valida `ContaOrigemId != ContaDestinoId`, ambas as contas `Origem == OrigemConta.Manual` E `Ativa == true`, `Valor > 0`, e cria a `Transferencia` + 2 `Lancamento` (Debit origem/Credit destino, `Status=Pago`, `Manual=true`, mesmo `TransferenciaId`) atomicamente.
CRITERIO DE ACEITE:
1. Transferencia entre 2 contas manuais ativas cria exatamente 2 lancamentos com mesmo `TransferenciaId`.
2. Transferencia envolvendo conta OF, conta inativa (origem ou destino) ou mesma conta origem/destino e rejeitada.
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances\Services\TransferenciaService.cs` (novo)
`MyFinances\MyFinances\DTOs\CriarTransferenciaRequest.cs` (novo)
`MyFinances\MyFinances\DTOs\TransferenciaResponse.cs` (novo)
`MyFinances\MyFinances\Program.cs`
NAO FAZER: nao permitir transferencia com conta `origem=OPEN_FINANCE` ou `Ativa=false`; nao expor a entity `Transferencia` crua no DTO (usar `TransferenciaResponse.FromTransferencia`, igual `PagamentoResponse.FromTransferencia`).
RETORNO ESPERADO: service + DTOs, compilando, registrado em `Program.cs`.

---

## TASK-046 — FluxoCaixaService + DTO

STATUS: CONCLUIDA (feito diretamente pelo usuario fora da fila do Kira, commit 37fddd8 em 2026-07-19. decisions.md tem TASK-046 APROVADO)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-043
CONTEXTO A LER: regra-de-negocio.md item 12 paragrafo "Duas visoes" e item 3 ("aparece como uma unica linha logica"); branch antiga `Services/FluxoCaixaService.cs` (forma da query, adaptar para consumir `ILancamentoRepository.ListarParaFluxoCaixa` em vez de `AppDbContext` direto)
ESCOPO: criar `FluxoCaixaService.ObterLancamentosCaixaAsync(Guid? contaId)` que devolve a visao CAIXA: compras de cartao fora, lancamentos ocultos fora, cada transferencia aparecendo como uma unica linha (perna Debit).
CRITERIO DE ACEITE:
1. Pagamento de fatura (transferencia conta corrente->cartao) aparece 1 vez.
2. Compra de cartao nao aparece.
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances\Services\FluxoCaixaService.cs` (novo)
`MyFinances\MyFinances\DTOs\LancamentoCaixaResponse.cs` (novo)
`MyFinances\MyFinances\Program.cs`
NAO FAZER: nao listar compras de cartao (`FaturaId != null`) nesta visao; nao duplicar as 2 pernas de uma transferencia na resposta.
RETORNO ESPERADO: service + DTO, compilando, registrado em `Program.cs`.

---

## TASK-047 — Controllers (Lancamentos, ContaLancamentos, Transferencias)

STATUS: CONCLUIDA COM DESVIO DE ROTA (feito diretamente pelo usuario fora da fila do Kira, commit 8d48e6a em 2026-07-19. decisions.md tem TASK-047 APROVADO. Desvio real: nao existe `ContaLancamentosController` separado nem `GET /api/lancamentos` cross-conta — tudo consolidado em `LancamentosController` sob `api/contas/{contaId}/lancamentos`, incluindo `GET .../fluxo-caixa` (sempre escopado a uma conta, nao cross-conta como o desenho original de killua previa). Ja revisado e aprovado pelo style na epoca; nao alterado nesta reconciliacao)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-044, TASK-045, TASK-046
CONTEXTO A LER: clean-code.md "Organizacao (.NET)" (controller so orquestra); `Controllers/CartaoComprasController.cs` e `Controllers/FaturasController.cs` (padrao de rota atual: `api/contas/{contaId}/...` para escopo de conta)
ESCOPO: criar `LancamentosController` (`GET api/lancamentos?contaId=`, visao caixa via `FluxoCaixaService`), `ContaLancamentosController` (`api/contas/{contaId}/lancamentos`: `GET` com filtro `status`, `POST`, `PUT/{id}`, `DELETE/{id}`, via `LancamentoManualService`), `TransferenciasController` (`POST api/transferencias`, via `TransferenciaService`).
CRITERIO DE ACEITE:
1. Os 3 controllers compilam.
2. Cada endpoint chama exatamente 1 Service.
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances\Controllers\LancamentosController.cs` (novo)
`MyFinances\MyFinances\Controllers\ContaLancamentosController.cs` (novo)
`MyFinances\MyFinances\Controllers\TransferenciasController.cs` (novo)
NAO FAZER: nao colocar validacao de regra de negocio no controller; nao reintroduzir `?visao=caixa`; nao devolver entity crua.
RETORNO ESPERADO: contrato de API documentado (rota, verbo, shape de retorno, codigos de status).

---

## TASK-048 — Testes de Service (LancamentoManual, Transferencia, FluxoCaixa)

STATUS: CONCLUIDA (feito diretamente pelo usuario fora da fila do Kira, commit 4353598 em 2026-07-19. decisions.md tem TASK-048 APROVADO)
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-044, TASK-045, TASK-046
CONTEXTO A LER: regra-de-negocio.md itens 1, 3, 5; branch antiga `LancamentoManualServiceTests.cs` e `TransferenciaServiceTests.cs` (so os nomes/casos, adaptar setup de `AppDbContext` para `MyFinancesDbContext` in-memory)
ESCOPO: testar CRUD manual (feliz + rejeicoes: conta OF, conta inativa, status SUGERIDO, valor<=0, exclusao bloqueada por vinculo), transferencia (feliz + rejeicoes: mesma conta, conta OF, conta inativa em qualquer ponta, valor<=0), fluxo caixa (exclui compra cartao, exclui oculto, transferencia como 1 linha).
CRITERIO DE ACEITE: testes passando cobrindo os casos listados.
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances.Tests\Services\LancamentoManualServiceTests.cs` (novo)
`MyFinances\MyFinances.Tests\Services\TransferenciaServiceTests.cs` (novo)
`MyFinances\MyFinances.Tests\Services\FluxoCaixaServiceTests.cs` (novo)
NAO FAZER: nao alterar os Services para o teste passar; bug de codigo volta relatorio estruturado.
RETORNO ESPERADO: testes passando; relatorio de bug se houver.

---

## TASK-049 — Testes HTTP dos controllers

STATUS: CONCLUIDA (feito diretamente pelo usuario fora da fila do Kira, commit 392e475 em 2026-07-19. decisions.md tem TASK-049 APROVADO)
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-047
CONTEXTO A LER: `MyFinances.Tests/Controllers/ContasControllerTests.cs` (padrao WebApplicationFactory + InMemory DB + JWT ja usado no projeto)
ESCOPO: testes HTTP dos 3 endpoints novos: fluxo caixa cross-conta, CRUD de lancamento manual, criacao de transferencia.
CRITERIO DE ACEITE: testes passando; status HTTP corretos (400 nas rejeicoes, 201/200/204 nos casos felizes).
ARQUIVOS PERMITIDOS:
`MyFinances\MyFinances.Tests\Controllers\LancamentosControllerTests.cs` (novo)
`MyFinances\MyFinances.Tests\Controllers\TransferenciasControllerTests.cs` (novo)
NAO FAZER: nao alterar controller/service pra passar teste sem reportar.
RETORNO ESPERADO: testes passando.

---

## TASK-050 — Style: revisao geral do modulo

STATUS: CONCLUIDA + APROVADA (feito diretamente pelo usuario fora da fila do Kira. commit 6e3b8e4 em 2026-07-19 corrige 5 problemas encontrados na revisao: [CRITICO] filtro de ListarParaFluxoCaixa descartava a perna CREDIT de transferencias; [ALTO] faltava validacao de conta ativa em MarcarComoPagoAsync/EditarAsync; [MEDIO] status HTTP inconsistentes entre controllers; [MEDIO] duplicacao entre TransferenciaService e PagamentoFaturaService (extraido TransferenciaLancamentoHelper); [BAIXO] TransferenciaResponse reaproveitava PagamentoResponse. 3 testes RED do mike confirmados GREEN apos fix. decisions.md tem TASK-050 APROVADO.

BUG POS-MERGE ENCONTRADO EM 2026-07-20: apos o merge do modulo Contas a Receber (PR anterior, que tornou `Transferencia.ContaDestinoId` de `Guid` para `Guid?` — TASK-002 daquele modulo), a `main` ficou QUEBRADA — `TransferenciaResponse.cs` (deste modulo) ainda declarava `ContaDestinoId` como `Guid` nao-nulo, causando erro de compilacao CS0266 na atribuicao `Guid?` -> `Guid`. Nenhum dos dois modulos, isolado, previa essa colisao. Corrigido nesta sessao (`ContaDestinoId` -> `Guid?` em `TransferenciaResponse.cs`); build e suite completa (324/324) confirmados verdes apos o fix. NOTA (modulo Conta Fixa, 2026-07-23): esse mesmo campo teve edicao fora de escopo revertida por Kira pelo menos 3 vezes durante a execucao de TASK-051/056/061 — ver docs/conta-fixa.md secao "Notas operacionais".)
AGENT: style
FLUXO: Implementacao
DEPENDENCIAS: TASK-049
CONTEXTO A LER: regra-de-negocio.md itens 1, 2, 3, 5, 12; clean-code.md inteiro
ESCOPO: revisar todo o modulo portado contra regra de negocio e clean-code, com atencao especial a: nenhum service le `DbContext` direto, nenhuma leitura de `Valor` fora do calculo explicito ja revisado em TASK-042, nenhum endpoint expõe entity crua.
CRITERIO DE ACEITE: veredito final (APROVADO ou tarefa de correcao no esquema padrao, redespachada ao levi).
ARQUIVOS PERMITIDOS: nenhum (style nao edita)
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito final do modulo.

---

## Mapa de dependencia (TASK-038 a TASK-050)

```
038 (esqueleto) -> 039 (RED) -> 040 (GREEN) -> 041 (confirma GREEN) -> 042 (style)
043 (repo)  ─┬─> 044 (LancamentoManualService) ─┐
             └─> 046 (FluxoCaixaService)        ├─> 047 (controllers) -> 049 (testes HTTP) -> 050 (style geral)
045 (TransferenciaService, sem dependencia) ────┘
044, 045, 046 -> 048 (testes de service)
```
043 e 045 nao dependem de 038-042 (a cadeia critica de Classificacao e
independente do resto — pode rodar em paralelo).

## Pendencias — resolvidas com o usuario em 2026-07-19

1. **Ocultacao de lancamento Open Finance (item 4) confirmada fora desta
   entrega.** `regra-de-negocio.md` marca isso como fora de escopo v1 —
   TASK-044 nao inclui `LancamentoOcultacaoService`/`PATCH /ocultar`.
2. **Validacao de `conta.Ativa` confirmada.** Gap pre-existente (nem a branch
   antiga nem o desenho original validavam isso) — usuario decidiu exigir
   conta ativa tambem em lancamento/transferencia manual, mesmo mode
   `regra-de-negocio.md` nao dizendo explicitamente. Refletido em TASK-044
   (`conta.Origem == Manual` + `conta.Ativa == true`), TASK-045 (ambas as
   contas ativas) e TASK-048 (casos de teste de rejeicao por conta inativa).

Nenhuma pendencia de decisao de produto restante. Queue pronta para execucao.

---

> **NOTA DE RECONCILIACAO (2026-07-23):** as duas secoes abaixo — "Modulo
> Conta Fixa" e "Modulo Limite de Gasto por Categoria" / "Modulo Projecao do
> Mes" — foram desenvolvidas em paralelo, em worktrees separados, sem
> coordenacao entre sessoes Kira. Ambas reutilizaram a mesma faixa de
> numeracao (TASK-051 em diante) de forma independente. Os numeros de task
> NAO sao unicos no arquivo inteiro a partir daqui — sempre resolva
> referencias pelo titulo + secao do modulo, nunca so pelo numero.

# Modulo Conta Fixa (DEMANDA-002)

Gerado por killua em 2026-07-20. Modulo greenfield: nenhum arquivo de codigo
existe ainda (`Domain/ContaFixa.cs` nao existe). Consome a arquitetura de
Lancamento Geral (DEMANDA-001, TASK-038 a TASK-050 acima, todas CONCLUIDA —
confirmado em disco por Kira antes de abrir esta secao).

Decisoes ja confirmadas com o usuario em 2026-07-20 (nao sao mais pendencia):
1. Horizonte de geracao: mes corrente + proximo (2 meses), a cada criacao/
   reativacao de ContaFixa.
2. Gatilho v1 (sync automatico e v2, item 11): geracao acontece so ao CRIAR
   ou REATIVAR uma ContaFixa, nunca por job separado. Idempotente.

Regra CRITICA deste modulo: geracao duplicada de Lancamento infla
`total_a_pagar_no_mes` (item 9) com despesa fictitia — mesmo criterio de
"calculo que afeta dinheiro real" ja usado em ClassificacaoLancamentoService/
ContaReceberSaldoCalculator. Segue ciclo TDD RED->GREEN (killua esqueleto ->
mike RED -> levi GREEN -> mike confirma -> style) para
`ContaFixaLancamentoFactory`/`ContaFixaService.GerarLancamentosPendentes`.
CRUD simples (criar/editar/desativar/reativar/listar) segue fluxo simples.

## Decisoes de modelagem (Killua)

- **`ContaFixaLancamentoFactory` como calculador estatico puro**, mesmo
  padrao de `ClassificacaoLancamentoService`/`ContaReceberSaldoCalculator`/
  `FaturaSaldoCalculator` ja usados no projeto. Constroi o `Lancamento`
  (calculo de data com clamp de dia + copia de campos), sem persistir e sem
  checar idempotencia — isso fica no Service+Repository, mantendo a regra
  critica testavel sem mock de banco.
- **`ExisteLancamentoGerado` vive em `IContaFixaRepository`, nao em
  `ILancamentoRepository`.** E uma pergunta de dominio de Conta Fixa sobre
  Lancamento, mesmo raciocinio de `IContaReceberRepository.ListarParaProjecaoDoMes`
  viver no repository "dono" do agregado.
- **`ContaFixaService` consome `ILancamentoRepository` direto (Adicionar/
  Salvar), nao `LancamentoManualService`.** Mesma decisao arquitetural que
  `CompraCartaoService` ja toma: geracao automatica pelo sistema nao passa
  pelo service de CRUD manual do usuario, que exige DTO com validacoes de UX
  que nao fazem sentido pra lancamento gerado automaticamente.
- **Retorno em tupla `(bool, T?, string?)`** em todos os metodos de
  `IContaFixaService`, seguindo o padrao do modulo irmao que este consome
  (`LancamentoManualService`/`TransferenciaService`). Nota: o projeto hoje
  tem dois estilos de retorno coexistindo (excecao tipada em
  `ContaReceberService`/`AtivoService` vs tupla aqui) — divida tecnica
  conhecida, fora de escopo unificar agora.

## Decisoes do usuario em 2026-07-20 (fecham as 3 duvidas de killua)

1. **Tipo do lancamento gerado = DEBIT fixo.** Confirmado. Nao existe conta
   fixa de recebimento na v1.
2. **Edicao de ContaFixa propaga para Lancamentos `Status=Pendente` ja
   gerados** (nunca `Pago`). Confirmado — opcao B do relatorio de killua.
3. **Desativar ContaFixa cancela (hard delete) os Lancamentos
   `Status=Pendente` ja gerados**; `Pago` fica intocado. Confirmado.

Refletido em `regra-de-negocio.md` item 6. TASK-056 e TASK-059 desbloqueadas.

---

## TASK-051 — Entidade ContaFixa + Configuration + migration

STATUS: CONCLUIDA (build limpo, migration AddContaFixa gerada; levi corrigiu de passagem um bug de build pre-existente e nao relacionado — TransferenciaResponse.ContaDestinoId estava Guid nao-nulo, incompativel com Transferencia.ContaDestinoId ja nullable desde TASK-002 de Contas a Receber; Kira verificou que e um fix minimo de 1 linha, ja identificado em outra branch, sem risco)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: nenhuma
CONTEXTO A LER: schema.dbml tabela `conta_fixa`; regra-de-negocio.md item 6 (revisado); `FaturaConfiguration.cs`/`ContaReceberConfiguration.cs` como padrao de estilo
ESCOPO: criar `Domain/ContaFixa.cs` (Id, ContaId, CategoriaId?, Descricao, Valor, DiaVencimento, Ativa, navegacoes Conta?/Categoria?/ICollection<Lancamento> Lancamentos) e `Infrastructure/Configurations/ContaFixaConfiguration.cs` (ToTable("conta_fixa"), mapeamento de cada coluna, HasOne Conta com OnDelete Cascade, HasOne Categoria com OnDelete SetNull). Registrar DbSet<ContaFixa> no MyFinancesDbContext e gerar migration.
CRITERIO DE ACEITE:
1. Projeto compila.
2. Migration aplicavel cria tabela `conta_fixa` com os campos e tipos do schema.dbml.
ARQUIVOS PERMITIDOS: `MyFinances\MyFinances\Domain\ContaFixa.cs` (novo), `MyFinances\MyFinances\Infrastructure\Configurations\ContaFixaConfiguration.cs` (novo), `MyFinances\MyFinances\Data\MyFinancesDbContext.cs`, `MyFinances\MyFinances\Migrations\**`
NAO FAZER: nao criar Repository/Service ainda (TASK-053/054); nao mexer em Lancamento.cs/LancamentoConfiguration.cs (TASK-052).
RETORNO ESPERADO: migration aplicavel; tabela conta_fixa criada.

---

## TASK-052 — Navegacao Lancamento.ContaFixa + FK

STATUS: CONCLUIDA (build limpo; migration so ajusta a FK ja existente por convencao do EF desde a TASK-051 para OnDelete=SetNull, sem recriar tabela/coluna)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-051
CONTEXTO A LER: schema.dbml tabela lancamento (campo conta_fixa_id); `Infrastructure/Configurations/LancamentoConfiguration.cs` (coluna conta_fixa_id ja mapeada, sem FK/navegacao ainda — confirmar linha exata)
ESCOPO: adicionar `public ContaFixa? ContaFixa { get; set; }` em Lancamento.cs e `builder.HasOne(l => l.ContaFixa).WithMany(cf => cf.Lancamentos).HasForeignKey(l => l.ContaFixaId).OnDelete(DeleteBehavior.SetNull)` em LancamentoConfiguration.cs. Gerar migration de ALTERACAO (adiciona FK constraint, coluna ja existe).
CRITERIO DE ACEITE:
1. Projeto compila.
2. Migration so adiciona FK constraint, nao recria a tabela lancamento.
ARQUIVOS PERMITIDOS: `MyFinances\MyFinances\Domain\Lancamento.cs`, `MyFinances\MyFinances\Infrastructure\Configurations\LancamentoConfiguration.cs`, `MyFinances\MyFinances\Migrations\**`
NAO FAZER: nao alterar nenhum outro campo de Lancamento; nao tocar em outros HasOne ja existentes.
RETORNO ESPERADO: migration de alteracao aplicavel; build limpo.

---

## TASK-053 — Repository de ContaFixa

STATUS: CONCLUIDA (build limpo, 324 testes passando, so os 3 arquivos permitidos tocados; Kira corrigiu de passagem um gap de sincronia do ModelSnapshot deixado pela TASK-052 — FK OnDelete=SetNull nao tinha sido regenerada no snapshot, confirmado via migration vazia apos o fix)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-052
CONTEXTO A LER: regra-de-negocio.md item 6 (revisado); `IContaReceberRepository.cs`/`ContaReceberRepository.cs` como padrao de estilo
ESCOPO: criar `IContaFixaRepository`/`ContaFixaRepository` com `Adicionar`, `ObterPorId` (Include Conta, Categoria, Lancamentos), `Listar(bool? ativaFiltro)`, `Atualizar`, `ExisteLancamentoGerado(Guid contaFixaId, int ano, int mes)` (query direta em Lancamentos filtrando ContaFixaId + Data.Year + Data.Month), `Salvar`. Registrar no DI (Program.cs).
CRITERIO DE ACEITE:
1. `ExisteLancamentoGerado` retorna true so quando ha Lancamento com o ContaFixaId e mes/ano exatos.
2. Registrado em Program.cs.
ARQUIVOS PERMITIDOS: `MyFinances\MyFinances\Repositories\IContaFixaRepository.cs` (novo), `MyFinances\MyFinances\Repositories\ContaFixaRepository.cs` (novo), `MyFinances\MyFinances\Program.cs`
NAO FAZER: nao implementar logica de geracao/clamp de data aqui (isso e ContaFixaLancamentoFactory, TASK-054).
RETORNO ESPERADO: repository testavel, metodos nomeados por intencao.

---

## TASK-054 — Esqueleto de assinatura: ContaFixaLancamentoFactory + ContaFixaService (regra critica)

STATUS: CONCLUIDA (Kira materializou os 3 arquivos a partir do esqueleto ja escrito no relatorio de arquitetura de killua; build limpo, so NotImplementedException)
AGENT: killua
FLUXO: Implementacao
DEPENDENCIAS: TASK-053
CONTEXTO A LER: regra-de-negocio.md item 6 INTEIRO (revisado); `FaturaCicloService.cs` (padrao CriarDataValida) e `ClassificacaoLancamentoService.cs` (padrao de calculador estatico puro)
ESCOPO: entregar esqueleto compilavel (corpo NotImplementedException) de `Domain/ContaFixaLancamentoFactory.cs` (metodo estatico `CriarLancamentoPendente(ContaFixa, int ano, int mes) -> Lancamento`) e `Services/IContaFixaService.cs` + `Services/ContaFixaService.cs` (metodos CriarAsync, EditarAsync, DesativarAsync, ReativarAsync, ObterPorId, Listar, GerarLancamentosPendentes, todos em tupla `(bool, T?, string?)`). Kira cria os arquivos a partir do esqueleto ja escrito no relatorio de arquitetura.
ARQUIVOS PERMITIDOS: nenhum (killua nao escreve arquivo)
NAO FAZER: nao implementar logica real em nenhum metodo.
RETORNO ESPERADO: Kira cria os 3 arquivos; projeto compila (so assinatura) antes de despachar mike.

---

## TASK-055 — [REGRA CRITICA] RED: testes de ContaFixaLancamentoFactory + idempotencia de GerarLancamentosPendentes

STATUS: CONCLUIDA (15 testes, RED confirmado por NotImplementedException, verificado por Kira. Primeira rodada tinha 2 bugs de compilacao no proprio teste — mock de metodo inexistente ILancamentoRepository.ObterPorContaFixaId e It.Any em vez de It.IsAny do Moq — mike corrigiu na segunda rodada, sem tocar em codigo de producao)
AGENT: mike
FLUXO: Implementacao (rodada RED)
DEPENDENCIAS: TASK-054
CONTEXTO A LER: regra-de-negocio.md item 6 INTEIRO
ESCOPO: testar (a) `CriarLancamentoPendente` com dia_vencimento normal (ex: 15) gera Data com esse dia no ano/mes informado; (b) dia_vencimento 31 em mes de 30 dias clampa pro ultimo dia (30); (c) dia_vencimento 31 em fevereiro (ano comum e bissexto) clampa para 28/29; (d) Tipo sempre Debit, Status sempre Pendente, Manual=true; (e) ContaId/CategoriaId/Descricao/Valor copiados fielmente da ContaFixa; (f) ContaFixaId do lancamento gerado aponta pra ContaFixa de origem; (g) `GerarLancamentosPendentes` cria exatamente 2 Lancamento (mes corrente + proximo) na primeira chamada; (h) rodar `GerarLancamentosPendentes` 2 vezes para a mesma ContaFixa/dataReferencia NAO duplica (idempotencia); (i) ContaFixa inexistente retorna Sucesso=false sem criar nada; (j) ContaFixa com Ativa=false retorna Sucesso=false sem criar nada; (k) `EditarAsync` atualiza valor/dia_vencimento/categoria dos Lancamentos vinculados com Status=Pendente, mas NAO altera nenhum Lancamento com Status=Pago; (l) `DesativarAsync` exclui os Lancamentos vinculados com Status=Pendente, mas NAO exclui nenhum Lancamento com Status=Pago.
CRITERIO DE ACEITE: testes compilam e falham por `NotImplementedException` (nunca erro de compilacao).
ARQUIVOS PERMITIDOS: `MyFinances\MyFinances.Tests\Domain\ContaFixaLancamentoFactoryTests.cs` (novo), `MyFinances\MyFinances.Tests\Services\ContaFixaServiceTests.cs` (novo)
NAO FAZER: nao implementar logica em ContaFixaService/ContaFixaLancamentoFactory para o teste passar.
RETORNO ESPERADO: confirmacao de RED caso a caso.

---

## TASK-056 — [REGRA CRITICA] GREEN: implementar ContaFixaLancamentoFactory + ContaFixaService

STATUS: CONCLUIDA (339/339 testes passando, sem regressao — 324 pre-existentes + 15 de ContaFixa. levi foi interrompido por limite de sessao da API no meio da task, mas a implementacao ja estava completa e correta no disco; Kira verificou build+suite geral e reverteu um desvio destrutivo fora de escopo que sobrou de uma investigacao anterior do proprio levi — TransferenciasController.cs tinha sido esvaziado (rotas/DI removidos) e TransferenciaResponse.cs alterado, nenhum dos dois no ARQUIVOS PERMITIDOS desta task; revertidos ao HEAD, build confirmado limpo sem eles)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-055
CONTEXTO A LER: regra-de-negocio.md item 6 INTEIRO (revisado com as decisoes de tipo/propagacao/desativacao); arquivos de teste da TASK-055 (leitura, nunca escrita)
ESCOPO: implementar `ContaFixaLancamentoFactory.CriarLancamentoPendente` (clamp de dia via `DateTime.DaysInMonth`, mesmo padrao de `FaturaCicloService.CriarDataValida`, Tipo sempre Debit) e todos os metodos de `ContaFixaService` contra os testes RED da TASK-055. `GerarLancamentosPendentes`: para (ano,mes) = dataReferencia e dataReferencia.AddMonths(1), checar `ExisteLancamentoGerado` antes de criar; `CriarAsync`/`ReativarAsync` chamam `GerarLancamentosPendentes` apos persistir. `EditarAsync` faz UPDATE em Lancamentos vinculados com `Status=Pendente` (nunca `Pago`) refletindo valor/dia_vencimento/categoria novos. `DesativarAsync` exclui (hard delete) os Lancamentos vinculados com `Status=Pendente` (nunca `Pago`).
CRITERIO DE ACEITE:
1. Todos os testes da TASK-055 GREEN.
2. Nenhuma duplicata de Lancamento em chamadas repetidas de GerarLancamentosPendentes.
3. EditarAsync nao altera Lancamento com Status=Pago; DesativarAsync nao exclui Lancamento com Status=Pago.
ARQUIVOS PERMITIDOS: `MyFinances\MyFinances\Domain\ContaFixaLancamentoFactory.cs`, `MyFinances\MyFinances\Services\ContaFixaService.cs`, `MyFinances\MyFinances\Services\IContaFixaService.cs` (so se incompatibilidade real com teste), `MyFinances\MyFinances\Program.cs` (DI)
NAO FAZER: nao alterar arquivos em MyFinances.Tests/**.
RETORNO ESPERADO: implementacao completa, testes rodados localmente GREEN antes de devolver.

---

## TASK-057 — Confirmar GREEN (mike)

STATUS: CONCLUIDA (redundante com verificacao que Kira ja fez na TASK-056 — 339/339 GREEN confirmado, sem dispatch separado, decisao do usuario)
AGENT: mike
FLUXO: Implementacao (rodada GREEN)
DEPENDENCIAS: TASK-056
CONTEXTO A LER: nenhum novo
ESCOPO: rodar `ContaFixaLancamentoFactoryTests`/`ContaFixaServiceTests` e confirmar GREEN.
CRITERIO DE ACEITE: 100% dos testes da TASK-055 passando.
ARQUIVOS PERMITIDOS: nenhum (so execucao)
NAO FAZER: nao reescrever teste; nao editar ContaFixaService.
RETORNO ESPERADO: GREEN confirmado ou relatorio de bug (arquivo+linha).

---

## TASK-058 — Style: revisao da regra critica

STATUS: CONCLUIDA + APROVADA PELO STYLE (15/15 testes reconfirmados. Clamp de data identico ao padrao de FaturaCicloService, GerarLancamentosPendentes nunca gera fora do par mes-atual/proximo, idempotencia checada antes de cada criacao, EditarAsync/DesativarAsync nunca tocam Status=Pago. Lacuna nao-bloqueante apontada: falta teste de virada de ano dezembro->janeiro na geracao — logica depende de DateOnly.AddMonths do framework, sem risco, so polimento de cobertura se quiser fechar depois)
AGENT: style
FLUXO: Implementacao
DEPENDENCIAS: TASK-057
CONTEXTO A LER: regra-de-negocio.md item 6; clean-code.md
ESCOPO: validar clamp de data, idempotencia, e que `GerarLancamentosPendentes` nunca cria lancamento fora do par (mes corrente, proximo mes).
CRITERIO DE ACEITE: veredito (APROVADO ou tarefa de correcao no esquema padrao).
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + (se houver) tarefa de correcao redespachada a levi.

---

## TASK-059 — Controller REST de ContaFixa + DTOs

STATUS: CONCLUIDA (build limpo, 339/339 testes sem regressao, so os 4 arquivos permitidos tocados; confirmado que nao repetiu o desvio de escopo em TransferenciasController/TransferenciaResponse da task anterior)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-058
CONTEXTO A LER: clean-code.md "Organizacao (.NET)"; `ContasReceberController.cs` como padrao de estilo
ESCOPO: criar `ContaFixaController` com `POST api/contas-fixas`, `PUT api/contas-fixas/{id}`, `POST api/contas-fixas/{id}/desativar`, `POST api/contas-fixas/{id}/reativar`, `GET api/contas-fixas` (filtro `?ativa=`), `GET api/contas-fixas/{id}`. DTOs: `CriarContaFixaRequest`, `EditarContaFixaRequest`, `ContaFixaResponse`.
CRITERIO DE ACEITE:
1. Criar dispara geracao imediata (2 lancamentos), verificavel na resposta ou em chamada subsequente de listagem de lancamentos.
2. Reativar dispara geracao idempotente.
3. Controller nao contem regra de negocio, so orquestra Service+DTO.
ARQUIVOS PERMITIDOS: `MyFinances\MyFinances\Controllers\ContaFixaController.cs` (novo), `MyFinances\MyFinances\DTOs\ContaFixa\*.cs` (novo)
NAO FAZER: nao expor Ativa como campo editavel direto (so via desativar/reativar); nao expor entity crua.
RETORNO ESPERADO: contrato de API documentado (rota, verbo, body, shape de retorno).

---

## TASK-060 — Testes HTTP do ContaFixaController

STATUS: CONCLUIDA (8/8 GREEN, suite completa 347/347. So o arquivo permitido tocado)
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-059
CONTEXTO A LER: regra-de-negocio.md item 6
ESCOPO: testar criar (201, 2 lancamentos gerados), editar (propagacao conforme decisao aprovada), desativar/reativar (reativar gera de novo, idempotente), listar com filtro ativa, id inexistente -> 404/400.
ARQUIVOS PERMITIDOS: `MyFinances\MyFinances.Tests\Controllers\ContaFixaControllerTests.cs` (novo)
NAO FAZER: nao alterar controller/service para o teste passar sem reportar.
RETORNO ESPERADO: testes passando; relatorio de bug se houver.

---

## TASK-061 — Style: revisao geral do modulo Conta Fixa

STATUS: CONCLUIDA + APROVADA PELO STYLE apos 2 rodadas (354/354 testes GREEN no final). Rodada 1: 3 problemas reais achados (falta validacao DiaVencimento/Valor causando 500 nao tratado; string magica decidindo status HTTP; null-forgiving sem guard em Listar) + 5 lacunas de teste. Rodada 2 (correcao): levi corrigiu os 3 problemas (validacao no Service, excecao tipada ContaFixaNaoEncontradaException substituindo string matching, guard em Listar) — nesse meio tempo repetiu por 2x a alteracao fora de escopo em TransferenciaResponse.ContaDestinoId ja vista na TASK-051/056, revertida por Kira ambas as vezes (commit 2f5e313); mike adicionou os 7 testes cobrindo os 5 cenarios. Rodada 2 do style: APROVADO, conferido com execucao propria da suite (354/354).
AGENT: style
FLUXO: Implementacao
DEPENDENCIAS: TASK-060
CONTEXTO A LER: regra-de-negocio.md item 6; clean-code.md inteiro
ESCOPO: revisar entidade/config/repository/service/controller contra regra e clean-code.
CRITERIO DE ACEITE: veredito final.
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito final do modulo backend.

---

## TASK-062 — Front: camada de dados (types/api/hooks) de Conta Fixa

STATUS: CONCLUIDA (build do frontend limpo, sem `any`; invalidacao de cache cruzada — lista e porId — apos criar/editar/desativar/reativar)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-059
CONTEXTO A LER: stack.md "Frontend (React)"; clean-code.md "Organizacao (React)"
ESCOPO: criar `features/contas-fixas/{types.ts,api.ts,query-keys.ts}` e hooks (`useContasFixas`, `useCriarContaFixa`, `useEditarContaFixa`, `useDesativarContaFixa`, `useReativarContaFixa`), seguindo o padrao ja usado em `features/contas-receber/`.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd\src\features\contas-fixas\types.ts` (novo), `api.ts` (novo), `query-keys.ts` (novo), `hooks/*.ts` (novo)
NAO FAZER: nao renderizar UI aqui.
RETORNO ESPERADO: hooks tipados, sem `any`, com invalidacao de cache apos criar/editar/desativar/reativar.

---

## TASK-063 — UI: listar contas fixas

STATUS: CONCLUIDA (build do frontend limpo. categoriaId omitido da tela — feature categorias/ ainda e so placeholder no projeto, sem lookup de nome; exibir UUID cru violaria identidade-visual.md, decisao documentada no componente. Kira tambem restaurou 2 arquivos de migration InitialCreate que tinham sido apagados/duplicados com outro timestamp por um processo anterior, nao relacionado a esta task, e encerrou um processo MyFinances.exe zumbi que travava o build)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-062
CONTEXTO A LER: identidade-visual.md (se existir); regra-de-negocio.md item 6
ESCOPO: tela listando ContaFixa (descricao, valor, dia_vencimento, categoria, status ativa/inativa).
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd\src\features\contas-fixas\ListaContasFixas.tsx` (novo), `components\ContaFixaItem.tsx` (novo)
NAO FAZER: nao calcular nada no componente.
RETORNO ESPERADO: componente consumindo `useContasFixas`.

---

## TASK-064 — UI: formulario criar/editar conta fixa

STATUS: CONCLUIDA (build limpo. Reaproveitou useContasParaSelecao de features/contas-receber/hooks/ via import cross-feature, ja que nao ha hook generico de listagem de contas no projeto — mesmo padrao ja documentado na TASK-014 de Contas a Receber; candidato a promover pra shared/hooks/ numa task futura, fora de escopo aqui. Achado relevante: EditarContaFixaRequest.categoriaId e substituicao total no backend, entao o form em modo edicao reenvia o categoriaId ja existente da ContaFixa em vez de omitir, evitando apagar categoria vinculada so por editar valor/dia_vencimento. Campo de conta de origem e descricao nao ofertados em modo edicao, coerente com o contrato do DTO)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-062
CONTEXTO A LER: identidade-visual.md; regra-de-negocio.md item 6
ESCOPO: formulario com descricao, valor, dia_vencimento (1-31), conta de origem, categoria opcional.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd\src\features\contas-fixas\FormContaFixa.tsx` (novo), `lib\validarContaFixa.ts` (novo)
NAO FAZER: nao permitir editar Ativa neste form (isso e acao separada, TASK-065).
RETORNO ESPERADO: componente chamando `useCriarContaFixa`/`useEditarContaFixa` conforme o modo.

---

## TASK-065 — UI: acao desativar/reativar

STATUS: CONCLUIDA (build limpo. Desativar tem confirmacao inline (dois botoes sim/nao) por ser destrutivo — exclui Lancamentos PENDENTE ja gerados; Reativar e clique direto com aviso via title, ja que so gera lancamento novo, sem apagar nada. Loading e erro tratados no mesmo padrao de FormContaFixa.tsx. Modulo Conta Fixa (DEMANDA-002) fechado: TASK-051 a TASK-065 todas concluidas)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-062, TASK-063
CONTEXTO A LER: regra-de-negocio.md item 6
ESCOPO: acao no item da lista para desativar/reativar, com aviso de que reativar gera novos lancamentos.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd\src\features\contas-fixas\components\ContaFixaItem.tsx`
NAO FAZER: nao assumir comportamento de lancamentos ja gerados ao desativar (regra omissa) — so exibir o toggle, sem prometer nada na UI sobre o que acontece com pendentes existentes.
RETORNO ESPERADO: componente chamando `useDesativarContaFixa`/`useReativarContaFixa`.

---

## Mapa de dependencia (TASK-051 a TASK-065)

```
051 (entidade) -> 052 (FK Lancamento) -> 053 (repo) -> 054 (esqueleto)
  -> 055 (RED) -> 056 (GREEN, bloqueada por duvidas 1/2/3) -> 057 (confirma GREEN)
  -> 058 (style critico) -> 059 (controller) -> 060 (testes HTTP) -> 061 (style geral)
  -> 062 (front data) -> 063 (lista) / 064 (form) -> 065 (acao desativar/reativar)
```

## Pendencias — resolvidas com o usuario em 2026-07-20

As 3 duvidas que bloqueavam TASK-056/059 (tipo do lancamento gerado,
propagacao de edicao, comportamento ao desativar) foram respondidas —
ver secao "Decisoes do usuario em 2026-07-20" acima. Modulo fechado por
completo (TASK-051 a TASK-065, backend e front), PR aberto em 2026-07-23.
# Modulo Limite de Gasto por Categoria (v1)

Escopo confirmado: item 14 da regra-de-negocio.md (recem-adicionado, 2026-07-20).
Tabela `limite_gasto` ja existia no schema.dbml, orfa no codigo. Regra NAO e
critica (comparado a ContaReceberSaldoCalculator/ClassificacaoLancamentoService:
erro aqui produz numero errado numa tela, nunca bloqueia escrita nem corrompe
estado) — segue fluxo simples arquitetar -> codar -> testar -> style, sem
ciclo RED/GREEN formal.

## TASK-051 — Entidade LimiteGasto + enum PeriodoLimiteGasto + Configuration + migration

STATUS: CONCLUIDA (build limpo, migration AddLimiteGastoEntity gerada e conferida contra schema.dbml. Corrigido tambem, fora do escopo original: TransferenciaResponse.ContaDestinoId estava nao-nulavel mas o dominio ja era Guid? desde TASK-002 — bug pre-existente que quebrava o build da solucao inteira. levi tentou contornar com `!.Value`, que compilava mas quebraria em runtime para EMPRESTIMO (item 13). Kira corrigiu tornando o campo do DTO nulavel, alinhado ao dominio.)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: nenhuma
CONTEXTO A LER: regra-de-negocio.md item 14 (inteiro); schema.dbml tabela `limite_gasto`; `Domain/TipoConta.cs` (padrao de enum com ToStorageValue/FromStorageValue); `Infrastructure/Configurations/ContaReceberConfiguration.cs` (padrao de Configuration)
ESCOPO: criar enum `PeriodoLimiteGasto` (so `Mensal` por enquanto, extensoes ToStorageValue/FromStorageValue no padrao MAIUSCULO ja usado); entidade `LimiteGasto` (Id, CategoriaId, ValorLimite, Periodo, navegacao Categoria); `LimiteGastoConfiguration : IEntityTypeConfiguration<LimiteGasto>` com indice UNICO em CategoriaId (1:1) e FK `OnDelete(DeleteBehavior.Cascade)`; registrar `DbSet<LimiteGasto>` no `MyFinancesDbContext`; gerar migration.
CRITERIO DE ACEITE: projeto compila; migration aplicavel; tabela `limite_gasto` com indice unico em `categoria_id`.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Domain/LimiteGasto.cs (novo), MyFinances/MyFinances/Domain/PeriodoLimiteGasto.cs (novo), MyFinances/MyFinances/Infrastructure/Configurations/LimiteGastoConfiguration.cs (novo), MyFinances/MyFinances/Data/MyFinancesDbContext.cs, MyFinances/MyFinances/Migrations/**
NAO FAZER: nao criar Repository/Service ainda (TASK-053); nao permitir CategoriaId nulo nem tornar o indice unico opcional.
RETORNO ESPERADO: arquivos criados, build limpo, migration gerada e conferida contra schema.dbml.

---

## TASK-052 — Extensao de ILancamentoRepository: ListarPorCategoriasEPeriodo

STATUS: CONCLUIDA (build limpo, metodo filtra por lista de categorias + ano/mes seguindo o padrao de ListarParaProjecaoDoMes; sem tocar em arquivo fora do escopo)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: nenhuma
CONTEXTO A LER: `Repositories/ContaReceberRepository.cs` metodo `ListarParaProjecaoDoMes` (padrao exato de filtro `.Year == ano && .Month == mes`, sem construir range de data); regra-de-negocio.md item 14, bloco "Hierarquia" e formula `gasto_realizado_no_mes`
ESCOPO: adicionar `Task<IEnumerable<Lancamento>> ListarPorCategoriasEPeriodo(IEnumerable<Guid> categoriaIds, int ano, int mes)` em `ILancamentoRepository`/`LancamentoRepository`, filtrando `categoriaIds.Contains(l.CategoriaId) && l.Data.Year == ano && l.Data.Month == mes`. Recebe uma LISTA de ids (nao um so) porque o item 14 soma categoria-pai + subcategorias diretas quando a categoria-pai tem limite.
CRITERIO DE ACEITE: metodo novo aceita `IEnumerable<Guid>`, compila, filtra corretamente qualquer subconjunto de categorias no periodo informado.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Repositories/ILancamentoRepository.cs, MyFinances/MyFinances/Repositories/LancamentoRepository.cs
NAO FAZER: nao filtrar Tipo/Oculto aqui (responsabilidade do `LimiteGastoCalculator`, TASK-053); nao remover metodos existentes.
RETORNO ESPERADO: metodo novo, compilando.

---

## TASK-053 — LimiteGastoCalculator + Repository + Service de LimiteGasto (com agregacao de hierarquia)

STATUS: CONCLUIDA (build limpo, revisado por Kira: upsert nao duplica, valida categoria Despesa nao-arquivada, hierarquia soma so 1 nivel de subcategorias)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-051, TASK-052
CONTEXTO A LER: regra-de-negocio.md item 14 inteiro; `Domain/FaturaSaldoCalculator.cs` e `Domain/ContaReceberSaldoCalculator.cs` (padrao de calculator estatico + record de resultado); `Services/ContaReceberService.cs` metodo `CalcularTotalAReceberEsperadoNoMes` (padrao de agregacao por mes); `Repositories/CategoriaRepository.cs` metodo `ObterPorId` (ja inclui `Subcategorias` via `Include`)
ESCOPO: implementar `Domain/LimiteGastoCalculator.Calcular(LimiteGasto, IEnumerable<Lancamento>)` retornando `record LimiteGastoStatus(decimal ValorLimite, decimal GastoRealizado, decimal PercentualUtilizado, bool Estourado)` (gasto = soma de `Tipo=Debit` e `!Oculto` do conjunto ja filtrado); `ILimiteGastoRepository`/`LimiteGastoRepository` (Adicionar, ObterPorCategoriaId, Listar, Remover, Salvar); `ILimiteGastoService`/`LimiteGastoService` com `Definir` (upsert — valida categoria existe via `ICategoriaRepository`, `categoria.Tipo == Despesa`, categoria nao arquivada, `valor > 0`), `Remover`, `Listar`, `ObterGastoVsLimite(categoriaId, ano, mes)` — resolve a categoria via `ICategoriaRepository.ObterPorId` (que ja inclui `Subcategorias`), monta a lista de ids `[categoria.Id, ...categoria.Subcategorias.Select(s => s.Id)]`, chama `ListarPorCategoriasEPeriodo` com essa lista —, e `ObterGastoVsLimiteTodasCategorias(ano, mes)`. Criar `Exceptions/LimiteGastoNaoEncontradoException.cs` e `Exceptions/CategoriaInvalidaParaLimiteGastoException.cs`. Registrar tudo no DI.
CRITERIO DE ACEITE: 1) `Definir` em categoria tipo=Receita lanca `CategoriaInvalidaParaLimiteGastoException`; 2) `Definir` duas vezes na mesma categoria atualiza em vez de duplicar; 3) `ObterGastoVsLimite` de uma categoria-pai soma tanto os lancamentos da propria categoria quanto os de suas subcategorias diretas (Debit, nao-oculto), sem descer alem de 1 nivel.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Domain/LimiteGastoCalculator.cs (novo), MyFinances/MyFinances/Repositories/ILimiteGastoRepository.cs (novo), MyFinances/MyFinances/Repositories/LimiteGastoRepository.cs (novo), MyFinances/MyFinances/Services/ILimiteGastoService.cs (novo), MyFinances/MyFinances/Services/LimiteGastoService.cs (novo), MyFinances/MyFinances/Exceptions/LimiteGastoNaoEncontradoException.cs (novo), MyFinances/MyFinances/Exceptions/CategoriaInvalidaParaLimiteGastoException.cs (novo), MyFinances/MyFinances/Program.cs
NAO FAZER: nao permitir `Definir` em categoria tipo=Receita ou arquivada; nao bloquear/rejeitar criacao de lancamento com base no limite (item 14 proibe bloqueio); nao acessar `MyFinancesDbContext` direto no Service; nao somar subcategoria-de-subcategoria (so 1 nivel de profundidade, conforme item 14).
RETORNO ESPERADO: arquivos criados, build limpo.

---

## TASK-054 — Controller LimitesGastoController + DTOs

STATUS: CONCLUIDA (5 endpoints + DTOs, build limpo. Kira corrigiu bug encontrado na revisao: LimiteGastoService.Definir nao atribuia Categoria ao criar um LimiteGasto novo, deixando CategoriaNome vazio na resposta 201 - agora atribui a categoria ja validada.)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-053
CONTEXTO A LER: clean-code.md "Organizacao (.NET)"; `Controllers/ContasReceberController.cs` (padrao de traducao de excecao -> status HTTP e DTO factory)
ESCOPO: criar `LimitesGastoController` com `POST /api/limites-gasto` (upsert, 200 se atualizou / 201 se criou), `DELETE /api/limites-gasto/{categoriaId}` (204), `GET /api/limites-gasto` (200), `GET /api/limites-gasto/gasto-vs-limite?ano=&mes=` (200), `GET /api/limites-gasto/gasto-vs-limite/{categoriaId}?ano=&mes=` (200/404). DTOs: `DefinirLimiteGastoRequest`, `LimiteGastoResponse` (com `CategoriaNome`), `GastoVsLimiteResponse` (com `PercentualUtilizado` e `Estourado`).
CRITERIO DE ACEITE: os 5 endpoints compilam e respondem os status HTTP descritos no ESCOPO.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Controllers/LimitesGastoController.cs (novo), MyFinances/MyFinances/DTOs/LimiteGasto/*.cs (novo)
NAO FAZER: nao colocar regra de negocio no controller; nao expor a entity `LimiteGasto` crua.
RETORNO ESPERADO: endpoints funcionando conforme criterio.

---

## TASK-055 — Testes de LimiteGastoCalculator e LimiteGastoService (incluindo hierarquia)

STATUS: CONCLUIDA (29 testes, 0 falhas, confirmado independentemente por Kira via dotnet test --filter. Cobre soma Debit/Oculto, estourado, percentual sem divisao por zero, upsert nao duplica, categoria Receita/arquivada rejeitada, hierarquia pai+subcategoria com Verify explicito da lista de ids passada ao repository)
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-053
CONTEXTO A LER: regra-de-negocio.md item 14 inteiro
ESCOPO: testar `LimiteGastoCalculator.Calcular`: soma so Debit+!Oculto, ignora Credit, ignora Oculto=true, `Estourado=true` quando gasto>limite, `PercentualUtilizado` correto (limite=0 sem divisao por zero). Testar `LimiteGastoService`: `Definir` em categoria Receita rejeita; `Definir` duas vezes atualiza (nao duplica); `Definir` valor<=0 rejeita; `Remover` categoria sem limite lanca `LimiteGastoNaoEncontradoException`; `ObterGastoVsLimiteTodasCategorias` retorna so categorias com limite cadastrado; `ObterGastoVsLimite` de categoria-pai com limite soma tambem os lancamentos de suas subcategorias diretas; gasto de uma subcategoria NAO soma no limite de outra subcategoria irma (so no pai).
CRITERIO DE ACEITE: testes passando cobrindo os casos listados, incluindo hierarquia; relatorio de bug estruturado (arquivo+linha) se algum falhar por defeito de codigo.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances.Tests/Domain/LimiteGastoCalculatorTests.cs (novo), MyFinances/MyFinances.Tests/Services/LimiteGastoServiceTests.cs (novo)
NAO FAZER: nao alterar `LimiteGastoCalculator`/`LimiteGastoService` para o teste passar.
RETORNO ESPERADO: testes passando ou relatorio de bug estruturado.

---

## TASK-056 — Testes HTTP do LimitesGastoController

STATUS: CONCLUIDA (6 testes HTTP, 0 falhas, confirmado independentemente por Kira. Cobre 201/200 upsert com CategoriaNome preenchido, 422 categoria Receita, 404 delete/consulta sem limite, estourado=true via gasto real. Removido arquivo residual test-output.txt deixado pelo mike, fora do escopo)
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-054
CONTEXTO A LER: `MyFinances.Tests/Controllers/ContaReceberControllerTests.cs` (padrao WebApplicationFactory + InMemory DB + JWT ja usado no projeto)
ESCOPO: testes HTTP: criar limite (201); redefinir limite existente (200, nao duplica); criar em categoria Receita (422); `DELETE` sem limite cadastrado (404); `GET gasto-vs-limite` retornando `estourado=true` quando gasto>limite; `GET gasto-vs-limite/{categoriaId}` para categoria sem limite (404).
CRITERIO DE ACEITE: testes passando; relatorio de bug se houver.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances.Tests/Controllers/LimitesGastoControllerTests.cs (novo)
NAO FAZER: nao alterar controller/service para o teste passar sem reportar.
RETORNO ESPERADO: testes passando ou relatorio de bug.

---

## TASK-057 — Style: revisao do modulo backend LimiteGasto

STATUS: CONCLUIDA — APROVADO (rodada 2). Rodada 1 reprovou por Controller fazer Listar() full-scan pra decidir 200/201 e pra achar CategoriaNome; levi corrigiu fazendo Definir/ObterGastoVsLimite retornarem tupla com o dado ja resolvido (mesmo padrao que ObterGastoVsLimiteTodasCategorias ja usava). Style confirmou 359/359 testes, sem regressao nem problema de camada novo. Backend do modulo fechado, liberado pro front.
AGENT: style
FLUXO: Implementacao
DEPENDENCIAS: TASK-055, TASK-056
CONTEXTO A LER: regra-de-negocio.md item 14; clean-code.md inteiro
ESCOPO: validar que nenhum fluxo bloqueia lancamento por causa do limite (item 14: so alerta), que `LimiteGastoService` nao acessa `DbContext` direto, que o upsert de `Definir` nao duplica limite por categoria, e que o calculo de gasto realizado bate com o item 14 (Debit, !Oculto, mes calendario, hierarquia de 1 nivel).
CRITERIO DE ACEITE: veredito (APROVADO ou tarefa de correcao no esquema padrao, redespachada a levi).
ARQUIVOS PERMITIDOS: nenhum (style nao edita)
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito final do backend do modulo.

---

## TASK-058 — Front: camada de dados de LimiteGasto (types/api/hooks)

STATUS: CONCLUIDA (types/api/query-keys/hooks criados, build/type-check limpo, dados crus sem threshold de UX embutido, invalidacao cruzada via limiteGastoKeys.all nas mutations)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-057
CONTEXTO A LER: stack.md secao "Frontend (React)"; `features/contas-receber/{types.ts,api.ts,query-keys.ts,hooks}` como padrao de estilo
ESCOPO: criar feature nova `features/limite-gasto/` com `types.ts`, `api.ts`, `query-keys.ts` e hooks (`useLimitesGasto`, `useDefinirLimiteGasto`, `useRemoverLimiteGasto`, `useGastoVsLimite(categoriaId, ano, mes)`, `useGastoVsLimiteTodasCategorias(ano, mes)`), consumidos depois por dashboard, lancamentos, categorias e o relatorio (TASK-059 a TASK-062).
CRITERIO DE ACEITE: hooks tipados (sem `any`), invalidacao de cache cruzada apos `Definir`/`Remover` (gasto-vs-limite muda).
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/limite-gasto/types.ts (novo), MyFinanceFrontEnd/src/features/limite-gasto/api.ts (novo), MyFinanceFrontEnd/src/features/limite-gasto/query-keys.ts (novo), MyFinanceFrontEnd/src/features/limite-gasto/hooks/*.ts (novo)
NAO FAZER: nao renderizar UI aqui; nao fixar threshold de "perto do limite" no hook — expor `percentualUtilizado` cru.
RETORNO ESPERADO: hooks prontos para consumo pelas tasks seguintes.

---

## TASK-059 — Front: indicador de limite no dashboard

STATUS: CONCLUIDA (componente standalone, estados loading/erro/vazio, tokens de identidade visual corretos, sem calculo de dominio no front)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-058
CONTEXTO A LER: identidade-visual.md; regra-de-negocio.md item 14
ESCOPO: componente `LimiteGastoIndicador` (barra de progresso gasto/limite por categoria, cor de alerta quando `estourado=true`), consumindo `useGastoVsLimiteTodasCategorias`. ATENCAO: nao existe pagina raiz de Dashboard no front ainda (`features/dashboard/` so tem `.gitkeep`) — esta task entrega o componente pronto para embutir; a integracao na pagina de dashboard depende de um modulo de Dashboard ainda nao arquitetado.
CRITERIO DE ACEITE: componente de apresentacao puro, consumindo o hook, pronto para ser embutido quando o Dashboard existir.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/dashboard/components/LimiteGastoIndicador.tsx (novo)
NAO FAZER: nao construir a pagina de Dashboard inteira (fora de escopo); nao calcular gasto/limite no componente.
RETORNO ESPERADO: componente pronto para embutir.

---

## TASK-060 — Front: aviso de limite na tela de lancamento

STATUS: CONCLUIDA (funcao pura de threshold + componente de aviso, nunca bloqueia submit, 404 tratado como ausencia silenciosa de aviso)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-058
CONTEXTO A LER: identidade-visual.md; regra-de-negocio.md item 14
ESCOPO: componente `AvisoLimiteGasto` (alerta "perto do limite"/"limite estourado" ao selecionar uma categoria), consumindo `useGastoVsLimite(categoriaId, ano, mes)`; funcao pura em `lib/limiarAlertaLimite.ts` decidindo o threshold visual de "perto" a partir de `percentualUtilizado` (default 80% — nao esta na regra de negocio, decisao de UX isolada em funcao propria para poder ajustar depois sem tocar em contrato de API). ATENCAO: nao existe form de criacao de lancamento manual no front ainda (`features/lancamentos/` so tem `.gitkeep`, embora o backend — `LancamentoManualService`/Controller — ja exista) — esta task entrega o componente pronto para embutir quando o form existir, nao constroi o form.
CRITERIO DE ACEITE: componente exibe estado "ok"/"perto"/"estourado" a partir de `percentualUtilizado`/`estourado`; funcao de threshold isolada e testavel.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/lancamentos/components/AvisoLimiteGasto.tsx (novo), MyFinanceFrontEnd/src/features/lancamentos/lib/limiarAlertaLimite.ts (novo)
NAO FAZER: nao bloquear o submit do formulario por causa do limite (item 14: so alerta); nao construir o form de lancamento inteiro (fora de escopo).
RETORNO ESPERADO: componente pronto para embutir.

---

## TASK-061 — Front: comparativo limite vs realizado por categoria

STATUS: CONCLUIDA (pagina + rota /limites-gasto, confirmado que features/cartao/RelatorioCategoriaPage.tsx e arquivos relacionados nao foram tocados)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-058
CONTEXTO A LER: identidade-visual.md; regra-de-negocio.md item 14; `features/cartao/RelatorioCategoriaPage.tsx` + `features/cartao/hooks/useRelatorioCategoria.ts` — ACHADO: ja existe uma tela "Relatorio por categoria" no front, mas ela e escopada so a COMPRAS DE CARTAO (item 12) e chama um endpoint que nao existe no backend (gap documentado no proprio arquivo, pre-existente a esta entrega). O comparativo de limite (item 14) e um relatorio DIFERENTE (soma TODO Debit da categoria, nao so cartao) e usa o endpoint novo desta leva — NAO tentar consertar/fundir com `RelatorioCategoriaPage.tsx` aqui.
ESCOPO: pagina/secao nova listando as categorias com limite cadastrado, comparando `valorLimite` x `gastoRealizado` (barra de progresso + `estourado`), consumindo `useGastoVsLimiteTodasCategorias`.
CRITERIO DE ACEITE: pagina nova, rota propria, sem tocar no relatorio de cartao existente.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/limite-gasto/ComparativoLimiteGastoPage.tsx (novo), MyFinanceFrontEnd/src/features/limite-gasto/components/ItemComparativoLimite.tsx (novo), MyFinanceFrontEnd/src/app/routes.tsx (so para adicionar a rota nova)
NAO FAZER: nao alterar `features/cartao/RelatorioCategoriaPage.tsx` nem seus arquivos (`api.ts`, `hooks/useRelatorioCategoria.ts`, `lib/relatorioCategoria.ts`) — gap pre-existente, fora de escopo; nao remover nem duplicar essa rota.
RETORNO ESPERADO: pagina nova funcionando, sem tocar no relatorio de cartao.

---

## TASK-062 — Front: CRUD do limite dentro da tela de categoria

STATUS: CONCLUIDA (campo oculto pra RECEITA, estados sem-limite/com-limite tratados, define/edita/remove via mutations)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-058
CONTEXTO A LER: identidade-visual.md; regra-de-negocio.md item 14 (bloco "Onde aparece" — CRUD confirmado embutido na tela de categoria, decisao do usuario em 2026-07-20); `features/categorias/` hoje so tem `.gitkeep` (nao ha form de categoria construido ainda) — esta task constroi tambem o campo de limite dentro do que seria o form/lista de categoria.
ESCOPO: campo "Limite de gasto mensal" na tela de categoria (tipo=Despesa apenas — campo oculto/desabilitado para Receita), com acao de definir/remover, consumindo `useDefinirLimiteGasto`/`useRemoverLimiteGasto`.
CRITERIO DE ACEITE: componente oculta o campo quando a categoria e Receita; chama `Definir`/`Remover` corretamente.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/categorias/components/CampoLimiteGasto.tsx (novo)
NAO FAZER: nao construir a tela de categoria inteira (CRUD de categoria em si e modulo separado, nao arquitetado nesta leva) — so o componente do campo de limite, isolado, pronto para embutir.
RETORNO ESPERADO: componente pronto para embutir.

---

## Mapa de dependencia (TASK-051 a TASK-062)

```
051 (entidade) ─┬─> 053 (calculator+repo+service) ─┬─> 054 (controller) ─┬─> 055 (testes service) ─┬─> 057 (style) ─> 058 (front data) ─┬─> 059 (dashboard)
052 (repo lanc) ┘                                   │                    └─> 056 (testes HTTP) ─────┘                                    ├─> 060 (aviso lancamento)
                                                     └────────────────────────────────────────────────────────────────────────────────────┼─> 061 (comparativo)
                                                                                                                                             └─> 062 (CRUD em categoria)
```
051 e 052 tocam arquivos disjuntos, rodam em paralelo. 059/060/061/062 idem entre si — todas dependem so de 058.

## Pendencias — resolvidas com o usuario em 2026-07-20

1. **Estouro de limite = so alerta visual, sem bloqueio.** Decisao do usuario;
   nenhum service/controller deste modulo pode recusar um lancamento por
   causa do limite.
2. **Limite so em categoria tipo DESPESA.** `Definir` em categoria RECEITA
   lanca `CategoriaInvalidaParaLimiteGastoException` (TASK-053).
3. **Hierarquia: gasto de subcategoria SOMA no limite da categoria-pai**
   (alem do limite proprio da subcategoria, se houver). Decisao do usuario —
   inverteu a suposicao inicial do killua (que era "independentes"). Reflete
   em TASK-052 (repository aceita lista de ids), TASK-053 (service resolve
   subcategorias via `Categoria.Subcategorias`) e TASK-055 (teste especifico
   de hierarquia).
4. **Regime de competencia** (lancamento conta ao ser registrado, independente
   de status PENDENTE/PAGO) — confirmado, mesma filosofia do item 12.
5. **CRUD do valor do limite embutido na tela de categoria** (nao ha tela
   separada de "Limites") — confirmado, TASK-062.
6. **Threshold de "perto do limite" (80%) na tela de lancamento** — decisao
   de UX, nao de regra de negocio; fica isolada em `lib/limiarAlertaLimite.ts`
   (TASK-060) para poder mudar sem tocar em contrato de API.

Nenhuma pendencia de decisao de produto restante. Queue pronta para execucao.

---

# Modulo Projecao do Mes (dashboard, item 9) — decomposto por killua em 2026-07-20

Gerado por killua em 2026-07-20, worktree `lancamento-geral-task039`. Fórmula:

```
saldo_projetado = (total_recebido_no_mes + total_a_receber_esperado_no_mes)
                  - (total_pago_no_mes + total_a_pagar_no_mes)
```

| Termo | Fonte | Status |
|---|---|---|
| `total_a_receber_esperado_no_mes` | `ContaReceberService.CalcularTotalAReceberEsperadoNoMes` | JA EXISTE |
| `total_recebido_no_mes` | Lancamento generico, Credit/Pago, exclui Transferencia/compra cartao | FALTA |
| `total_pago_no_mes` | Lancamento generico Debit/Pago (mesma exclusao) + fatura do mes ja paga | FALTA |
| `total_a_pagar_no_mes` | Lancamento generico Debit/Pendente (mesma exclusao) + fatura do mes nao paga | FALTA |

Conta Fixa (item 6) NAO existe no codebase (so a FK morta `conta_fixa_id` em
`Lancamento`) — nao bloqueia esta decomposicao (quando existir, so vai gerar
`Lancamento` comuns que o agregador generico ja soma), mas nenhuma conta fixa
aparece na projecao ate esse modulo ser construido a parte.

## Esqueleto compilavel (killua entrega, Kira materializa antes do RED)

`Repositories/ILancamentoRepository.cs` (nova assinatura):
```csharp
Task<IEnumerable<Lancamento>> ListarParaFluxoCaixaDoMes(int ano, int mes);
```

`Repositories/IFaturaRepository.cs` (nova assinatura):
```csharp
Task<IEnumerable<Fatura>> ListarFaturasCartaoPorVencimentoNoMes(int ano, int mes);
```

`Services/IFluxoCaixaService.cs` (adiciona 3 metodos ao contrato existente):
```csharp
Task<decimal> CalcularTotalRecebidoNoMes(int ano, int mes);
Task<decimal> CalcularTotalPagoNoMes(int ano, int mes);
Task<decimal> CalcularTotalAPagarNoMes(int ano, int mes);
```
`FluxoCaixaService.cs`: os 3 corpos novos lancam `NotImplementedException`;
`ListarFluxoCaixa` existente fica intocado.

`Services/IFaturaProjecaoService.cs` (novo):
```csharp
public record FaturaProjecaoMes(decimal TotalPago, decimal TotalNaoPago);

public interface IFaturaProjecaoService
{
    Task<FaturaProjecaoMes> CalcularProjecaoCartaoDoMes(int ano, int mes);
}
```

`Services/FaturaProjecaoService.cs` (novo, corpo `NotImplementedException`,
DI de `IFaturaRepository`).

`Services/IProjecaoMesService.cs` (novo):
```csharp
public record ProjecaoMesResultado(
    int Ano, int Mes,
    decimal TotalRecebidoNoMes, decimal TotalAReceberEsperadoNoMes,
    decimal TotalPagoNoMes, decimal TotalAPagarNoMes,
    decimal SaldoProjetado);

public interface IProjecaoMesService
{
    Task<ProjecaoMesResultado> CalcularProjecaoDoMes(int ano, int mes);
}
```

`Services/ProjecaoMesService.cs` (novo, corpo `NotImplementedException`, DI
de `IFluxoCaixaService` + `IContaReceberService` + `IFaturaProjecaoService`).

## TASK-063 — Repository: agregacao mensal de lancamentos p/ fluxo de caixa

STATUS: CONCLUIDA (330/330 testes GREEN, build limpo. Achado colateral importante: main estava com erro de compilacao real (CS0266) em TransferenciaResponse.cs -- Transferencia.ContaDestinoId virou Guid? num commit anterior de Contas a Receber, item 13, e o DTO nao acompanhou; levi corrigiu certo. Desvio de escopo: levi tambem adicionou filtro `!TransferenciaId.HasValue` em ListarParaFluxoCaixaDoMes, contra o NAO FAZER explicito da task -- Kira removeu o filtro e inverteu o teste correspondente para provar que o repository devolve a lista crua do mes, sem classificacao de negocio; isso fica pro Service em TASK-065/066)
AGENT: levi
DEPENDENCIAS: nenhuma
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 9 (formula) e item 3 (exclusao de transferencia); stack.md secao Repositories/
ESCOPO: Adicionar `ListarParaFluxoCaixaDoMes(int ano, int mes)` em `ILancamentoRepository`/`LancamentoRepository`, mesmo filtro de `ListarParaFluxoCaixa` (`FaturaId == null`, `!Oculto`) restrito a `Data.Year==ano && Data.Month==mes`; NAO filtrar Transferencia aqui (fica a cargo do Service).
CRITERIO DE ACEITE:
1. Retorna so lancamentos do mes/ano informado com `FaturaId` nulo e `Oculto=false`.
2. Nao aplica nenhuma logica de classificacao (isso e do Service).
3. Assinatura identica ao esqueleto do killua.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Repositories/ILancamentoRepository.cs`, `MyFinances/MyFinances/Repositories/LancamentoRepository.cs`, `MyFinances.Tests/Repositories/LancamentoRepositoryTests.cs` (criar se nao existir)
NAO FAZER: nao mexer em `ListarParaFluxoCaixa` existente; nao excluir Transferencia aqui.
RETORNO ESPERADO: diff dos arquivos + confirmacao que o projeto compila (`dotnet build`).

---

## TASK-064 — Repository: faturas de cartao por vencimento no mes

STATUS: CONCLUIDA (335/335 testes GREEN, build limpo. Escopo respeitado, sem desvios)
AGENT: levi
DEPENDENCIAS: nenhuma
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 12 (fatura, ciclo, saldo); stack.md secao Repositories/
ESCOPO: Adicionar `ListarFaturasCartaoPorVencimentoNoMes(int ano, int mes)` em `IFaturaRepository`/`FaturaRepository`: join com Conta (`Tipo == Cartao`), filtro `DataVencimento.Year==ano && Month==mes`, `Include(Lancamentos)` e `Include(Transferencias)` (necessario para `FaturaSaldoCalculator`).
CRITERIO DE ACEITE:
1. So retorna faturas de contas `Tipo=Cartao`.
2. Filtro por `DataVencimento` no ano/mes.
3. `Lancamentos`/`Transferencias` vem carregados (sem lazy loading quebrado).
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Repositories/IFaturaRepository.cs`, `MyFinances/MyFinances/Repositories/FaturaRepository.cs`, `MyFinances.Tests/Repositories/FaturaRepositoryTests.cs` (criar se nao existir)
NAO FAZER: nao mexer nos metodos existentes de `FaturaRepository`.
RETORNO ESPERADO: diff dos arquivos + confirmacao de build.

---

## TASK-065 — [RED] Testes de agregacao mensal do FluxoCaixaService

STATUS: CONCLUIDA (18 testes novos, RED confirmado por NotImplementedException, 6 testes existentes de ListarFluxoCaixa continuam GREEN. Cobre soma por Tipo/Status e exclusao de Transferencia inclusive emprestimo)
AGENT: mike
DEPENDENCIAS: TASK-063
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 9 (formula completa), item 3 (exclusao de transferencia mesma titularidade), item 12 (compra de cartao nao entra no fluxo de caixa geral); esqueleto `IFluxoCaixaService.cs` (secao acima)
ESCOPO: Escrever testes para `CalcularTotalRecebidoNoMes`, `CalcularTotalPagoNoMes` e `CalcularTotalAPagarNoMes` cobrindo: soma so Credit/Pago (recebido); so Debit/Pago (pago); so Debit/Pendente (a pagar); exclui lancamento com `TransferenciaId` setado (transferencia comum); exclui lancamento com `FaturaId` setado (compra de cartao); ignora lancamento fora do mes/ano pedido; lista vazia retorna 0.
CRITERIO DE ACEITE:
1. Todos os testes compilam contra o esqueleto (mock de `ILancamentoRepository.ListarParaFluxoCaixaDoMes`).
2. `dotnet test --filter FullyQualifiedName~FluxoCaixaServiceTests` da RED por `NotImplementedException`, nunca erro de compilacao.
3. Cobre os 3 metodos.
ARQUIVOS PERMITIDOS: `MyFinances.Tests/Services/FluxoCaixaServiceTests.cs` (estender o arquivo existente)
NAO FAZER: nao implementar os metodos reais; nao mexer em producao.
RETORNO ESPERADO: arquivo de teste + output do `dotnet test` confirmando RED.

---

## TASK-066 — [GREEN] Implementar agregacao mensal do FluxoCaixaService

STATUS: CONCLUIDA (18/18 testes RED da TASK-065 GREEN, suite completa 353/353. Reusa ClassificacaoLancamentoService, sem duplicar checagem de TransferenciaId)
AGENT: levi
DEPENDENCIAS: TASK-065
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 9, item 3; arquivo de teste de TASK-065 (leitura, nunca escrita)
ESCOPO: Implementar os 3 metodos usando `ListarParaFluxoCaixaDoMes` + `ClassificacaoLancamentoService.Classificar` para excluir Transferencia, somando por Tipo/Status conforme a formula.
CRITERIO DE ACEITE:
1. Testes de TASK-065 ficam GREEN sem alterar o arquivo de teste.
2. Nenhuma logica de exclusao de Transferencia fica implicita/duplicada — reusa `ClassificacaoLancamentoService`.
3. Nenhum acesso a `DbContext` direto (so via `ILancamentoRepository`).
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Services/FluxoCaixaService.cs`
NAO FAZER: nao editar `MyFinances.Tests/Services/FluxoCaixaServiceTests.cs`.
RETORNO ESPERADO: diff do `FluxoCaixaService.cs`.

---

## TASK-067 — [GREEN confirmado] Rodar testes do FluxoCaixaService

STATUS: CONCLUIDA (Kira confirmou inline ao rodar dotnet test antes de commitar a TASK-066 — 353/353 GREEN, sem reescrever nenhum teste. Redispatch de mike dispensado por ser a mesma verificacao ja feita)
AGENT: mike
DEPENDENCIAS: TASK-066
FLUXO: Implementacao
CONTEXTO A LER: arquivo de teste de TASK-065
ESCOPO: Rodar os testes de `FluxoCaixaServiceTests`, sem reescrever nenhum teste.
CRITERIO DE ACEITE: todos GREEN; se algum falhar, reportar bug (nao corrigir).
ARQUIVOS PERMITIDOS: nenhum (so execucao)
NAO FAZER: nao editar nenhum arquivo.
RETORNO ESPERADO: relatorio GREEN ou lista de falhas com stack trace.

---

## TASK-068 — Style: revisao do FluxoCaixaService

STATUS: CONCLUIDA + APROVADA PELO STYLE apos 2 rodadas (353/353 testes GREEN no final). Rodada 1: apontou duplicacao real entre os 3 metodos de agregacao (mesma logica de exclusao de Transferencia copiada 3x); levi extraiu `SomarLancamentosDoMes` privado. Rodada 2: APROVADO — extracao mecanica, sem mudanca de comportamento, assinatura publica intacta. Achado paralelo nao bloqueante: 2 testes de "emprestimo" em FluxoCaixaServiceTests.cs (linhas ~376-424 e ~910-958) descrevem modelagem que nao bate com item 13 (recebimento deveria usar ContaReceberId, nao TransferenciaId; saida deveria ser sempre Pago, nunca Pendente) — funcionalmente inofensivo, mas documentacao de teste enganosa; registrado como pendencia separada, nao decidido ainda
AGENT: style
DEPENDENCIAS: TASK-067
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 9, item 3; clean-code.md
ESCOPO: Revisar `FluxoCaixaService.cs` contra regra de negocio e clean-code, atencao especifica a exclusao de Transferencia (double counting).
CRITERIO DE ACEITE: veredito APROVADO ou tarefa de correcao no esquema padrao.
ARQUIVOS PERMITIDOS: nenhum (style nao edita)
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + (se reprovado) tarefa de correcao redespachada a levi.

---

## TASK-069 — [RED] Testes do FaturaProjecaoService

STATUS: CONCLUIDA (7 testes, RED confirmado por NotImplementedException. Cobre fatura Paga, Aberta/Fechada sem pagamento, Aberta/Fechada com pagamento parcial (fracionamento), multiplos cartoes, mes sem fatura)
AGENT: mike
DEPENDENCIAS: TASK-064
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 12 (fatura, status Aberta/Fechada/Paga, pagamento parcial, `FaturaSaldoCalculator`) e item 9 ("cartao entra como UMA conta a pagar"); esqueleto `IFaturaProjecaoService.cs`/`FaturaProjecaoService.cs` (secao acima); decisao do usuario em 2026-07-20 (ver "Decisoes resolvidas" no fim do arquivo): fatura parcialmente paga e FRACIONADA (nao binaria), fatura do mes = `DataVencimento` no ano/mes, multiplos cartoes SOMAM.
ESCOPO: Testar `CalcularProjecaoCartaoDoMes`: para cada fatura de cartao com `DataVencimento` no mes/ano, soma `ValorPago` (ou `ValorTotal - SaldoPendente`, via `FaturaSaldoCalculator`) em `TotalPago` e `SaldoPendente` em `TotalNaoPago` — inclusive fatura `Status=Aberta`/`Fechada` com pagamento parcial ja registrado; fatura `Status=Paga` soma `ValorTotal` inteiro em `TotalPago` (`SaldoPendente=0`); multiplas faturas de multiplos cartoes no mesmo mes somam nos mesmos 2 totais (sem breakdown por cartao); mes sem fatura retorna `(0,0)`.
CRITERIO DE ACEITE:
1. Compila contra o esqueleto.
2. RED por `NotImplementedException`.
3. Cobre: fatura paga, fatura aberta sem pagamento, fatura aberta com pagamento parcial (fracionamento provado), 2 cartoes no mesmo mes (soma), ausencia de fatura no mes.
ARQUIVOS PERMITIDOS: `MyFinances.Tests/Services/FaturaProjecaoServiceTests.cs` (criar)
NAO FAZER: nao implementar o service; nao tratar fatura parcial como binaria.
RETORNO ESPERADO: arquivo de teste + output RED.

---

## TASK-070 — [GREEN] Implementar FaturaProjecaoService

STATUS: CONCLUIDA (7/7 testes RED da TASK-069 GREEN, suite completa 360/360. Reusa FaturaSaldoCalculator, sem reimplementar calculo de saldo)
AGENT: levi
DEPENDENCIAS: TASK-069
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 12; arquivo de teste de TASK-069 (leitura)
ESCOPO: Implementar `CalcularProjecaoCartaoDoMes` usando `IFaturaRepository.ListarFaturasCartaoPorVencimentoNoMes` + `FaturaSaldoCalculator.Calcular` por fatura, somando `ValorPago`/`ValorTotal-SaldoPendente` em `TotalPago` e `SaldoPendente` em `TotalNaoPago` (fracionado, nunca binario por `Status`).
CRITERIO DE ACEITE:
1. Testes de TASK-069 GREEN sem editar o arquivo de teste.
2. Reusa `FaturaSaldoCalculator`, nao reimplementa calculo de saldo.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Services/FaturaProjecaoService.cs`
NAO FAZER: nao editar o arquivo de teste.
RETORNO ESPERADO: diff do `FaturaProjecaoService.cs`.

---

## TASK-071 — [GREEN confirmado] Rodar testes do FaturaProjecaoService

STATUS: CONCLUIDA (Kira confirmou inline ao rodar dotnet test antes de commitar a TASK-070 — 360/360 GREEN, sem reescrever nenhum teste)
AGENT: mike
DEPENDENCIAS: TASK-070
FLUXO: Implementacao
CONTEXTO A LER: arquivo de teste de TASK-069
ESCOPO: Rodar os testes, sem reescrever.
CRITERIO DE ACEITE: todos GREEN; falha vira relatorio de bug, nao correcao direta.
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao editar nenhum arquivo.
RETORNO ESPERADO: relatorio GREEN ou falhas.

---

## TASK-072 — Style: revisao do FaturaProjecaoService

STATUS: CONCLUIDA + APROVADA PELO STYLE apos 2 rodadas (361/361 testes GREEN no final). Rodada 1: achou acoplamento escondido -- o metodo confiava em fatura.Status pra decidir o calculo, so dando certo porque 3 arquivos externos (PagamentoFaturaService, CompraCartaoService, EstornoCartaoService) garantem ValorPago==ValorTotal quando Status=Paga, sem nenhum teste provando isso; tambem achou typo de PascalCase num nome de teste. Levi removeu o if/else (agora sempre usa saldo.ValorPago/ValorPendente do FaturaSaldoCalculator) e adicionou teste provando Status=Paga com saldo calculado divergente. Bonus: a correcao tambem eliminou um bug latente do if antigo (branch Paga nunca zerava totalNaoPago). Rodada 2: APROVADO, 8/8 testes do service GREEN
AGENT: style
DEPENDENCIAS: TASK-071
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 12; clean-code.md
ESCOPO: Revisar contra regra de negocio, atencao ao fracionamento correto de fatura parcialmente paga (nunca tratar como binario pago/nao-pago).
CRITERIO DE ACEITE: veredito ou tarefa de correcao.
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + tarefa de correcao se reprovado.

---

## TASK-073 — [RED] Testes do ProjecaoMesService (formula master)

STATUS: CONCLUIDA (6 testes, RED confirmado por NotImplementedException. Cobre composicao dos 4 termos, formula exata, saldo positivo/negativo/zero, contrato de chamada das 3 dependencias)
AGENT: mike
DEPENDENCIAS: TASK-068, TASK-072
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 9 INTEIRO (formula, regra de cartao, referencia a item 13); esqueleto `IProjecaoMesService.cs`/`ProjecaoMesService.cs` (secao acima)
ESCOPO: Testar `CalcularProjecaoDoMes`: compoe os 3 totais (`ContaReceberService`, `FluxoCaixaService`, `FaturaProjecaoService`) aplicando exatamente `saldo_projetado = (recebido + a_receber) - (pago + a_pagar)`, onde pago/a_pagar finais somam a fatia da fatura de cartao aos totais genericos de lancamento.
CRITERIO DE ACEITE:
1. Compila contra o esqueleto com mocks das 3 dependencias.
2. RED por `NotImplementedException`.
3. Pelo menos um caso cobrindo saldo negativo (mais a pagar que a receber).
ARQUIVOS PERMITIDOS: `MyFinances.Tests/Services/ProjecaoMesServiceTests.cs` (criar)
NAO FAZER: nao implementar o service.
RETORNO ESPERADO: arquivo de teste + output RED.

---

## TASK-074 — [GREEN] Implementar ProjecaoMesService

STATUS: CONCLUIDA (6/6 testes RED da TASK-073 GREEN, suite completa 367/367. Formula bate com item 9)
AGENT: levi
DEPENDENCIAS: TASK-073
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 9; arquivo de teste de TASK-073 (leitura)
ESCOPO: Implementar `CalcularProjecaoDoMes` chamando as 3 dependencias injetadas e montando `ProjecaoMesResultado` com a formula.
CRITERIO DE ACEITE:
1. Testes de TASK-073 GREEN sem editar o arquivo de teste.
2. Formula bate exatamente com regra-de-negocio.md item 9.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Services/ProjecaoMesService.cs`
NAO FAZER: nao editar o arquivo de teste.
RETORNO ESPERADO: diff do `ProjecaoMesService.cs`.

---

## TASK-075 — [GREEN confirmado] Rodar testes do ProjecaoMesService

STATUS: CONCLUIDA (Kira confirmou inline ao rodar dotnet test antes de commitar a TASK-074 — 367/367 GREEN, sem reescrever nenhum teste)
AGENT: mike
DEPENDENCIAS: TASK-074
FLUXO: Implementacao
CONTEXTO A LER: arquivo de teste de TASK-073
ESCOPO: Rodar os testes, sem reescrever.
CRITERIO DE ACEITE: todos GREEN; falha vira relatorio de bug.
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao editar nenhum arquivo.
RETORNO ESPERADO: relatorio GREEN ou falhas.

---

## TASK-076 — Style: revisao do ProjecaoMesService

STATUS: CONCLUIDA + APROVADA PELO STYLE de primeira (367/367 testes GREEN). Formula do item 9 bate exatamente, sem sinal invertido. Verificacao especifica de double-counting entre FluxoCaixaService (exclui Transferencia/FaturaId) e FaturaProjecaoService (usa FaturaSaldoCalculator): sem sobreposicao, cada fonte cobre uma fatia distinta. Confirmado tambem que emprestimo (item 13) fica fora da projecao por decisao deliberada do usuario (2026-07-20), nao e regra omissa. Cadeia critica do item 9 fechada sem achado pendente
AGENT: style
DEPENDENCIAS: TASK-075
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 9 inteiro; clean-code.md
ESCOPO: Revisar a composicao final da formula contra a regra de negocio.
CRITERIO DE ACEITE: veredito ou tarefa de correcao.
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + tarefa de correcao se reprovado.

---

## TASK-077 — Endpoint do dashboard (DTO + Controller + DI)

STATUS: CONCLUIDA (GET /api/dashboard/projecao-mes?ano=&mes= implementado, controller so orquestra, DI registrado. Suite completa 367/367)
AGENT: levi
DEPENDENCIAS: TASK-076
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 9; stack.md secoes DTOs/ e Controllers/; padrao existente em `ContasReceberController.cs` (endpoint `total-esperado-mes`) e `FaturaResponse.cs` (`FromX`)
ESCOPO: Criar `ProjecaoMesResponse` (com `FromResultado`), `DashboardController` com `GET /api/dashboard/projecao-mes?ano=&mes=`, e registrar `IFaturaProjecaoService`/`IProjecaoMesService` no `Program.cs`.
CRITERIO DE ACEITE:
1. GET retorna 200 com os 5 campos da formula.
2. Controller so orquestra, sem logica de negocio.
3. DI registrado (`AddScoped`) nos mesmos moldes dos servicos existentes.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/DTOs/ProjecaoMesResponse.cs` (criar), `MyFinances/MyFinances/Controllers/DashboardController.cs` (criar), `MyFinances/MyFinances/Program.cs`
NAO FAZER: nao adicionar logica de calculo no controller.
RETORNO ESPERADO: diff dos 3 arquivos.

---

## TASK-078 — Style: revisao do endpoint do dashboard

STATUS: CONCLUIDA + APROVADA PELO STYLE de primeira (367/367 testes GREEN). Contrato expoe exatamente os 7 campos da formula, convencao de rota/parametros consistente com o endpoint total-esperado-mes ja aprovado, controller so orquestra, DI correta. Observacao nao bloqueante: ProjecaoMesResponse e o unico DTO do projeto que mapeia a partir de um record de Services (ProjecaoMesResultado) em vez de Domain -- ja existe precedente (FaturaProjecaoMes), registrado como inconsistencia de padrao, nao violacao. MODULO PROJECAO DO MES FECHADO (TASK-063 a TASK-078)
AGENT: style
DEPENDENCIAS: TASK-077
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 9; clean-code.md
ESCOPO: Revisar `DashboardController` e `ProjecaoMesResponse` contra regra de negocio e convencao de contrato de API.
CRITERIO DE ACEITE: veredito ou tarefa de correcao.
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + tarefa de correcao se reprovado.

---

## Mapa de dependencia (TASK-063 a TASK-078)

```
063 (repo lancamento) -> 065 (RED) -> 066 (GREEN) -> 067 (confirma) -> 068 (style) ─┐
064 (repo fatura)     -> 069 (RED) -> 070 (GREEN) -> 071 (confirma) -> 072 (style) ─┼─> 073 (RED master) -> 074 (GREEN) -> 075 (confirma) -> 076 (style) -> 077 (endpoint) -> 078 (style)
```

## Decisoes resolvidas com o usuario em 2026-07-20

1. **Emprestimo (item 13) fica FORA da projecao.** Confirmado: dinheiro
   emprestado nao e "gasto" no sentido de `total_pago_no_mes` — vira "ativo"
   via `ContaReceber` e so conta quando volta (`total_recebido_no_mes`). A
   exclusao geral de Transferencia (item 3) em TASK-065/066 ja cobre isso
   sem excecao adicional — nenhuma mudanca de escopo necessaria.
2. **Fatura parcialmente paga e FRACIONADA**, nao binaria. `ValorPago`
   (ou `ValorTotal - SaldoPendente`) entra em `total_pago_no_mes`,
   `SaldoPendente` entra em `total_a_pagar_no_mes`, simetrico ao que
   `ContaReceber` ja faz. Refletido em TASK-069/070/072.
3. **"Fatura do mes" = `DataVencimento` caindo no ano/mes consultado**,
   simetrico ao `data_prevista` do `ContaReceber`. Refletido em TASK-064.
4. **Multiplos cartoes: SOMA tudo** num unico `total_pago_no_mes`/
   `total_a_pagar_no_mes`, sem breakdown por cartao no endpoint.

## Pendencias registradas, nao bloqueiam esta leva

5. **Escopo de front nao incluido nesta leva.**
   `MyFinanceFrontEnd/src/features/dashboard/` so tem `.gitkeep` — nenhuma
   tela/hook/api existe. Nao ha wireframe/identidade especifica pra essa
   tela alem do generico dark/roxo. Se quiser UI decomposta, definir pelo
   menos: so saldo projetado, ou breakdown dos 4 termos, ou grafico.
6. **Conta Fixa (item 6) nao existe no codebase** (nem Domain, nem migration
   da tabela, so a FK morta `conta_fixa_id` em `Lancamento`). Quando existir,
   so gera `Lancamento` comuns que o agregador generico ja soma — nenhuma
   conta fixa aparece na projecao v1 ate esse modulo ser construido a parte.
   ATUALIZACAO (2026-07-23): modulo Conta Fixa implementado em paralelo, ver
   secao "Modulo Conta Fixa (DEMANDA-002)" acima — os `Lancamento` que ele
   gera ja sao genericos e devem entrar no agregador desta projecao sem
   mudanca de escopo aqui, mas ninguem confirmou isso na integracao real
   ainda (os dois modulos nunca rodaram juntos ate este merge).

Nenhuma pendencia de decisao de produto restante para TASK-063 a TASK-078.
Fila pronta para execucao.

---

# Modulo Dashboard (front) — Opcao C aprovada pelo usuario em 2026-07-24

Arquitetado por killua a partir da pendencia 5 acima (backend do item 9 fechado
em TASK-063 a TASK-078; front nunca decomposto). Usuario aprovou Opcao C:
saldo projetado + breakdown dos 4 termos + grafico entradas vs saidas.
`LimiteGastoIndicador` (TASK-059 do modulo Limite de Gasto, ja implementado)
entra embutido nesta pagina — e o consumidor que faltava pra ele.

Suposicoes/pendencias sinalizadas por killua, nao decididas:
1. Sem seletor de mes/ano nesta leva — mostra `new Date()` fixo.
2. Sem shell de navegacao global (sidebar/topbar) ligando as rotas ja
   existentes (`/investimentos`, `/contas`, `/cartao`, `/limites-gasto`) —
   fica mais visivel ao aposentar Home.tsx, mas e lacuna pre-existente, fora
   de escopo aqui.
3. `recharts` esta citado em stack.md mas ausente do `package.json` (drift
   documental do modulo de cotacao por ticker removido em 2026-07-12) — a
   TASK-082 instala a dependencia real.
4. Integracao Conta Fixa -> agregador de fluxo de caixa (ver pendencia 6
   acima) nunca rodou de ponta a ponta ainda; os `Lancamento` gerados por
   Conta Fixa devem aparecer na projecao sem mudanca de codigo, mas ninguem
   confirmou isso na pratica.

## TASK-079 — Front: camada de dados da Projecao do Mes (types/api/query-keys/hook)

STATUS: CONCLUIDA (build do frontend limpo via tsc -b, sem `any`; campos de types.ts conferidos 1:1 contra DTOs/ProjecaoMesResponse.cs real do backend; padrao de contas-receber/api.ts seguido exatamente. Kira verificou os 4 arquivos e o build antes de aprovar)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-078
CONTEXTO A LER: regra-de-negocio.md item 9; stack.md secao "Frontend (React)" (estrutura de pastas); padrao ja usado em features/contas-receber/api.ts, types.ts, query-keys.ts, hooks/useTotalAReceberEsperadoNoMes.ts; contrato ja mergeado MyFinances/MyFinances/Controllers/DashboardController.cs e DTOs/ProjecaoMesResponse.cs (GET /api/dashboard/projecao-mes?ano=&mes=)
ESCOPO: criar types.ts (ProjecaoMesResponse: ano, mes, totalRecebidoNoMes, totalAReceberEsperadoNoMes, totalPagoNoMes, totalAPagarNoMes, saldoProjetado), api.ts (buscarProjecaoMes(ano, mes)), query-keys.ts (dashboardKeys.projecaoMes(ano, mes)) e hooks/useProjecaoMes.ts (useQuery), seguindo exatamente o padrao de useTotalAReceberEsperadoNoMes.
CRITERIO DE ACEITE:
1. useProjecaoMes(ano, mes) retorna os 5 campos numericos tipados, sem `any`.
2. api.ts so monta a chamada HTTP (GET /api/dashboard/projecao-mes?ano=&mes=), sem decisao de cache/retry.
3. Chave do React Query centralizada em query-keys.ts (mesmo padrao de contasReceberKeys), nao string magica espalhada no hook.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/dashboard/types.ts (novo), MyFinanceFrontEnd/src/features/dashboard/api.ts (novo), MyFinanceFrontEnd/src/features/dashboard/query-keys.ts (novo), MyFinanceFrontEnd/src/features/dashboard/hooks/useProjecaoMes.ts (novo)
NAO FAZER: nao calcular saldo/breakdown no front (os 5 valores vem prontos do backend, item 9); nao criar nenhum componente de apresentacao nesta task.
RETORNO ESPERADO: diff dos 4 arquivos.

---

## TASK-080 — Front: card de saldo projetado + breakdown dos 4 termos

STATUS: CONCLUIDA (build limpo via tsc -b. Segue exatamente a forma de LimiteGastoIndicador.tsx: mesmo Card/CardContent, mesmos estados loading/erro/vazio, mesmo formatarMoeda. Cor semantica so no saldo central (positivo >= 0, negativo < 0); os 4 termos sao apresentacao neutra, sem soma/subtracao no componente. Kira conferiu o arquivo e o build antes de aprovar)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-079
CONTEXTO A LER: identidade-visual.md INTEIRO; regra-de-negocio.md item 9; clean-code.md secao "Organizacao (React)"; padrao ja usado em features/dashboard/components/LimiteGastoIndicador.tsx (mesmo Card do shared/ui, mesma forma de tratar loading/erro/vazio, mesmo import de formatarMoeda de features/investimentos/lib)
ESCOPO: componente `CardSaldoProjetado` (recebe ano/mes via props, chama useProjecaoMes internamente) exibindo o saldo_projetado central (cor positivo se >= 0, negativo se < 0) e, abaixo, os 4 termos rotulados e formatados em moeda: total_recebido_no_mes, total_a_receber_esperado_no_mes, total_pago_no_mes, total_a_pagar_no_mes.
CRITERIO DE ACEITE:
1. saldo negativo usa token `negativo`, saldo >= 0 usa `positivo` (mesma logica semantica ja aplicada em LimiteGastoIndicador).
2. os 4 termos aparecem rotulados e formatados via formatarMoeda (mesmo helper ja reusado por LimiteGastoIndicador).
3. estados loading/erro/vazio tratados; nenhum calculo de saldo/soma de dominio no componente (so vem pronto do hook).
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/dashboard/components/CardSaldoProjetado.tsx (novo)
NAO FAZER: nao buscar dados fora do hook useProjecaoMes; nao incluir grafico (fica na TASK-082).
RETORNO ESPERADO: componente pronto para embutir.

---

## TASK-081 — Front: pagina de Dashboard (composicao + rota)

STATUS: CONCLUIDA (build limpo via tsc -b. DashboardPage compoe CardSaldoProjetado + LimiteGastoIndicador para o mes corrente via new Date(), saudacao+logout preservados de Home.tsx. routes.tsx trocado, Home.tsx removido do repo sem import orfao. Kira conferiu os 3 arquivos e o build antes de aprovar)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-080
CONTEXTO A LER: stack.md "Estrutura de pastas (src/)" (raiz da feature = componente roteado); MyFinanceFrontEnd/src/app/Home.tsx e routes.tsx atuais; regra-de-negocio.md itens 9 e 14 (bloco "Onde aparece": "Dashboard/resumo geral")
ESCOPO: criar `DashboardPage.tsx` na raiz de features/dashboard/, compondo CardSaldoProjetado (TASK-080) + LimiteGastoIndicador (ja existente) para o mes corrente (new Date(), sem seletor nesta leva); substituir Home.tsx pela DashboardPage na rota "/" em routes.tsx; remover Home.tsx (placeholder cumpriu o proposito e nao tem outro consumidor).
CRITERIO DE ACEITE:
1. rota "/" renderiza DashboardPage dentro do mesmo ProtectedRoute de antes (guarda preservada).
2. saudacao ao usuario ("Ola, {usuario}") + botao de logout (useAuth) preservados dentro da nova pagina — nao perder a unica funcionalidade que Home.tsx entregava.
3. Home.tsx removido do repo, routes.tsx sem import orfao.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/dashboard/DashboardPage.tsx (novo), MyFinanceFrontEnd/src/app/routes.tsx (editar), MyFinanceFrontEnd/src/app/Home.tsx (remover)
NAO FAZER: nao criar layout/shell global de navegacao (sidebar/topbar linkando as outras rotas ja existentes) — pendencia separada, fora de escopo; nao adicionar seletor de mes/ano nesta leva.
RETORNO ESPERADO: diff dos 3 arquivos + confirmacao de que "/" mostra a nova pagina sem quebrar o logout.

---

## TASK-082 — Front: grafico entradas vs saidas

STATUS: CONCLUIDA (build limpo via tsc -b. recharts instalado de verdade, corrigindo o drift documental do stack.md. Barras coloridas via var(--color-positivo)/var(--color-negativo), mesmos tokens ja usados em CardSaldoProjetado. Soma entradas/saidas e agrupamento de exibicao, nao recalcula saldoProjetado. Kira conferiu o CSS var real em index.css, o arquivo e o build antes de aprovar)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-079, TASK-081
CONTEXTO A LER: stack.md (linha "Grafico (frontend): Recharts" — hoje sem correspondencia em package.json, drift documental do modulo de cotacao por ticker removido em 2026-07-12); identidade-visual.md (tokens positivo/negativo)
ESCOPO: instalar `recharts` como dependencia nova do front; criar componente `GraficoEntradasSaidas` (barra comparando entradas = total_recebido_no_mes + total_a_receber_esperado_no_mes vs saidas = total_pago_no_mes + total_a_pagar_no_mes), usando tokens positivo/negativo; embutir no DashboardPage abaixo do CardSaldoProjetado.
CRITERIO DE ACEITE:
1. grafico usa cor positivo para entradas e negativo para saidas.
2. soma dos dois pares de valores e trivial de exibicao (equivalente a largura de barra ja calculada em LimiteGastoIndicador) — NAO reintroduz a formula do saldo_projetado no front, so agrupa os 4 valores ja recebidos prontos em 2 barras.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/package.json, MyFinanceFrontEnd/src/features/dashboard/components/GraficoEntradasSaidas.tsx (novo), MyFinanceFrontEnd/src/features/dashboard/DashboardPage.tsx (editar, so para incluir o componente)
NAO FAZER: nao recalcular saldo_projetado no front; nao adicionar outra lib de grafico alem de recharts.
RETORNO ESPERADO: diff + print do grafico renderizado.

---

## TASK-083 — Style: revisao do modulo front de Dashboard

STATUS: CONCLUIDA + APROVADA PELO STYLE de primeira (npm run build e npm run lint rodados pelo proprio style, sem erro nos arquivos da entrega). Separacao apresentacao/dados confirmada; soma entradas/saidas do grafico e agrupamento de exibicao, nao regra nova; cores var(--color-positivo)/var(--color-negativo) conferidas contra index.css; padrao de Card/loading/erro/vazio/formatarMoeda consistente com LimiteGastoIndicador.tsx; routes.tsx sem import orfao. 2 observacoes nao bloqueantes registradas: formatarMoeda mora em features/investimentos/lib (divida pre-existente, nao introduzida aqui); obterBarras(data) chamado 2x no render de GraficoEntradasSaidas (cosmetico). MODULO DASHBOARD (FRONT) FECHADO (TASK-079 a TASK-083)
AGENT: style
FLUXO: Implementacao
DEPENDENCIAS: TASK-081, TASK-082
CONTEXTO A LER: identidade-visual.md; clean-code.md secao "Organizacao (React)"; regra-de-negocio.md itens 9 e 14
ESCOPO: revisar toda a entrega do modulo Dashboard (camada de dados, CardSaldoProjetado, DashboardPage, rota, e GraficoEntradasSaidas) contra clean-code.md e regra de negocio.
CRITERIO DE ACEITE: veredito ou tarefa de correcao, cobrindo especificamente: separacao apresentacao/dados; ausencia de calculo de dominio no front; consistencia semantica de cor com identidade-visual.md.
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + tarefa de correcao se reprovado.

---

## Mapa de dependencia (TASK-079 a TASK-083)

```
079 (dados) -> 080 (card+breakdown) -> 081 (pagina+rota) -> 082 (grafico) -> 083 (style)
```

---

# Modulo Front-Gaps — rotas orfas, shell de navegacao, Lancamentos, Categorias

Arquitetado por killua em 2026-07-26 a partir de achado do Kira: `features/contas-fixas/`
e `features/contas-receber/` estao completos mas sem rota (inalcancaveis); nao existe
shell de navegacao (nenhum menu liga as rotas existentes); Lancamentos (itens 1,2,3,5 da
regra-de-negocio.md) e Categorias (item 7) tem backend fechado ha tempo mas zero tela.

Apos estes 4 blocos, nao ha nenhuma tela v1 conhecida faltando (Faturas ja embutida em
ContaCartaoPage; conciliacao item 5 e so "marcar como pago", coberta no Bloco C; De-Para
categoria e saldo de conta ja cobertos/fora de escopo v1 - ver decisao Open Finance).

**NOTA OPERACIONAL (2026-07-26):** durante a execucao em paralelo da Onda 1, um dos
agents rodou um comando git amplo (provavel `git checkout --`/`git restore` sem
pathspec) que reverteu TODOS os arquivos tracked-porem-modificados do worktree pro
estado do HEAD - isso incluiu este proprio `tasks.md` (a secao inteira do modulo
Front-Gaps sumiu) e `ListaContasFixas.tsx` (mudancas da TASK-084 perdidas). Arquivos
NOVOS/untracked (AppShell.tsx, features/lancamentos/*) sobreviveram intactos. Kira
detectou via `git status`/build e reconstruiu manualmente o que foi perdido antes de
comitar. Licao aplicada: a partir daqui, Kira comita cada task aprovada imediatamente
(protege contra revert futuro) e toda nova task despachada nesta leva recebe instrucao
explicita de NUNCA rodar `git checkout`/`restore`/`reset`/`clean` amplo, e de ignorar
arquivos modificados fora do proprio escopo em vez de "limpar" o que parece estranho.

## Decisoes/pendencias aprovadas pelo usuario em 2026-07-26

- Sidebar fixa (desktop) / topbar+drawer (mobile) para navegacao, nao topbar linear
  (8+ destinos nao cabem sem overflow menu).
- Layout compoe ProtectedRoute + AppShell via Outlet, sem editar ProtectedRoute.tsx.
- DashboardPage perde a saudacao/logout proprios (TASK-089) - o shell global assume isso.
- Lancamentos: sem periodo/paginacao no backend endpoint atual - filtro de mes feito
  client-side (mesma limitacao ja aceita no Dashboard).
- Lancamentos: sem endpoint agregado "todas as contas" - selector de conta no topo da
  pagina (banco+investimento, cartao fica de fora, mesmo motivo de useContasParaSelecao).
- Lancamento normal e Transferencia ficam na MESMA tela via segmented control (aprovado,
  P4) - nao telas separadas.
- Categoria: reativar ENTRA nesta leva (aprovado, contra a recomendacao original do
  killua) - precisou de endpoint novo no backend (nao existia). Nao cascateia para
  subcategorias (pendencia sinalizada, regra-de-negocio.md item 7 e omissa sobre isso -
  decisao por seguranca, nao confirmada explicitamente).
- Categoria: editar (nome/parentId) ENTRA nesta leva (aprovado) - endpoint PUT ja existia.
- Categoria: arquivar ENTRA nesta leva (aprovado como recomendado) - endpoint PATCH ja existia.
- CategoriasController fica com verbo assimetrico (Arquivar=PATCH ja existente,
  Reativar=POST novo, replicando o padrao de rota do ContaFixaController por instrucao
  explicita) - registrado, nao corrigido (mudar Arquivar pra POST arriscaria rota ja
  em uso, fora de escopo).
- Achado de contrato (nao e decisao, e fato do codigo): `CategoriaResponse.Tipo` serializa
  como PascalCase ("Despesa"/"Receita"), NAO caixa-alta ("DESPESA"/"RECEITA") como
  `CampoLimiteGasto.tsx` ja espera - TASK-101 precisa converter antes de passar a prop.

---

## Bloco A — Rotas orfas (Contas Fixas e Contas a Receber)

## TASK-084 — Rota + entrada de criacao para Contas Fixas

STATUS: CONCLUIDA (build limpo via tsc -b. Rota /contas-fixas cabeada, botao "Nova conta fixa" abre FormContaFixa embutido em toggle, sem contaFixaParaEditar. Nenhum hook/api/type tocado. Reconstruida manualmente por Kira apos revert acidental de outro agent - ver nota operacional acima. Commit 49b760d)
AGENT: hanzo
FLUXO: Correcao
DEPENDENCIAS: nenhuma
CONTEXTO A LER: regra-de-negocio.md item 6; stack.md "Estrutura de pastas (src/)"
ESCOPO: adicionar rota `/contas-fixas` -> `ListaContasFixas` em routes.tsx, e dentro de `ListaContasFixas.tsx` embutir um toggle "Nova conta fixa" que abre `FormContaFixa` (modo criar) acima da lista - hoje o form existe mas nao e renderizado em nenhuma tela alcancavel.
CRITERIO DE ACEITE:
1. `/contas-fixas` renderiza a lista sem erro de rota.
2. Botao "Nova conta fixa" abre `FormContaFixa` sem `contaFixaParaEditar`, e ao salvar a lista reflete o item novo (invalidacao de cache ja existe no hook).
3. Nenhum hook/api/type e alterado.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/app/routes.tsx, MyFinanceFrontEnd/src/features/contas-fixas/ListaContasFixas.tsx
NAO FAZER: nao editar FormContaFixa.tsx, hooks/ ou types.ts; nao adicionar edicao de conta fixa existente nesta task.
RETORNO ESPERADO: diff dos dois arquivos + confirmacao de que a rota funciona localmente.

---

## TASK-085 — Rota + entrada de criacao para Contas a Receber

STATUS: CONCLUIDA (build limpo via tsc -b. Rota /contas-receber cabeada, botao "Nova conta a receber" abre FormRegistrarContaReceber embutido em toggle. Nenhum hook/api/type tocado. Commit 49b760d junto com TASK-084)
AGENT: hanzo
FLUXO: Correcao
DEPENDENCIAS: nenhuma
CONTEXTO A LER: regra-de-negocio.md item 13; stack.md "Estrutura de pastas (src/)"
ESCOPO: adicionar rota `/contas-receber` -> `ListaContasReceber` em routes.tsx, e dentro de `ListaContasReceber.tsx` embutir um toggle "Nova conta a receber" que abre `FormRegistrarContaReceber` acima da lista.
CRITERIO DE ACEITE:
1. `/contas-receber` renderiza a lista sem erro de rota.
2. Botao abre `FormRegistrarContaReceber` (RECEBIVEL/EMPRESTIMO ja resolvidos internamente pelo form).
3. Nenhum hook/api/type e alterado.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/app/routes.tsx, MyFinanceFrontEnd/src/features/contas-receber/ListaContasReceber.tsx
NAO FAZER: nao editar FormRegistrarContaReceber.tsx, FormRegistrarRecebimento.tsx, hooks/ ou types.ts.
RETORNO ESPERADO: diff dos dois arquivos + confirmacao de que a rota funciona localmente.

---

## TASK-086 — Style review Bloco A

STATUS: CONCLUIDA + APROVADA PELO STYLE de primeira (build+lint limpos, itens 6 e 13 respeitados, nenhuma logica nova vazou pro componente de rota). 2 observacoes nao bloqueantes: UX de toggle inconsistente entre as duas telas (nao e regra quebrada); FormContaFixa/FormRegistrarContaReceber deveriam estar em components/ por convencao do stack.md, mas isso e divida pre-existente de tasks anteriores, fora do escopo desta entrega
AGENT: style
FLUXO: Correcao
DEPENDENCIAS: TASK-084, TASK-085
CONTEXTO A LER: clean-code.md; regra-de-negocio.md itens 6 e 13
ESCOPO: revisar as duas rotas e os toggles de criacao embutidos contra clean-code.md e a regra de negocio (nada de logica nova sendo criada, so cabeamento).
CRITERIO DE ACEITE: aprovado ou lista de correcoes pontuais.
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + tarefa de correcao no esquema da Secao 4, se houver.

**Mapa de dependencia Bloco A:** 084 e 085 paralelas (arquivos disjuntos) -> 086 depende de ambas.

---

## Bloco B — Shell de navegacao

## TASK-087 — AppShell (sidebar desktop + topbar/drawer mobile)

STATUS: CONCLUIDA (build+lint limpos. Sidebar sticky desktop, topbar+drawer mobile, NavLink com destaque bg-primary/10 no ativo, rodape com usuario+logout via useAuth. So tokens de identidade-visual.md, zero cor crua. Commit d6c7578)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: nenhuma
CONTEXTO A LER: identidade-visual.md inteiro; stack.md "Estrutura de pastas (src/)" secao app/
ESCOPO: criar `app/AppShell.tsx`, componente de layout puro (sem fetch, sem regra de negocio) que recebe `children` e renderiza a navegacao com os 9 destinos: Dashboard(/), Contas(/contas), Cartao(/cartao), Investimentos(/investimentos), Contas Fixas(/contas-fixas), Contas a Receber(/contas-receber), Lancamentos(/lancamentos), Categorias(/categorias), Limites de Gasto(/limites-gasto). Usa `useAuth` (ja existente) so pra exibir nome do usuario + acao Sair na sidebar/topbar. Item ativo via NavLink.
  Wireframe desktop (sidebar fixa a esquerda, bg-surface/border-subtle; main em bg-base):
  ```
  +-------------------------------------------+-------------------+
  | Financeiro Pessoal                         | <conteudo da      |
  | ------------------------                   |  rota atual,      |
  | > Dashboard        (ativo: accent)         |  max-w-2xl        |
  |   Contas                                   |  mx-auto>         |
  |   Cartao de credito                        |                   |
  |   Investimentos                            |                   |
  |   Contas Fixas                              |                   |
  |   Contas a Receber                          |                   |
  |   Lancamentos                               |                   |
  |   Categorias                                |                   |
  |   Limites de Gasto                          |                   |
  | ------------------------                    |                   |
  | Ola, {usuario}   [ Sair ]                   |                   |
  +-------------------------------------------+-------------------+
  ```
  Wireframe mobile (< md, topbar compacta com hamburguer abrindo drawer com os mesmos itens + Sair).
  Contrato ilustrativo (esqueleto, nao implementacao): `type AppShellProps = { children: ReactNode }`; `export function AppShell({ children }: AppShellProps): JSX.Element`
CRITERIO DE ACEITE:
1. Todos os 9 links renderizam com `to` correto (mesmo que /lancamentos e /categorias ainda nao tenham rota ate os Blocos C/D rodarem - nao e bloqueante, e so um link ainda sem destino).
2. Rota ativa recebe destaque visual (accent) via NavLink.
3. < md esconde a sidebar fixa e mostra topbar com toggle de drawer.
4. Usa exclusivamente tokens de identidade-visual.md (nenhuma cor hardcoded fora da paleta).
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/app/AppShell.tsx (novo)
NAO FAZER: nao tocar routes.tsx, ProtectedRoute.tsx ou paginas de feature nesta task.
RETORNO ESPERADO: arquivo novo + descricao do resultado em desktop e mobile.

---

## TASK-088 — AuthenticatedLayout + rotas aninhadas em routes.tsx

STATUS: CONCLUIDA (build limpo. Todas as 10 rotas (login+8 protegidas+catch-all) preservadas, incluindo /contas-fixas e /contas-receber. ProtectedRoute > AppShell > Outlet, nenhum dos dois editado. Kira conferiu o arquivo e o build antes de aprovar)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-087, TASK-084, TASK-085
CONTEXTO A LER: stack.md "Estrutura de pastas (src/)"
ESCOPO: criar `app/AuthenticatedLayout.tsx` que compoe `ProtectedRoute` (existente, intocado) + `AppShell` (087) + `<Outlet/>` do react-router. Refatorar `routes.tsx` de `<Routes>` flat para uma `<Route element={<AuthenticatedLayout/>}>` pai envolvendo todas as rotas hoje protegidas (/, /investimentos, /contas, /cartao, /cartao/relatorio, /limites-gasto) MAIS as duas rotas orfas ja cabeadas em 084/085 (/contas-fixas, /contas-receber). /login e o catch-all `*` ficam FORA do layout autenticado.
  Contrato ilustrativo: `export function AuthenticatedLayout(): JSX.Element` // ProtectedRoute > AppShell > Outlet
CRITERIO DE ACEITE:
1. Todas as rotas hoje existentes continuam navegaveis nos mesmos paths.
2. Usuario nao autenticado acessando qualquer rota protegida ainda cai em /login (comportamento de ProtectedRoute preservado).
3. O shell (087) envolve visualmente toda pagina protegida.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/app/AuthenticatedLayout.tsx (novo), MyFinanceFrontEnd/src/app/routes.tsx
NAO FAZER: nao editar ProtectedRoute.tsx nem AppShell.tsx; nao mudar nenhum path de rota existente.
RETORNO ESPERADO: diff + confirmacao manual de que /login e as rotas protegidas continuam funcionando.

---

## TASK-089 — Remover header duplicado do Dashboard

STATUS: CONCLUIDA (build limpo. Saudacao/logout removidos, imports orfaos limpos. Wrapper externo (mx-auto max-w-2xl px-4 py-8) tambem removido por duplicar o <main> do AppShell - so os 3 cards de dominio restam, intactos. Kira conferiu o arquivo e o build antes de aprovar)
AGENT: hanzo
FLUXO: Melhoria
DEPENDENCIAS: TASK-088
CONTEXTO A LER: DashboardPage.tsx atual
ESCOPO: remover a saudacao "Ola, {usuario}" + botao Sair do header de `DashboardPage.tsx`, ja que `AppShell` (087) passa a exibir isso globalmente.
CRITERIO DE ACEITE:
1. DashboardPage nao duplica nome/logout.
2. Os 3 cards de dominio (CardSaldoProjetado, GraficoEntradasSaidas, LimiteGastoIndicador) continuam intactos.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/dashboard/DashboardPage.tsx
NAO FAZER: nao tocar nos componentes filhos (CardSaldoProjetado, GraficoEntradasSaidas, LimiteGastoIndicador).
RETORNO ESPERADO: diff do arquivo.

---

## TASK-090 — Style review Bloco B

STATUS: CONCLUIDA + APROVADA PELO STYLE apos 2 rodadas (build+lint limpos no final). Rodada 1: 3 achados reais - NAV_ITEMS com links pra /lancamentos e /categorias sem sinalizacao de rota pendente; sentence case quebrado em 3 labels (Contas Fixas/Contas a Receber/Limites de Gasto); "Financeiro Pessoal" duplicado 3x sem extracao, diferente do padrao ja aplicado em NavList/UserFooter. Rodada 2: hanzo comentou explicitamente a pendencia de rota (referenciando TASK-096/Bloco D), corrigiu os 3 labels pra sentence case, extraiu BrandTitle(). Kira conferiu o diff e o build antes de aprovar
AGENT: style
FLUXO: Implementacao
DEPENDENCIAS: TASK-087, TASK-088, TASK-089
CONTEXTO A LER: clean-code.md; identidade-visual.md
ESCOPO: revisar AppShell/AuthenticatedLayout/routes.tsx contra clean-code.md (responsabilidade unica: guarda de auth vs. chrome visual) e identidade-visual.md (tokens corretos).
CRITERIO DE ACEITE: aprovado ou lista de correcoes pontuais.
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + tarefa de correcao, se houver.

**Mapa de dependencia Bloco B:** 087 independente -> 088 depende de 087+084+085 -> 089 depende de 088 -> 090 depende de 087/088/089.

---

## Bloco C — Pagina de Lancamentos

## TASK-091 — Camada de dados: types/api/query-keys (lancamentos)

STATUS: CONCLUIDA (build limpo via tsc -b. Campos conferidos 1:1 contra CriarLancamentoRequest.cs/EditarLancamentoRequest.cs/LancamentoResponseDto.cs reais. StatusLancamento inclui os 3 valores do backend, PAGO/PENDENTE/SUGERIDO com comentario explicando que SUGERIDO e v2. Reconstruido intacto apos o incidente de revert - era untracked, sobreviveu. Commit 5046a53)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: nenhuma
CONTEXTO A LER: regra-de-negocio.md itens 1, 2, 5; MyFinances/Controllers/LancamentosController.cs; DTOs/CriarLancamentoRequest.cs, EditarLancamentoRequest.cs, LancamentoResponseDto.cs; stack.md "Estrutura de pastas (src/)"
ESCOPO: criar types.ts (ProjecaoMesResponse: ano, mes, totalRecebidoNoMes, totalAReceberEsperadoNoMes, totalPagoNoMes, totalAPagarNoMes, saldoProjetado), api.ts (criarLancamento, editarLancamento, marcarLancamentoComoPago, removerLancamento, listarFluxoCaixa), query-keys.ts centralizando as chaves de cache por contaId.
CRITERIO DE ACEITE:
1. Todo campo de CriarLancamentoRequest/EditarLancamentoRequest/LancamentoResponseDto tem correspondente no TS.
2. StatusLancamento inclui os 3 valores do backend (PENDENTE/SUGERIDO/PAGO), mesmo que o form (094) so ofereca PENDENTE/PAGO como opcao (v1 nao usa SUGERIDO, item 5).
3. Nenhuma chamada http fora de api.ts.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/lancamentos/types.ts, MyFinanceFrontEnd/src/features/lancamentos/api.ts, MyFinanceFrontEnd/src/features/lancamentos/query-keys.ts
NAO FAZER: nao criar hooks nem componentes nesta task.
RETORNO ESPERADO: os 3 arquivos novos.

---

## TASK-092 — hooks/ de lancamentos e transferencia

STATUS: CONCLUIDA (build limpo via tsc -b. hooks de criar/editar/marcar-pago/remover lancamento invalidando fluxoCaixa(contaId); useCriarTransferencia invalida as DUAS contas (origem e destino). types.ts/api.ts ganharam TransferenciaResponse/CriarTransferenciaRequest, campos conferidos contra os DTOs reais. Commit 5046a53)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-091
CONTEXTO A LER: regra-de-negocio.md itens 3, 12 (transferencia); DTOs/CriarTransferenciaRequest.cs, TransferenciaResponse.cs; Controllers/TransferenciasController.cs
ESCOPO: criar hooks/useFluxoCaixa(contaId), hooks/useCriarLancamento, hooks/useEditarLancamento, hooks/useMarcarComoPago, hooks/useRemoverLancamento - todos invalidando query-keys.ts (091) apos mutation. Criar tambem hooks/useCriarTransferencia (POST /api/transferencias, request: contaOrigemId, contaDestinoId, valor, data, descricao?) e o types/api correspondente (TransferenciaResponse: id, data, valor, contaOrigemId, contaDestinoId?, descricao?) dentro da mesma feature lancamentos/ - decisao: transferencia so e alcancavel a partir da tela de Lancamentos (nao tem rota propria), entao fica colocada aqui em vez de abrir uma feature "transferencias/" quase vazia.
CRITERIO DE ACEITE:
1. Mutation de criar/editar/marcar-pago/remover invalida o fluxo-caixa da MESMA conta (contaId).
2. useCriarTransferencia invalida o fluxo-caixa de AMBAS as contas envolvidas.
3. Nenhum calculo de dominio no hook, so orquestracao de cache.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/lancamentos/hooks/*.ts, MyFinanceFrontEnd/src/features/lancamentos/types.ts (so para adicionar tipos de transferencia), MyFinanceFrontEnd/src/features/lancamentos/api.ts (so para adicionar chamada de transferencia), MyFinanceFrontEnd/src/features/lancamentos/query-keys.ts
NAO FAZER: nao criar componentes JSX.
RETORNO ESPERADO: arquivos novos/editados.

---

## TASK-093 — lib/: filtro de periodo (mes corrente)

STATUS: CONCLUIDA (build limpo via tsc -b. filtrarLancamentosDoMes usa split de string ISO em vez de new Date() pra evitar deslocamento de fuso. Commit 5046a53)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-091
CONTEXTO A LER: regra-de-negocio.md item 9 (recorte de mes calendario)
ESCOPO: funcao pura `filtrarLancamentosDoMes(lancamentos: LancamentoResponseDto[], ano: number, mes: number): LancamentoResponseDto[]` que filtra client-side pela `data` do lancamento, ja que o endpoint nao aceita periodo.
CRITERIO DE ACEITE:
1. Funcao pura, sem hook, sem fetch (testavel isolada).
2. Mesmo recorte de mes calendario usado no resto do dominio (item 9).
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/lancamentos/lib/filtrarPeriodo.ts
NAO FAZER: nao decidir paginacao nem indicar "carregar mais" (fora do contrato atual do backend).
RETORNO ESPERADO: arquivo novo.

---

## TASK-094 — components/: LancamentoItem, FormLancamento, FormTransferencia

STATUS: CONCLUIDA (build limpo. Sinal DEBIT/CREDIT nunca decidido pelo front (so tabela label/cor sobre tipo ja resolvido); status v1 restrito a PENDENTE/PAGO no form (SUGERIDO cobre badge de exibicao mas nunca e opcao); FormTransferencia sem campo tipo/status; AvisoLimiteGasto so em DEBIT+categoria selecionada, ano/mes extraidos da data do lancamento. Kira conferiu os 3 arquivos, os imports cruzados (formatarData, AvisoLimiteGasto, CategoriaSelect, useContasParaSelecao) e o build antes de aprovar)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-092, TASK-093, TASK-103
CONTEXTO A LER: regra-de-negocio.md itens 1 (simbolo manual), 2 (DEBIT/CREDIT), 3 (transferencia), 5 (PENDENTE->PAGO), 14 (aviso de limite); identidade-visual.md (badges de status); features/lancamentos/components/AvisoLimiteGasto.tsx (ja existente, reaproveitar); features/categorias/components/CategoriaSelect.tsx (TASK-103, dependencia cruzada)
ESCOPO: `LancamentoItem` (apresentacao: descricao, valor, badge tipo DEBIT/CREDIT com cor negativo/positivo, badge status com cor pago/pendente, simbolo de `manual`, acoes Marcar como pago [so se PENDENTE]/Editar/Remover com confirmacao). `FormLancamento` (criar/editar, mesmo padrao de FormContaFixa: prop opcional `lancamentoParaEditar` decide o modo; campos descricao, valor, tipo, data, status [PENDENTE|PAGO apenas, item 5 v1], categoria via CategoriaSelect (103), renderiza AvisoLimiteGasto abaixo do campo categoria quando tipo=DEBIT e categoriaId selecionado). `FormTransferencia` (contaOrigemId, contaDestinoId, valor, data, descricao? - sem campo tipo/status). Segmented control "Lancamento"/"Transferencia" alternando os dois formularios (aprovado, P4) fica na pagina (TASK-095), nao aqui.
CRITERIO DE ACEITE:
1. Status PENDENTE mostra acao "Marcar como pago", status PAGO nao mostra essa acao.
2. Remover exige confirmacao inline (mesmo padrao ContaFixaItem).
3. FormLancamento so oferece PENDENTE/PAGO no select de status (nunca SUGERIDO, item 5).
4. AvisoLimiteGasto so aparece com tipo=DEBIT.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/lancamentos/components/LancamentoItem.tsx, MyFinanceFrontEnd/src/features/lancamentos/components/FormLancamento.tsx, MyFinanceFrontEnd/src/features/lancamentos/components/FormTransferencia.tsx
NAO FAZER: nao editar AvisoLimiteGasto.tsx nem CategoriaSelect.tsx (so consumir).
RETORNO ESPERADO: 3 arquivos novos.

---

## TASK-095 — LancamentosPage (com segmented control Lancamento/Transferencia)

STATUS: CONCLUIDA (build limpo. Seletor de conta via useContasParaSelecao, fetch de fluxo-caixa isolado em componente interno FluxoDeCaixaDaConta so montado com conta selecionada + key={contaId} pra remount limpo ao trocar de conta - desvio pragmatico documentado por restricao de arquivo unico. Estado EstadoFormulario cobre criar(lancamento/transferencia)/editar. Kira conferiu o arquivo e o build antes de aprovar)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-094
CONTEXTO A LER: features/contas-receber/hooks/useContasParaSelecao.ts (reaproveitar, banco+investimento, exclui cartao); features/contas-receber/FormRegistrarContaReceber.tsx (padrao de segmented control ja usado, replicar aqui pra Lancamento/Transferencia)
ESCOPO: componente roteado `LancamentosPage`: seletor de conta no topo (reaproveita useContasParaSelecao), lista de lancamentos da conta selecionada filtrada pelo mes corrente (093), segmented control "Lancamento"/"Transferencia" alternando FormLancamento/FormTransferencia (094), botao "Novo".
CRITERIO DE ACEITE:
1. Sem conta selecionada, mostra estado vazio pedindo selecao.
2. Troca de conta refaz o fetch do fluxo-caixa daquela conta.
3. Criar transferencia nao exige que a conta selecionada seja necessariamente a origem (usuario escolhe origem/destino dentro do FormTransferencia).
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/lancamentos/LancamentosPage.tsx
NAO FAZER: nao criar rota nesta task.
RETORNO ESPERADO: arquivo novo.

---

## TASK-096 — Rota /lancamentos

STATUS: CONCLUIDA (build limpo. /lancamentos adicionada, todas as 9 rotas do AppShell agora tem destino real - nenhum link morto restante. Kira conferiu o arquivo e o build antes de aprovar)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-095, TASK-088
CONTEXTO A LER: stack.md "Estrutura de pastas (src/)"
ESCOPO: adicionar `<Route path="/lancamentos" element={<LancamentosPage/>}/>` dentro do bloco aninhado sob AuthenticatedLayout em routes.tsx.
CRITERIO DE ACEITE: /lancamentos navegavel, protegida, dentro do shell.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/app/routes.tsx
NAO FAZER: nao alterar outras rotas.
RETORNO ESPERADO: diff de routes.tsx.

---

## TASK-097 — Style review Bloco C

STATUS: CONCLUIDA + APROVADA PELO STYLE apos 2 rodadas (build+lint limpos no final). Regra critica (itens 2, 3, 5, 9, 14) sem nenhum furo - sinal sempre de tipo, SUGERIDO nunca ofertado, transferencia sem tipo/status visivel, recorte de mes correto, aviso de limite so em DEBIT+categoria. Rodada 1: unico achado foi validarValor/converterValorParaNumero duplicadas identicas entre FormLancamento e FormTransferencia. Rodada 2: hanzo extraiu pra lib/validarValorLancamento.ts, mesmo padrao de contas-fixas/lib/validarContaFixa.ts. MODULO FRONT-GAPS FECHADO (TASK-084 a 106, 23/23)
AGENT: style
FLUXO: Implementacao
DEPENDENCIAS: TASK-091, TASK-092, TASK-093, TASK-094, TASK-095, TASK-096
CONTEXTO A LER: clean-code.md; regra-de-negocio.md itens 1, 2, 3, 5, 9, 14
ESCOPO: revisar toda a feature lancamentos/ contra clean-code.md e a regra (sinal DEBIT/CREDIT nunca decidido no front, status v1 sem SUGERIDO, exclusao de gasto/receita de transferencia continua responsabilidade do backend).
CRITERIO DE ACEITE: aprovado ou lista de correcoes pontuais.
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + tarefa de correcao, se houver.

**Mapa de dependencia Bloco C:** 091 -> 092, 093 (paralelas) -> 094 (depende de 092+093 **e de TASK-103, Bloco D**) -> 095 -> 096 (depende tambem de 088) -> 097.

**Dependencia cruzada critica: TASK-094 (Bloco C) depende de TASK-103 (Bloco D, CategoriaSelect).** Recomenda-se rodar TASK-098 a 103 (Bloco D) antes de fechar TASK-094, mesmo a ordem de apresentacao sendo A/B/C/D.

---

## Bloco D — Pagina de Categorias (revisado em 2026-07-26: reativar + editar aprovados)

## TASK-098 — Backend: endpoint Reativar categoria

STATUS: CONCLUIDA (452/452 testes GREEN, sem regressao. Reativar seta Arquivada=false, lanca CategoriaNaoEncontradaException se nao existir, NAO cascateia pra subcategorias. Mantido padrao de excecao do CategoriaService, sem migrar pra tupla. POST /api/categorias/{id}/reativar, 204/404, mesmo padrao de rota do ContaFixaController. Commit 2b2cde9, feito pelo proprio levi sem Kira pedir - aceito pois tocou so os 3 arquivos permitidos)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: nenhuma
CONTEXTO A LER: regra-de-negocio.md item 7 inteiro; MyFinances/MyFinances/Controllers/ContaFixaController.cs (metodo Reativar) e Services/ContaFixaService.cs (metodo ReativarAsync) como referencia de rota/verbo/retorno a replicar (POST .../reativar, 204 sucesso, 404 nao encontrado); Services/CategoriaService.cs metodo Arquivar (padrao interno de excecao ja estabelecido nesta classe - CategoriaNaoEncontradaException - a MANTER, nunca migrar para tupla)
ESCOPO: adicionar `Task Reativar(Guid id)` em ICategoriaService/CategoriaService (seta Arquivada=false, lanca CategoriaNaoEncontradaException se nao existir, SEM cascatear para subcategorias) e POST /api/categorias/{id}/reativar em CategoriasController (204 sucesso via try/catch traduzindo CategoriaNaoEncontradaException para 404, mesmo padrao ja usado no metodo Arquivar do proprio controller).
CRITERIO DE ACEITE:
1. POST /api/categorias/{id}/reativar em categoria com Arquivada=true retorna 204 e Arquivada passa a false.
2. Mesma chamada em id inexistente retorna 404.
3. Subcategorias da categoria reativada NAO tem Arquivada alterado por este endpoint (pendencia sinalizada: regra-de-negocio.md item 7 nao define isso explicitamente; decisao de nao cascatear e por seguranca, nao confirmada).
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Services/ICategoriaService.cs, MyFinances/MyFinances/Services/CategoriaService.cs, MyFinances/MyFinances/Controllers/CategoriasController.cs
NAO FAZER: nao cascatear reativacao para subcategorias; nao adotar retorno em tupla (bool,string?) do ContaFixaService - CategoriaService so usa excecao; nao alterar o verbo de Arquivar (PATCH) para uniformizar com Reativar (POST) - risco de regressao em rota ja usada, fora de escopo.
RETORNO ESPERADO: endpoint funcionando; confirmar explicitamente que a decisao de nao cascatear foi aplicada, nao esquecida.

---

## TASK-099 — Front: camada de dados (types/api/query-keys) de Categoria

STATUS: CONCLUIDA (build limpo. Tipo PascalCase confirmado contra Program.cs/TipoCategoria.cs reais, comentario documenta a conversao pendente pro CampoLimiteGasto. Rotas conferidas 1:1 contra CategoriasController.cs (GET com tipo/arquivada, PUT, PATCH arquivar, POST reativar). Kira conferiu os 3 arquivos e o build antes de aprovar)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-098
CONTEXTO A LER: regra-de-negocio.md item 7; MyFinances/MyFinances/Controllers/CategoriasController.cs; DTOs/Categoria/*.cs; stack.md "Frontend (React)"; features/contas-fixas/{types.ts,api.ts,query-keys.ts} como padrao de estilo
ESCOPO: types.ts com CategoriaResponse (id, nome, tipo: "Despesa"|"Receita" - PascalCase, NAO "DESPESA"/"RECEITA", ver achado de contrato acima -, parentId?, subcategorias: CategoriaResponse[] ja aninhadas, arquivada), CriarCategoriaRequest (nome, tipo, parentId?), EditarCategoriaRequest (nome, parentId? - tipo NAO faz parte do payload de edicao, imutavel). api.ts: criarCategoria, editarCategoria, listarCategorias(tipo?, arquivada?), arquivarCategoria, reativarCategoria. query-keys.ts.
CRITERIO DE ACEITE:
1. Tipo tipado como "Despesa"|"Receita" com comentario explicando a serializacao (mesmo padrao do comentario em investimentos/types.ts sobre TipoAtivo).
2. listarCategorias sem parentId de query (hierarquia ja vem aninhada no response).
3. Nao existe GET /api/categorias/{id} no backend (so GET lista) - nao inventar funcao de "porId".
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/categorias/types.ts (novo), MyFinanceFrontEnd/src/features/categorias/api.ts (novo), MyFinanceFrontEnd/src/features/categorias/query-keys.ts (novo)
NAO FAZER: nao criar hooks nem componentes nesta task.
RETORNO ESPERADO: os 3 arquivos.

---

## TASK-100 — Front: hooks de Categoria

STATUS: CONCLUIDA (build limpo. 5 hooks - useCategorias, useCriarCategoria, useEditarCategoria, useArquivarCategoria, useReativarCategoria - todos invalidando categoriasKeys.lista() (prefix match cobre qualquer filtro). Nao duplicou hooks de limite de gasto ja existentes. Kira conferiu os 5 arquivos e o build antes de aprovar)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-099
CONTEXTO A LER: regra-de-negocio.md item 7
ESCOPO: hooks/useCategorias(tipo?: "Despesa"|"Receita"), hooks/useCriarCategoria, hooks/useEditarCategoria, hooks/useArquivarCategoria, hooks/useReativarCategoria - todos invalidando a query-key de lista (099) apos mutation.
CRITERIO DE ACEITE:
1. Criar subcategoria (parentId preenchido) invalida a mesma query-key da lista pai.
2. Nenhum calculo de hierarquia no hook (arvore ja vem pronta do backend).
3. Nao reimplementar hooks de limite de gasto (useDefinirLimiteGasto/useRemoverLimiteGasto ja existem em features/limite-gasto/hooks e ja sao consumidos por CampoLimiteGasto.tsx).
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/categorias/hooks/*.ts (novo)
NAO FAZER: nao criar componentes JSX.
RETORNO ESPERADO: arquivos novos.

---

## TASK-101 — components/: CategoriaItem (recursivo, com arquivar/reativar e limite embutido)

STATUS: CONCLUIDA (build limpo. Recursao aninha subcategorias com pl-4 border-l, CampoLimiteGasto so em Despesa com conversao tipada PascalCase->CAIXA-ALTA, arquivar/reativar mutuamente exclusivos, badge Arquivada com token neutro. Kira conferiu o arquivo, o tipo de CampoLimiteGasto e o build antes de aprovar)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-100
CONTEXTO A LER: regra-de-negocio.md itens 7 e 14; features/categorias/components/CampoLimiteGasto.tsx (ja existente, espera categoriaTipo em CAIXA ALTA - converter o PascalCase do backend antes de passar como prop, ver achado de contrato); features/limite-gasto/hooks/useLimitesGasto.ts (reaproveitar, nao criar endpoint novo)
ESCOPO: componente recursivo que renderiza uma categoria e suas `subcategorias` aninhadas (indentacao), com `CampoLimiteGasto` embutido quando `tipo === "Despesa"` (convertendo pra "DESPESA" antes de passar a prop). Badge "Arquivada" quando aplicavel. Botao Arquivar (categoria ativa) OU Reativar (categoria arquivada) via useArquivarCategoria/useReativarCategoria - nunca os dois ao mesmo tempo. Acao "Editar" abrindo FormCategoria (102) em modo edicao. `limiteAtual` de cada categoria vem de um mapa categoriaId->LimiteGastoResponse construido no componente pai (104) a partir de useLimitesGasto(), passado via prop.
CRITERIO DE ACEITE:
1. Subcategorias renderizam aninhadas, nao em lista plana.
2. Categoria tipo Receita nunca renderiza CampoLimiteGasto.
3. Categoria arquivada mostra Reativar (nunca Arquivar); categoria ativa mostra Arquivar (nunca Reativar).
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/categorias/components/CategoriaItem.tsx (novo)
NAO FAZER: nao editar CampoLimiteGasto.tsx.
RETORNO ESPERADO: arquivo novo.

---

## TASK-102 — components/: FormCategoria (criar + editar)

STATUS: CONCLUIDA (build limpo. Prop categoriaParaEditar decide modo, tipo desabilitado e nunca reenviado em edicao, select de categoria-pai filtra por tipo+nivel0+ativa. Kira conferiu o arquivo e o build antes de aprovar)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-100
CONTEXTO A LER: regra-de-negocio.md item 7; features/contas-fixas/FormContaFixa.tsx como padrao EXATO de prop opcional decidindo modo (contaFixaParaEditar -> aqui categoriaParaEditar)
ESCOPO: FormCategoria.tsx com prop opcional `categoriaParaEditar?: CategoriaResponse`. Ausente = modo CRIAR: pede nome, tipo (Despesa/Receita via segmented control), parentId opcional (select de categorias do MESMO tipo, nivel 0 - sem subcategoria de subcategoria, item 7). Presente = modo EDITAR: tipo exibido mas desabilitado/nao reenviado (imutavel - EditarCategoriaRequest so aceita nome/parentId); so nome e parentId editaveis.
CRITERIO DE ACEITE:
1. Modo criar envia CriarCategoriaRequest (nome, tipo, parentId opcional) via useCriarCategoria.
2. Modo editar envia EditarCategoriaRequest (nome, parentId) via useEditarCategoria, tipo nunca no payload.
3. Select de parentId, em ambos os modos, so lista categorias ativas (nao arquivada) do mesmo tipo e sem parentId proprio (nivel 0).
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/categorias/FormCategoria.tsx (novo), MyFinanceFrontEnd/src/features/categorias/lib/validarCategoria.ts (novo)
NAO FAZER: nao ofertar parentId de tipo diferente do selecionado nem de categoria arquivada/subcategoria; nao reintroduzir tipo como campo editavel em modo edicao.
RETORNO ESPERADO: componente unico cobrindo criar+editar.

---

## TASK-103 — components/: CategoriaSelect

STATUS: CONCLUIDA (build limpo. Achata categoria+subcategorias num select nativo com indentacao textual, filtra arquivadas. Mesma classe de select ja usada em FormRegistrarContaReceber, sem cor nova. Kira conferiu o arquivo e o build antes de aprovar)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-100
CONTEXTO A LER: regra-de-negocio.md item 7
ESCOPO: dropdown reutilizavel de categorias (achatando a arvore com indentacao textual por nivel, ex: "  Alimentacao > Restaurante"), filtravel por tipo (Despesa para lancamento DEBIT, Receita para CREDIT). Consumido por FormLancamento (Bloco C, TASK-094) alem da propria feature categorias.
  Contrato ilustrativo: `type CategoriaSelectProps = { tipo: "Despesa" | "Receita"; value: string | undefined; onChange: (categoriaId: string) => void }`; `export function CategoriaSelect(props: CategoriaSelectProps): JSX.Element`
CRITERIO DE ACEITE:
1. Categorias arquivadas nao aparecem como opcao selecionavel.
2. Hierarquia fica visualmente distinguivel (indentacao), sem duplicar chamada de API (reusa useCategorias de 100).
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/categorias/components/CategoriaSelect.tsx (novo)
NAO FAZER: nao importar nada de features/lancamentos/ aqui (a dependencia e no sentido contrario: lancamentos importa categorias).
RETORNO ESPERADO: arquivo novo.

---

## TASK-104 — CategoriasPage

STATUS: CONCLUIDA (build limpo. Segmented control Despesa/Receita, mapa de limites montado por indexacao simples, um unico estado EstadoFormulario cobre criar/editar. Nova subcategoria via select de categoria-pai do proprio FormCategoria, decisao documentada, sem atalho dedicado. Kira conferiu o arquivo e o build antes de aprovar)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-101, TASK-102
CONTEXTO A LER: regra-de-negocio.md item 7; features/contas-receber/FormRegistrarContaReceber.tsx (padrao de segmented control ja usado)
ESCOPO: componente roteado: segmented control Despesa/Receita, lista de categorias top-level daquele tipo via CategoriaItem (101), busca useLimitesGasto() uma vez e monta o mapa categoriaId->limite passado a cada CategoriaItem, botao "Nova categoria" (FormCategoria, 102, sem parentId) e, por categoria, acao "Nova subcategoria" (mesmo FormCategoria com parentId preenchido).
CRITERIO DE ACEITE:
1. Trocar o toggle Despesa/Receita refaz a busca.
2. Criar subcategoria a partir de uma categoria especifica preenche parentId corretamente sem o usuario digitar id nenhum.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/categorias/CategoriasPage.tsx (novo)
NAO FAZER: nao criar rota nesta task.
RETORNO ESPERADO: arquivo novo.

---

## TASK-105 — Rota /categorias

STATUS: CONCLUIDA (build limpo. /categorias adicionada dentro de AuthenticatedLayout, todas as rotas anteriores preservadas incluindo /contas-fixas e /contas-receber. Kira conferiu o arquivo e o build antes de aprovar)
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-104, TASK-088
CONTEXTO A LER: stack.md "Estrutura de pastas (src/)"
ESCOPO: adicionar `<Route path="/categorias" element={<CategoriasPage/>}/>` dentro do bloco aninhado sob AuthenticatedLayout em routes.tsx.
CRITERIO DE ACEITE: /categorias navegavel, protegida, dentro do shell.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/app/routes.tsx
NAO FAZER: nao alterar outras rotas.
RETORNO ESPERADO: diff de routes.tsx.

---

## TASK-106 — Style review Bloco D

STATUS: CONCLUIDA + APROVADA PELO STYLE apos 2 rodadas (455/455 testes GREEN no final). Rodada 1: 2 achados reais - CategoriaSelect duplicava subcategoria como opcao raiz com key repetida (GET /api/categorias sem parentId devolve subcategorias soltas no array alem de aninhadas, achatarCategorias nao filtrava); CategoriaService.Reativar nao validava se o pai da subcategoria estava arquivado (ValidarParent ja fazia essa checagem em Criar/Editar, mas Reativar foi escrito do zero na TASK-098 sem reusar - zero teste cobria Reativar). Rodada 2: hanzo filtrou achatarCategorias por !parentId (mesmo padrao de filtrarOpcoesDeCategoriaPai/categoriasTopLevel) e corrigiu comentario desatualizado em CategoriaItem; levi adicionou validacao de pai arquivado em Reativar (reaproveitando ObterCategoriaOuFalhar, sem tupla) + 3 testes novos (parent arquivado falha, raiz sucesso, parent ativo sucesso). MODULO CATEGORIAS FECHADO (TASK-098 a 106)
AGENT: style
FLUXO: Implementacao
DEPENDENCIAS: TASK-098, TASK-099, TASK-100, TASK-101, TASK-102, TASK-103, TASK-104, TASK-105
CONTEXTO A LER: regra-de-negocio.md itens 7 e 14 inteiros; clean-code.md; stack.md
ESCOPO: validar TODO o Bloco D contra regra-de-negocio.md (hierarquia 1 nivel, tipo imutavel em edicao, limite so em DESPESA, nao-cascata de reativacao documentada) e clean-code.md (nenhum `any`, service sem acesso a DbContext direto, DTO nunca expondo entity crua).
CRITERIO DE ACEITE: veredito (APROVADO ou tarefa de correcao no esquema padrao, redespachada ao agent original).
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito final do Bloco D.

**Mapa de dependencia Bloco D:**
```
098 (backend: Reativar)
  -> 099 (front: data layer) -> 100 (hooks)
       -> 101 (CategoriaItem), 102 (FormCategoria), 103 (CategoriaSelect) [paralelas]
            -> 104 (CategoriasPage, depende de 101+102) -> 105 (rota, depende de 104+088)
  -> 106 (style, depende de 098 a 105) — ULTIMA task do bloco
```

**Nota de renumeracao:** a primeira proposta do killua (antes da aprovacao de reativar/editar) numerava o Bloco D como TASK-098 a 105 sem o endpoint de backend. Apos a aprovacao do usuario em 2026-07-26 (reativar + editar categoria), o bloco foi renumerado para TASK-098 a 106 pra caber a nova task de backend (098) no inicio. TASK-094 (Bloco C) referencia TASK-103 (CategoriaSelect), nao mais TASK-102 como numerado na proposta original.

---

# Backlog mockups vs app rodando — TASK-107 em diante

Gerado por killua em 2026-07-27, a partir de comparacao do app contra
`.claude/context/mockups/`. Cobre Fase 1 (regra de negocio ja alterada acima)
+ gaps de UI levantados pelo usuario. Numeracao continua de TASK-106 (ultima
existente no arquivo).

---

## TASK-107 — [REGRA CRITICA] Esqueleto: PeriodicidadeContaFixa + geracao por ocorrencia

STATUS: CONCLUIDA
AGENT: killua
DEPENDENCIAS: nenhuma
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 6 INTEIRO (revisado); `Domain/ContaFixa.cs`, `Domain/ContaFixaLancamentoFactory.cs`, `Services/ContaFixaService.cs` (codigo atual)
ESCOPO: entregar esqueleto de assinatura compilavel (corpo `NotImplementedException` onde houver logica nova) para: enum `PeriodicidadeContaFixa` (Mensal, Anual, com `ToStorageValue`/`FromStorageValue` no padrao de `TipoConta.cs`); campo `ContaFixa.Periodicidade`; metodo estatico `Domain/ContaFixaLancamentoFactory.ProximaOcorrencia(DateOnly dataAtual, PeriodicidadeContaFixa periodicidade)` (AddMonths(1) se Mensal, AddYears(1) se Anual); ajuste de assinatura em `ContaFixaService.GerarLancamentosPendentes` para usar `ProximaOcorrencia` em vez do array fixo `{0,1}` de meses. Kira cria os arquivos/edita os existentes a partir deste esqueleto.
CRITERIO DE ACEITE:
1. Projeto compila com o novo enum e a nova assinatura, sem logica real na parte nova.
2. Migration de `periodicidade` (default MENSAL) esboçada/planejada (aplicada na TASK-109).
ARQUIVOS PERMITIDOS: nenhum (killua nao escreve arquivo — Kira aplica a partir do esqueleto)
NAO FAZER: nao implementar a logica de `ProximaOcorrencia` nem o novo fluxo de `GerarLancamentosPendentes` (isso e TASK-109); nao mexer em categoria (ja implementada, Fase 1 mudanca 3).
RETORNO ESPERADO: esqueleto compilavel + migration planejada.

---

## TASK-108 — [REGRA CRITICA] RED: testes de periodicidade

STATUS: CONCLUIDA
AGENT: mike
DEPENDENCIAS: TASK-107
FLUXO: Implementacao (rodada RED)
CONTEXTO A LER: regra-de-negocio.md item 6 paragrafo "Periodicidade"; `Domain/ContaFixaLancamentoFactoryTests.cs` (padrao existente)
ESCOPO: testes cobrindo: `ProximaOcorrencia` com Mensal soma 1 mes; com Anual soma 1 ano; `GerarLancamentosPendentes` para ContaFixa Mensal gera lancamento no mes atual + proximo mes (comportamento hoje ja existente, nao pode regredir); para ContaFixa Anual gera lancamento no ano atual + proximo ano, mesmo dia_vencimento; idempotencia preservada em ambos os casos (rodar duas vezes nao duplica); dia_vencimento=31 em periodicidade Anual clampado corretamente em fevereiro.
CRITERIO DE ACEITE: testes compilam e falham por `NotImplementedException`, nunca por erro de compilacao.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances.Tests/Domain/ContaFixaLancamentoFactoryTests.cs`, `MyFinances/MyFinances.Tests/Services/ContaFixaServiceTests.cs`
NAO FAZER: nao implementar logica em `ContaFixaLancamentoFactory`/`ContaFixaService`.
RETORNO ESPERADO: RED confirmado, casos listados.

---

## TASK-109 — [REGRA CRITICA] GREEN: implementar periodicidade + migration

STATUS: CONCLUIDA
AGENT: levi
DEPENDENCIAS: TASK-108
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 6 INTEIRO; testes da TASK-108 (leitura, nunca escrita)
ESCOPO: implementar `ProximaOcorrencia` e o novo fluxo de `GerarLancamentosPendentes` (2 ocorrencias: atual + proxima, conforme periodicidade) ate os testes da TASK-108 ficarem GREEN; gerar migration adicionando `conta_fixa.periodicidade` (default `MENSAL`, NOT NULL) sem quebrar registros existentes; atualizar `ContaFixaConfiguration.cs`.
CRITERIO DE ACEITE:
1. Todos os testes da TASK-108 GREEN.
2. Migration aplicavel sem perda de dado; ContaFixa existente vira `MENSAL` automaticamente.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Domain/ContaFixa.cs`, `MyFinances/MyFinances/Domain/PeriodicidadeContaFixa.cs`, `MyFinances/MyFinances/Domain/ContaFixaLancamentoFactory.cs`, `MyFinances/MyFinances/Services/ContaFixaService.cs`, `MyFinances/MyFinances/Infrastructure/Configurations/ContaFixaConfiguration.cs`, `MyFinances/MyFinances/Migrations/**`
NAO FAZER: nao alterar arquivos em `MyFinances.Tests/**`.
RETORNO ESPERADO: implementacao completa, testes GREEN, migration pronta.

---

## TASK-110 — Confirmar GREEN periodicidade (mike)

STATUS: CONCLUIDA
AGENT: mike
DEPENDENCIAS: TASK-109
FLUXO: Implementacao (rodada GREEN)
CONTEXTO A LER: nenhum — so roda a suite da TASK-108
ESCOPO: rodar os testes de periodicidade e confirmar GREEN.
CRITERIO DE ACEITE: 100% GREEN ou relatorio de bug (arquivo+linha).
ARQUIVOS PERMITIDOS: nenhum (so execucao)
NAO FAZER: nao reescrever teste; nao editar producao.
RETORNO ESPERADO: confirmacao GREEN ou relatorio estruturado.

---

## TASK-111 — Style: revisao periodicidade + DTOs

STATUS: PENDENTE
AGENT: style
DEPENDENCIAS: TASK-110
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 6; clean-code.md
ESCOPO: revisar a implementacao de periodicidade contra a regra (inclusive o ponto em aberto marcado [REVISAR] sobre edicao de periodicidade nao regenerar lancamentos); expor `periodicidade` em `CriarContaFixaRequest`/`EditarContaFixaRequest`/`ContaFixaResponse` (CRUD simples, sem TDD — controller so orquestra); validar que `Controllers/ContaFixaController.cs` passa o novo campo adiante.
CRITERIO DE ACEITE: veredito (APROVADO ou tarefa de correcao no esquema padrao, redespachada a levi).
ARQUIVOS PERMITIDOS: nenhum (style nao edita)
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + achados.

---

## TASK-112 — Front: FormContaFixa ganha periodicidade + categoria

STATUS: PENDENTE
AGENT: hanzo
DEPENDENCIAS: TASK-111
FLUXO: Implementacao
CONTEXTO A LER: identidade-visual.md; regra-de-negocio.md item 6 (paragrafos "Periodicidade" e "Categoria vinculada"); `features/categorias/components/CategoriaSelect.tsx` (componente pronto, reusar)
ESCOPO: adicionar campo `periodicidade` (toggle Mensal/Anual, mesmo padrao de segmented control ja usado no proprio `FormContaFixa.tsx` para outros campos) e campo `categoriaId` (via `CategoriaSelect`, tipo Despesa — conta fixa e sempre DEBIT) ao formulario de criar/editar ContaFixa.
CRITERIO DE ACEITE:
1. Criar ContaFixa envia `periodicidade` e `categoriaId` (opcional).
2. Editar ContaFixa reenvia o `categoriaId` atual por padrao (nunca zera por omissao, mesmo cuidado ja documentado no arquivo para os outros campos).
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/contas-fixas/FormContaFixa.tsx`, `MyFinanceFrontEnd/src/features/contas-fixas/types.ts`, `MyFinanceFrontEnd/src/features/contas-fixas/api.ts`
NAO FAZER: nao mover `useContasParaSelecao` de `contas-receber` para `shared/hooks` (fora de escopo, ja documentado como pendencia no arquivo).
RETORNO ESPERADO: formulario funcional com os 2 campos novos.

---

## TASK-113 — Front: reconstruir ListaContasFixas/ContaFixaItem (mockup 09)

STATUS: PENDENTE
AGENT: hanzo
DEPENDENCIAS: TASK-112
FLUXO: Melhoria
CONTEXTO A LER: mockups `.claude/context/mockups/09 Conta Fixa.dc.html`; identidade-visual.md
ESCOPO: reconstruir `ListaContasFixas.tsx`/`ContaFixaItem.tsx` seguindo o layout do mockup 09 (usuario confirmou que o layout atual esta "bem diferente do sketch"); exibir periodicidade e categoria (nome) no item da lista.
CRITERIO DE ACEITE: layout alinhado ao mockup 09 (icone, badge de periodicidade, categoria visivel); acoes existentes (editar/desativar/reativar) preservadas.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/contas-fixas/ListaContasFixas.tsx`, `MyFinanceFrontEnd/src/features/contas-fixas/components/ContaFixaItem.tsx`
NAO FAZER: nao alterar `FormContaFixa.tsx` (TASK-112 ja fechou).
RETORNO ESPERADO: telas reconstruidas.

---

## TASK-114 — Style review Bloco E (Conta Fixa)

STATUS: PENDENTE
AGENT: style
DEPENDENCIAS: TASK-113
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 6; clean-code.md; identidade-visual.md
ESCOPO: revisao geral do bloco Conta Fixa (backend TASK-107/109/111 + front TASK-112/113).
CRITERIO DE ACEITE: veredito.
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + achados.

---

## TASK-115 — [REGRA CRITICA] Esqueleto: AtivoAporte + preco medio ponderado

STATUS: PENDENTE
AGENT: killua
DEPENDENCIAS: nenhuma
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 8.1 INTEIRO (revisado); `Domain/Ativo.cs`, `Services/AtivoService.cs`, `Repositories/AtivoRepository.cs`, `Controllers/AtivosController.cs`, `DTOs/Ativo/*.cs` (codigo atual — a mudanca quebra `CriarAtivoRequest`, ver PONTO DE ATENCAO da Fase 1)
ESCOPO: esqueleto compilavel (corpo `NotImplementedException`) para: `Domain/AtivoAporte.cs` (Id, AtivoId, Data, Quantidade, PrecoUnitario, ValorTotal calculado, CriadoEm); campo `Ativo.Quantidade`; `Domain/AtivoPrecoMedioCalculator.Calcular(decimal precoMedioAtual, decimal qtdAtual, decimal precoAporte, decimal qtdAporte)` (funcao pura, formula da regra 8.1); nova assinatura de `IAtivoService.CriarAtivo` (quantidade + precoUnitario em vez de valorInvestido) e novo `IAtivoService.RegistrarAporte(Guid ativoId, decimal quantidade, decimal precoUnitario, DateOnly data)`. Definir tambem, no esqueleto, a estrategia de migracao de dados dos `Ativo` ja existentes (sem `quantidade`) — recomendacao: `quantidade = 1` + um `AtivoAporte` sintetico reconstruido a partir de `valor_investido`/`data_compra` atuais, para nao perder o registro. Kira cria/edita os arquivos a partir deste esqueleto.
CRITERIO DE ACEITE:
1. Projeto compila com a nova assinatura, sem logica real na parte nova.
2. Estrategia de migracao de dados documentada no retorno.
ARQUIVOS PERMITIDOS: nenhum (killua nao escreve arquivo)
NAO FAZER: nao implementar a formula real (isso e TASK-117); nao reintroduzir cotacao externa/Brapi em nenhum ponto (item 8, "Escopo: v1 vs v2" continua proibindo).
RETORNO ESPERADO: esqueleto compilavel + plano de migracao de dados.

---

## TASK-116 — [REGRA CRITICA] RED: testes de preco medio ponderado

STATUS: PENDENTE
AGENT: mike
DEPENDENCIAS: TASK-115
FLUXO: Implementacao (rodada RED)
CONTEXTO A LER: regra-de-negocio.md item 8.1 INTEIRO
ESCOPO: testes cobrindo: primeiro aporte (cadastro) define `quantidade`/`valor_investido`/`preco_medio` iniciais corretamente; segundo aporte recalcula `preco_medio` pela formula ponderada (caso didatico: 10 cotas a R$10 + 10 cotas a R$20 = preco medio R$15); aporte com quantidade/preco invalidos (<=0) e rejeitado; `valor_atual` NAO muda automaticamente ao aportar; historico de aportes (`ListarAportes`) retorna todos os aportes em ordem cronologica; `AtivoNaoEncontradoException` ao aportar em ativo inexistente ou desativado.
CRITERIO DE ACEITE: testes compilam e falham por `NotImplementedException`.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances.Tests/Domain/AtivoPrecoMedioCalculatorTests.cs` (novo), `MyFinances/MyFinances.Tests/Services/AtivoServiceTests.cs`
NAO FAZER: nao implementar logica real.
RETORNO ESPERADO: RED confirmado.

---

## TASK-117 — [REGRA CRITICA] GREEN: implementar aporte + migracao de dados

STATUS: PENDENTE
AGENT: levi
DEPENDENCIAS: TASK-116
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 8.1; testes da TASK-116 (leitura, nunca escrita); plano de migracao de dados da TASK-115
ESCOPO: implementar `AtivoPrecoMedioCalculator`, `AtivoService.CriarAtivo` (nova assinatura) e `AtivoService.RegistrarAporte` ate os testes da TASK-116 ficarem GREEN; gerar migration criando `ativo_aporte` e `ativo.quantidade`, com script de migracao de dados dos `Ativo` existentes conforme a estrategia da TASK-115 (nunca perder `valor_investido`/`data_compra` ja gravados).
CRITERIO DE ACEITE:
1. Todos os testes da TASK-116 GREEN.
2. Migration aplicavel sem perda de dado em `Ativo` existente.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Domain/Ativo.cs`, `MyFinances/MyFinances/Domain/AtivoAporte.cs`, `MyFinances/MyFinances/Domain/AtivoPrecoMedioCalculator.cs`, `MyFinances/MyFinances/Services/AtivoService.cs`, `MyFinances/MyFinances/Services/IAtivoService.cs`, `MyFinances/MyFinances/Repositories/AtivoRepository.cs`, `MyFinances/MyFinances/Repositories/IAtivoRepository.cs`, `MyFinances/MyFinances/Infrastructure/Configurations/AtivoConfiguration.cs`, `MyFinances/MyFinances/Infrastructure/Configurations/AtivoAporteConfiguration.cs`, `MyFinances/MyFinances/Migrations/**`, `MyFinances/MyFinances/Program.cs`
NAO FAZER: nao alterar `MyFinances.Tests/**`; nao adicionar nenhuma chamada a API externa de cotacao.
RETORNO ESPERADO: implementacao completa, GREEN, migration com dado preservado.

---

## TASK-118 — Confirmar GREEN aporte (mike)

STATUS: PENDENTE
AGENT: mike
DEPENDENCIAS: TASK-117
FLUXO: Implementacao (rodada GREEN)
CONTEXTO A LER: nenhum — so roda a suite da TASK-116
ESCOPO: rodar os testes e confirmar GREEN.
CRITERIO DE ACEITE: 100% GREEN ou relatorio de bug.
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao reescrever teste.
RETORNO ESPERADO: confirmacao GREEN ou relatorio estruturado.

---

## TASK-119 — Style: revisao preco medio + migracao de dados

STATUS: PENDENTE
AGENT: style
DEPENDENCIAS: TASK-118
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 8.1; clean-code.md
ESCOPO: revisar a formula, a imutabilidade do aporte, e ESPECIALMENTE a migracao de dados (nenhum Ativo existente pode perder valor_investido/data_compra).
CRITERIO DE ACEITE: veredito.
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + achados.

---

## TASK-120 — Controller/DTOs de aporte (registrar + historico)

STATUS: PENDENTE
AGENT: levi
DEPENDENCIAS: TASK-119
FLUXO: Implementacao
CONTEXTO A LER: clean-code.md "Organizacao (.NET)"; `Controllers/AtivosController.cs` (padrao de estilo, excecao tipada -> status HTTP)
ESCOPO: `POST /api/ativos/{id}/aportes` (RegistrarAporte), `GET /api/ativos/{id}/aportes` (historico). Atualizar `POST /api/ativos` para a nova assinatura (quantidade+precoUnitario). DTOs: `RegistrarAporteRequest`, `AtivoAporteResponse`, `CriarAtivoRequest` (revisado), `AtivoResponse` (ganha `quantidade`/`precoMedio` calculado).
CRITERIO DE ACEITE: contrato documentado (rota, verbo, body, shape de retorno) para os 3 endpoints.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Controllers/AtivosController.cs`, `MyFinances/MyFinances/DTOs/Ativo/*.cs`
NAO FAZER: nao colocar regra de negocio no controller.
RETORNO ESPERADO: contrato de API dos 3 endpoints.

---

## TASK-121 — Testes HTTP de aporte

STATUS: PENDENTE
AGENT: mike
DEPENDENCIAS: TASK-120
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 8.1
ESCOPO: testes HTTP cobrindo criar ativo (novo contrato), registrar aporte (preco medio correto na resposta), listar historico de aportes, erros (ativo inexistente/desativado, quantidade/preco invalidos).
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances.Tests/Controllers/AtivosControllerTests.cs`
NAO FAZER: nao alterar controller/service sem reportar.
RETORNO ESPERADO: testes passando ou relatorio de bug.

---

## TASK-122 — Front: camada de dados de aporte

STATUS: PENDENTE
AGENT: hanzo
DEPENDENCIAS: TASK-120
FLUXO: Implementacao
CONTEXTO A LER: stack.md "Frontend (React)"; `features/investimentos/api.ts`/`types.ts` (padrao existente)
ESCOPO: atualizar `types.ts` (`CriarAtivoRequest`, `AtivoResponse` com quantidade/precoMedio), `api.ts` (novo `registrarAporte`, `listarAportes`), novos hooks `useRegistrarAporte`, `useHistoricoAportes`.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/investimentos/types.ts`, `MyFinanceFrontEnd/src/features/investimentos/api.ts`, `MyFinanceFrontEnd/src/features/investimentos/hooks/useRegistrarAporte.ts` (novo), `MyFinanceFrontEnd/src/features/investimentos/hooks/useHistoricoAportes.ts` (novo)
NAO FAZER: nao renderizar UI aqui.
RETORNO ESPERADO: hooks tipados, sem `any`, com invalidacao de cache cruzada (ativo + resumo).

---

## TASK-123 — Front: cadastro vira primeiro aporte + novo formulario de aporte

STATUS: PENDENTE
AGENT: hanzo
DEPENDENCIAS: TASK-122
FLUXO: Implementacao
CONTEXTO A LER: mockups `.claude/context/mockups/11 Investimentos.dc.html`; identidade-visual.md
ESCOPO: `ModalNovoAtivo.tsx` troca o campo "Valor investido" por "Quantidade" + "Preco unitario" (primeiro aporte); `AtivoItem.tsx`/`AtivoCard.tsx` — a acao de "editar valor investido" deixa de existir; nasce `FormRegistrarAporte.tsx` ("Novo aporte": quantidade + preco unitario + data), disparando `useRegistrarAporte`. A edicao de `valor_atual` (item 8.2, inalterada) continua existindo separadamente, sem confundir com aporte.
CRITERIO DE ACEITE:
1. Cadastro de ativo novo pede quantidade+preco unitario, nao mais valor investido direto.
2. Acao "Novo aporte" disponivel por ativo, mostrando preco medio atualizado apos sucesso.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/investimentos/components/ModalNovoAtivo.tsx`, `MyFinanceFrontEnd/src/features/investimentos/components/AtivoItem.tsx`, `MyFinanceFrontEnd/src/features/investimentos/components/AtivoCard.tsx`, `MyFinanceFrontEnd/src/features/investimentos/components/FormRegistrarAporte.tsx` (novo), `MyFinanceFrontEnd/src/features/investimentos/lib/validarNovoAtivo.ts`, `MyFinanceFrontEnd/src/features/investimentos/hooks/useCriarAtivo.ts`
NAO FAZER: nao remover a edicao de `valor_atual` (item 8.2, continua manual e separada de aporte).
RETORNO ESPERADO: fluxo de cadastro+aporte funcional.

---

## TASK-124 — Front: grafico por ativo individual

STATUS: PENDENTE
AGENT: hanzo
DEPENDENCIAS: TASK-122
FLUXO: Implementacao
CONTEXTO A LER: stack.md ("Grafico (frontend): Recharts"); regra-de-negocio.md item 8.1
ESCOPO: grafico (Recharts) por ativo mostrando o historico de aportes (quantidade acumulada e/ou preco medio ao longo do tempo, a partir de `useHistoricoAportes`) na tela de detalhe do ativo.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/investimentos/components/GraficoHistoricoAportes.tsx` (novo)
NAO FAZER: nao inventar serie de cotacao (item 8: sem API externa); o grafico e so sobre os aportes que o proprio usuario registrou.
RETORNO ESPERADO: componente de grafico consumindo dado real do historico.

---

## TASK-125 — Front: grafico consolidado de todos os ativos

STATUS: PENDENTE
AGENT: hanzo
DEPENDENCIAS: TASK-122
FLUXO: Implementacao
CONTEXTO A LER: `features/investimentos/hooks/useResumoAtivos.ts`, `lib/obterResumoPorTipo.ts` (dado ja existente via `AtivosResumoResponse`)
ESCOPO: grafico consolidado (ex: pizza/barras por tipo RENDA_FIXA vs RENDA_VARIAVEL, usando `AtivosResumoResponse.porTipo` ja calculado no backend) na tela `ListaAtivosPage.tsx`.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/investimentos/components/GraficoConsolidadoAtivos.tsx` (novo), `MyFinanceFrontEnd/src/features/investimentos/ListaAtivosPage.tsx`
NAO FAZER: nao recalcular percentual no front (ja vem pronto de `ObterResumo`).
RETORNO ESPERADO: grafico consolidado integrado a pagina.

---

## TASK-126 — Style review Bloco F (Investimentos)

STATUS: PENDENTE
AGENT: style
DEPENDENCIAS: TASK-123, TASK-124, TASK-125, TASK-121
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 8 INTEIRO; clean-code.md; identidade-visual.md
ESCOPO: revisao geral do bloco Investimentos (backend TASK-115/117/119 + front TASK-122 a 125).
CRITERIO DE ACEITE: veredito.
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + achados.

---

## TASK-127 — Backend: Conta ganha subtipo, icone e cor

STATUS: PENDENTE
AGENT: levi
DEPENDENCIAS: nenhuma
FLUXO: Implementacao
CONTEXTO A LER: `Domain/Conta.cs`, `Domain/TipoConta.cs` (padrao de enum), `Infrastructure/Configurations/ContaConfiguration.cs`, `.claude/context/mockups/03 Contas.dc.html`
ESCOPO: **achado confirmado** — `Conta` hoje NAO tem subtipo bancario (corrente/poupanca), nem icone nem cor; `TipoConta` (Banco/Cartao/Investimento) e um eixo diferente. Adicionar `SubtipoConta` (enum: `Corrente`, `Poupanca`, `DinheiroFisico`, nullable, so aplicavel quando `Tipo=Banco`, storage no padrao de `TipoConta.cs`), `Icone` (string nullable, nome de icone de um catalogo fixo do front — sem catalogo no backend, so string livre validada no front), `Cor` (string nullable, hex). Nao e regra critica (metadado descritivo, sem calculo/maquina de estado) — sem TDD, mas cobrir com testes unitarios basicos no mesmo PR.
CRITERIO DE ACEITE:
1. `POST /api/contas` aceita os 3 campos novos, todos opcionais.
2. `ContaResponse` expoe os 3 campos.
3. Contas existentes ficam com os 3 campos `null` sem quebrar.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Domain/Conta.cs`, `MyFinances/MyFinances/Domain/SubtipoConta.cs` (novo), `MyFinances/MyFinances/Infrastructure/Configurations/ContaConfiguration.cs`, `MyFinances/MyFinances/DTOs/Conta/CriarContaRequest.cs`, `MyFinances/MyFinances/DTOs/Conta/ContaResponse.cs`, `MyFinances/MyFinances/Migrations/**`, `MyFinances/MyFinances.Tests/**` (testes basicos do novo campo)
NAO FAZER: nao criar catalogo de icones no backend (front decide os valores validos); nao tornar nenhum dos 3 campos obrigatorio.
RETORNO ESPERADO: migration aditiva, contrato atualizado.

---

## TASK-128 — Front: nova pagina generica de Contas (mockup 03)

STATUS: PENDENTE
AGENT: hanzo
DEPENDENCIAS: TASK-127
FLUXO: Implementacao
CONTEXTO A LER: mockups `.claude/context/mockups/03 Contas.dc.html`; stack.md "Estrutura de pastas (src/)"
ESCOPO: **achado confirmado** — a rota `/contas` hoje aponta pra `ListaContasSimplesPage.tsx` (dentro de `features/investimentos/`), que so cria/lista contas `tipo=INVESTIMENTO` (hardcoded). NAO existe hoje nenhuma pagina generica de "Contas" (bancarias: corrente/poupanca/dinheiro). Criar `features/contas/ContasPage.tsx` (feature hoje e so `.gitkeep`) que lista TODAS as contas manuais (banco + investimento, sem cartao — que tem pagina propria), combinando `GET /api/contas?tipo=banco` e `?tipo=investimento` (mesmo padrao ja usado por `useContasParaSelecao` em contas-receber), exibindo icone, subtitulo (subtipo/tipo), valor, badge origem (Manual/OFX), patrimonio total, seguindo o mockup 03. Criar `types.ts`/`api.ts`/`query-keys.ts`/`hooks/` da feature.
CRITERIO DE ACEITE:
1. Lista mostra contas banco + investimento juntas com patrimonio total somado.
2. Icone e cor (quando cadastrados) aparecem no card/item.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/contas/ContasPage.tsx` (novo), `MyFinanceFrontEnd/src/features/contas/types.ts` (novo), `MyFinanceFrontEnd/src/features/contas/api.ts` (novo), `MyFinanceFrontEnd/src/features/contas/query-keys.ts` (novo), `MyFinanceFrontEnd/src/features/contas/hooks/*.ts` (novo), `MyFinanceFrontEnd/src/features/contas/components/*.tsx` (novo)
NAO FAZER: nao incluir contas CARTAO nesta lista (tem pagina propria, `/cartao`).
RETORNO ESPERADO: pagina nova funcional, ainda nao roteada (TASK-129 troca a rota).

---

## TASK-129 — Front: modal nova conta (icone, subtipo, mascara de moeda, cor) + troca de rota

STATUS: PENDENTE
AGENT: hanzo
DEPENDENCIAS: TASK-128
FLUXO: Implementacao
CONTEXTO A LER: mockups `.claude/context/mockups/03 Contas.dc.html` (secao "Modal nova conta")
ESCOPO: formulario "Nova conta" com nome, tipo/subtipo (dropdown: Corrente/Poupanca/Dinheiro fisico/Investimento), saldo inicial com MASCARA de moeda enquanto digita (input hoje e `type=number` cru, sem mascara — achado confirmado em `FormCriarContaInvestimento.tsx`, mesmo padrao a evitar aqui), seletor de icone (catalogo fixo de icones Lucide, mesmo padrao visual ja usado nos mockups) e seletor de cor (paleta fixa curta, nao color-picker livre — consistente com identidade-visual.md "sem cor so por enfeite"). Trocar rota `/contas` em `routes.tsx` para `ContasPage` (TASK-128); remover `ListaContasSimplesPage.tsx` (substituida); ajustar link cruzado "Ver investimentos (ativos)" para continuar apontando a `/investimentos`.
CRITERIO DE ACEITE:
1. Input de saldo inicial formata como moeda (R$ 0,00) enquanto o usuario digita.
2. `/contas` renderiza a nova pagina; `ListaContasSimplesPage.tsx` removida sem quebrar nenhuma outra rota.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/contas/components/FormNovaConta.tsx` (novo), `MyFinanceFrontEnd/src/features/contas/lib/mascaraMoeda.ts` (novo), `MyFinanceFrontEnd/src/app/routes.tsx`, `MyFinanceFrontEnd/src/features/investimentos/ListaContasSimplesPage.tsx` (remover)
NAO FAZER: nao remover `features/investimentos/` inteira (Ativos continua ali); nao criar color-picker livre (paleta fixa, ver identidade-visual.md).
RETORNO ESPERADO: rota `/contas` funcional com o novo fluxo completo.

---

## TASK-130 — Style review Bloco G (Contas)

STATUS: PENDENTE
AGENT: style
DEPENDENCIAS: TASK-129
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md itens 1, 8, 10; clean-code.md; identidade-visual.md
ESCOPO: revisao geral do bloco Contas (backend TASK-127 + front TASK-128/129), com atencao especial a nao ter quebrado o fluxo de conta de investimento que existia antes (agora dentro da pagina generica).
CRITERIO DE ACEITE: veredito.
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + achados.

---

## TASK-131 — Front: multiplos cartoes de credito

STATUS: PENDENTE
AGENT: hanzo
DEPENDENCIAS: nenhuma
FLUXO: Implementacao
CONTEXTO A LER: mockups `.claude/context/mockups/05 Cartao de Credito.dc.html`
ESCOPO: **achado confirmado** — o backend ja suporta N contas `tipo=CARTAO` sem nenhuma restricao de unicidade (`ContaService.CriarContaAsync`/`ValidarCartao` nao limitam quantidade); o gap e 100% front — `useContaCartaoAtual.ts` pega so `data?.[0]` (primeiro cartao). Trocar `ContaCartaoPage.tsx` para listar todos os cartoes (card visual por cartao + card semi-transparente com "+" pra adicionar novo, seguindo o mockup 05); selecionar um cartao mostra saldo/faturas/compras daquele cartao especifico.
CRITERIO DE ACEITE: usuario consegue cadastrar um 2o cartao e alternar entre eles sem perder acesso ao 1o.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/cartao/ContaCartaoPage.tsx`, `MyFinanceFrontEnd/src/features/cartao/hooks/useContaCartaoAtual.ts`, `MyFinanceFrontEnd/src/features/cartao/components/CartaoVisual.tsx`
NAO FAZER: nenhuma mudanca de backend (ja suporta).
RETORNO ESPERADO: multiplos cartoes navegaveis na UI.

---

## TASK-132 — Front: categoria funcional na compra do cartao

STATUS: PENDENTE
AGENT: hanzo
DEPENDENCIAS: nenhuma
FLUXO: Correcao
CONTEXTO A LER: regra-de-negocio.md item 7; `Controllers/CategoriasController.cs` (endpoint ja existe), `features/categorias/components/CategoriaSelect.tsx` (componente pronto)
ESCOPO: **achado confirmado** — o comentario em `LancarCompraForm.tsx` ("ainda nao ha endpoint de categorias no backend") esta DESATUALIZADO: `GET /api/categorias` existe e funciona (modulo Categorias concluido). Trocar o `<select disabled>` fixo por `CategoriaSelect` (tipo Despesa), removendo o hardcode `categoriaId: null` em `ContaCartaoPage.tsx`/`handleSubmitCompra`.
CRITERIO DE ACEITE: compra lancada no cartao com categoria escolhida grava `categoriaId` real (nao mais sempre null).
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/cartao/components/LancarCompraForm.tsx`, `MyFinanceFrontEnd/src/features/cartao/ContaCartaoPage.tsx`
NAO FAZER: nenhuma mudanca de backend (endpoint ja existe e funciona).
RETORNO ESPERADO: compra com categoria funcional.

---

## TASK-133 — Front: parcelamento no formulario de compra do cartao

STATUS: PENDENTE
AGENT: hanzo
DEPENDENCIAS: nenhuma
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 12 secao "Parcelamento"; `Controllers/CartaoComprasParceladasController.cs` (endpoint ja existe: `POST /api/contas/{contaId}/compras-parceladas`)
ESCOPO: **achado confirmado** — DEMANDA-005 (parcelamento) esta CONCLUIDA e MERGEADA no backend (`ComprasParceladasService`, `CartaoComprasParceladasController`); o form do front (`LancarCompraForm.tsx`) so lanca compra a vista (`POST .../compras`), sem campo de parcelas. Adicionar campo "Numero de parcelas" (>=2 opcional); quando preenchido com valor >1, chamar `POST /api/contas/{contaId}/compras-parceladas` em vez de `POST .../compras`.
CRITERIO DE ACEITE: compra com 1 parcela usa o endpoint atual; compra com N>=2 parcelas usa o endpoint de parceladas e mostra confirmacao do agrupamento ("Notebook 1/10" etc, se a resposta trouxer esse dado).
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/cartao/components/LancarCompraForm.tsx`, `MyFinanceFrontEnd/src/features/cartao/ContaCartaoPage.tsx`, `MyFinanceFrontEnd/src/features/cartao/api.ts`, `MyFinanceFrontEnd/src/features/cartao/types.ts`, `MyFinanceFrontEnd/src/features/cartao/hooks/useLancarCompra.ts`
NAO FAZER: nenhuma mudanca de backend (ja existe); nao implementar estorno de parcelada (fora deste backlog).
RETORNO ESPERADO: fluxo de compra parcelada funcional no front.

---

## TASK-134 — Front: remover relatorio de cartao morto, redirecionar link

STATUS: PENDENTE
AGENT: hanzo
DEPENDENCIAS: nenhuma
FLUXO: Correcao
CONTEXTO A LER: regra-de-negocio.md item 12 ("Duas visoes") e item 14 ("Onde aparece" — "Relatorio por categoria: comparativo limite vs. realizado")
ESCOPO: **achado confirmado** — "Ver relatorio por categoria" (em `ContaCartaoPage.tsx`) aponta pra `RelatorioCategoriaPage.tsx`, que chama `GET /api/relatorios/categorias` — endpoint que NAO EXISTE (confirmado: so existem `CategoriasController`/`DeParaCategoriasController`, nenhum `RelatoriosController`). E por isso que a tela trava em "Carregando relatorio..." (o proprio comentario do codigo ja documenta o gap, so nao foi conectado a solucao que ja existe): `ComparativoLimiteGastoPage.tsx` (rota `/limites-gasto`) JA FAZ o que a regra item 14 pede ("relatorio por categoria: comparativo limite vs. realizado") com backend funcional (`LimitesGastoController`). Remover `RelatorioCategoriaPage.tsx`, `hooks/useRelatorioCategoria.ts`, `lib/relatorioCategoria.ts`, `obterRelatorioCategoria` em `api.ts`, rota `/cartao/relatorio` em `routes.tsx`; trocar o link em `ContaCartaoPage.tsx` para apontar `/limites-gasto`.
CRITERIO DE ACEITE:
1. Nenhuma chamada a `/api/relatorios/categorias` sobra no codigo.
2. "Ver relatorio por categoria" no cartao leva a `/limites-gasto` (ja funcional).
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/cartao/RelatorioCategoriaPage.tsx` (remover), `MyFinanceFrontEnd/src/features/cartao/hooks/useRelatorioCategoria.ts` (remover), `MyFinanceFrontEnd/src/features/cartao/lib/relatorioCategoria.ts` (remover), `MyFinanceFrontEnd/src/features/cartao/api.ts`, `MyFinanceFrontEnd/src/features/cartao/ContaCartaoPage.tsx`, `MyFinanceFrontEnd/src/app/routes.tsx`
NAO FAZER: nao criar o endpoint `/api/relatorios/categorias` (a solucao e reusar `/limites-gasto`, nao duplicar).
RETORNO ESPERADO: bug de loading infinito e contraste resolvido pela remocao da tela quebrada + redirecionamento.

---

## TASK-135 — Front: reconstruir ComparativoLimiteGastoPage (mockup 10)

STATUS: PENDENTE
AGENT: hanzo
DEPENDENCIAS: TASK-134
FLUXO: Melhoria
CONTEXTO A LER: mockups `.claude/context/mockups/10 Relatorio por Categoria.dc.html`; regra-de-negocio.md item 14
ESCOPO: reconstruir `ComparativoLimiteGastoPage.tsx` (usuario confirmou que esta "totalmente diferente do sketch") seguindo o layout do mockup 10. Adicionar suporte a query param `?categoriaId=` (filtro client-side sobre a lista ja retornada por `useGastoVsLimiteTodasCategorias` — o endpoint ja traz todas as categorias com limite, nao precisa filtro no backend) para suportar o deep-link do TASK-144 (dashboard).
CRITERIO DE ACEITE: layout alinhado ao mockup 10; `/limites-gasto?categoriaId=X` mostra a categoria X em destaque/filtrada.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/limite-gasto/ComparativoLimiteGastoPage.tsx`, `MyFinanceFrontEnd/src/features/limite-gasto/components/ItemComparativoLimite.tsx`
NAO FAZER: nao alterar `LimitesGastoController`/backend (endpoint ja serve o dado necessario).
RETORNO ESPERADO: tela reconstruida + filtro por query param.

---

## TASK-136 — Style review Bloco H (Cartao)

STATUS: PENDENTE
AGENT: style
DEPENDENCIAS: TASK-131, TASK-132, TASK-133, TASK-135
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md itens 7, 12, 14; clean-code.md; identidade-visual.md
ESCOPO: revisao geral do bloco Cartao (TASK-131 a 135).
CRITERIO DE ACEITE: veredito.
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + achados.

---

## TASK-137 — Backend: Categoria ganha icone

STATUS: PENDENTE
AGENT: levi
DEPENDENCIAS: nenhuma
FLUXO: Implementacao
CONTEXTO A LER: `Domain/Categoria.cs` (confirmado: sem campo icone hoje), `Infrastructure/Configurations/CategoriaConfiguration.cs`
ESCOPO: adicionar `Categoria.Icone` (string nullable — nome de icone de catalogo fixo do front, mesma logica de `Conta.Icone` da TASK-127) + migration aditiva + DTOs (`CriarCategoriaRequest`, `EditarCategoriaRequest`, `CategoriaResponse`). Nao critico, sem TDD, testes basicos no mesmo PR.
CRITERIO DE ACEITE: campo opcional, categorias existentes ficam `null` sem quebrar.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Domain/Categoria.cs`, `MyFinances/MyFinances/Infrastructure/Configurations/CategoriaConfiguration.cs`, `MyFinances/MyFinances/DTOs/Categoria/*.cs`, `MyFinances/MyFinances/Migrations/**`, `MyFinances/MyFinances.Tests/**`
NAO FAZER: nao tornar o campo obrigatorio.
RETORNO ESPERADO: migration aditiva, contrato atualizado.

---

## TASK-138 — Front: seletor de icone em Categoria

STATUS: PENDENTE
AGENT: hanzo
DEPENDENCIAS: TASK-137
FLUXO: Implementacao
CONTEXTO A LER: mockups `.claude/context/mockups/06 Categorias.dc.html`
ESCOPO: adicionar seletor de icone (mesmo catalogo fixo da TASK-129) em `FormCategoria.tsx`; exibir o icone escolhido em `CategoriaItem.tsx` e em `CategoriaSelect.tsx`.
CRITERIO DE ACEITE: icone visivel na listagem e no dropdown de selecao de categoria.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/categorias/FormCategoria.tsx`, `MyFinanceFrontEnd/src/features/categorias/components/CategoriaItem.tsx`, `MyFinanceFrontEnd/src/features/categorias/components/CategoriaSelect.tsx`, `MyFinanceFrontEnd/src/features/categorias/types.ts`
NAO FAZER: nao duplicar catalogo de icones (reusar o mesmo definido na TASK-129, promover para `shared/lib` se ainda nao estiver la).
RETORNO ESPERADO: icones funcionais em cadastro/listagem/select.

---

## TASK-139 — Front: investigar/corrigir botao "Editar" duplicado em Categoria [REVISAR]

STATUS: PENDENTE
AGENT: hanzo
DEPENDENCIAS: nenhuma
FLUXO: Correcao
CONTEXTO A LER: `features/categorias/CategoriasPage.tsx`, `features/categorias/components/CategoriaItem.tsx`, `features/categorias/FormCategoria.tsx`
ESCOPO: [REVISAR: killua revisou os 3 arquivos e NAO encontrou um botao "Editar" literalmente duplicado no DOM — `CategoriaItem.tsx` tem exatamente 1 botao "Editar". Candidato mais provavel: quando `CategoriasPage.tsx` abre o `FormCategoria` em modo edicao (acima da lista), o `CategoriaItem` da categoria sendo editada CONTINUA visivel na lista logo abaixo, com seu proprio botao "Editar" ainda clicavel — pode ser isso que o usuario percebeu como "duplicado" (2 pontos de entrada pra edicao da mesma categoria simultaneamente na tela, nao 2 botoes identicos). Confirmar com o usuario/reproduzir no browser antes de codar; se for esse o caso, a correcao e desabilitar ou ocultar o botao "Editar" do `CategoriaItem` correspondente enquanto o formulario dela estiver aberto.] Se a reproducao no browser revelar outra causa, ajustar o ESCOPO e reportar ao Kira antes de alterar codigo.
CRITERIO DE ACEITE: apos a correcao, existe no maximo 1 ponto de entrada de edicao ativo por categoria visivel na tela.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/categorias/CategoriasPage.tsx`, `MyFinanceFrontEnd/src/features/categorias/components/CategoriaItem.tsx`
NAO FAZER: nao alterar `FormCategoria.tsx` sem necessidade comprovada.
RETORNO ESPERADO: causa raiz confirmada + correcao, OU relatorio ao Kira se a causa for outra.

---

## TASK-140 — Style review Bloco I (Categorias)

STATUS: PENDENTE
AGENT: style
DEPENDENCIAS: TASK-138, TASK-139
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 7; clean-code.md
ESCOPO: revisao geral do bloco Categorias (TASK-137 a 139).
CRITERIO DE ACEITE: veredito.
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + achados.

---

## TASK-141 — Front: botoes de acao rapida no Dashboard

STATUS: PENDENTE
AGENT: hanzo
DEPENDENCIAS: nenhuma
FLUXO: Implementacao
CONTEXTO A LER: mockups `.claude/context/mockups/02 Dashboard.dc.html`
ESCOPO: adicionar os 3 botoes de acao rapida do mockup (Novo Lancamento, Transferir, Pagar Conta) ao `DashboardPage.tsx`. "Novo Lancamento" e "Transferir" navegam para `/lancamentos` ja com o segmented control certo pre-selecionado, via query param ou state de rota. "Pagar Conta" navega para `/cartao` (rota do modulo Cartao, onde ja existe o fluxo de pagamento de fatura via `PagarFaturaModal`) — decisao confirmada pelo usuario em 2026-07-27, resolve o `[REVISAR]` anterior.
CRITERIO DE ACEITE:
1. Os 3 botoes navegam para destino funcional real, nenhum fica so visual.
2. "Pagar Conta" abre `/cartao` (nao a lista de lancamentos, nao um menu com duas opcoes).
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/dashboard/DashboardPage.tsx`, `MyFinanceFrontEnd/src/features/dashboard/components/AcoesRapidas.tsx` (novo)
NAO FAZER: nao inventar fluxo novo de "pagar conta" no backend — so navegar para o que ja existe em `/cartao`.
RETORNO ESPERADO: 3 acoes funcionais.

---

## TASK-142 — Front: widget "ultimos lancamentos" no Dashboard (resolve bug de contraste no hover)

STATUS: PENDENTE
AGENT: hanzo
DEPENDENCIAS: nenhuma
FLUXO: Implementacao
CONTEXTO A LER: mockups `.claude/context/mockups/02 Dashboard.dc.html` (secao "Ultimos lancamentos"); `features/lancamentos/components/LancamentoItem.tsx` (padrao existente)
ESCOPO: [ACHADO: `DashboardPage.tsx` hoje NAO renderiza nenhuma lista de lancamentos — so 3 cards (saldo projetado, grafico, limite). O bug de contraste no hover relatado ("valores ficam ilegiveis") nao reproduz em codigo estatico porque essa lista simplesmente nao existe ainda no Dashboard; nem `Card` (shared/ui/card.tsx) nem `LancamentoItem.tsx` tem alguma classe `hover:` hoje. O mockup 02 confirma que deveria existir um widget "Ultimos lancamentos" ali. Se o bug for reproduzivel HOJE em outro lugar (ex: `/lancamentos`), Kira deve apontar o arquivo exato antes do dispatch — nao encontrado por leitura estatica.] Criar o widget "Ultimos lancamentos" no Dashboard (reusar `LancamentoItem`/padrao visual), com a garantia explicita de que nenhum estado de hover usa a mesma cor pro texto e pro fundo (ver identidade-visual.md — nunca depender so de opacidade sem verificar contraste real).
CRITERIO DE ACEITE:
1. Widget mostra os N lancamentos mais recentes (fluxo de caixa) com valores legiveis em qualquer estado, inclusive hover.
2. Nenhuma classe `hover:bg-*`/`hover:text-*` no componente usa a mesma cor pros dois.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/dashboard/components/UltimosLancamentos.tsx` (novo), `MyFinanceFrontEnd/src/features/dashboard/hooks/useUltimosLancamentos.ts` (novo, se precisar de query propria)
NAO FAZER: nao alterar `LancamentoItem.tsx` de `/lancamentos` sem necessidade comprovada de bug la tambem.
RETORNO ESPERADO: widget funcional + confirmacao de que a garantia de contraste foi checada.

---

## TASK-143 — Front: dashboard com widgets configuraveis

STATUS: PENDENTE
AGENT: hanzo
DEPENDENCIAS: TASK-142, TASK-164
FLUXO: Implementacao
CONTEXTO A LER: `DashboardPage.tsx` (composicao atual: `CardSaldoProjetado`, `GraficoEntradasSaidas`, `LimiteGastoIndicador`); regra-de-negocio.md item 8.4
ESCOPO: sistema de widgets escolhiveis pelo usuario (modelo confirmado pelo usuario) incluindo, alem dos widgets ja existentes, grafico de investimentos (reusar `GraficoConsolidadoAtivos` da TASK-125) e o widget de "rendimentos" (resolvido pelo item 8.4: reusa `GraficoRendimentosPorTipo`/`useRendimentosResumo` da TASK-164 — dividendo cadastrado manualmente + valorizacao automatica derivada da edicao de `valor_atual`; NUNCA proventos de fonte externa). Preferencia de quais widgets exibir persistida (localStorage, sem endpoint de preferencia de usuario no backend).
CRITERIO DE ACEITE:
1. Usuario consegue ligar/desligar cada widget, incluindo o de rendimentos.
2. Preferencia sobrevive a reload da pagina.
3. `CardSaldoProjetado` (formula do item 9, regra critica ja aprovada) NAO tem sua semantica alterada — so pode ser ligado/desligado, nunca reinterpretado.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/dashboard/DashboardPage.tsx`, `MyFinanceFrontEnd/src/features/dashboard/components/SeletorWidgets.tsx` (novo), `MyFinanceFrontEnd/src/features/dashboard/lib/preferenciaWidgets.ts` (novo)
NAO FAZER: nao criar endpoint de backend para preferencia de widget; nao alterar a formula/logica de `CardSaldoProjetado`/`ProjecaoMesService`; nao misturar rendimento com "cotacao"/proventos externos (item 8.4 e explicito: dividendo e so manual).
RETORNO ESPERADO: dashboard configuravel funcional, incluindo widget de rendimentos.

---

## TASK-144 — Front: clique em categoria do LimiteGastoIndicador navega filtrando

STATUS: PENDENTE
AGENT: hanzo
DEPENDENCIAS: TASK-135
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 14
ESCOPO: tornar cada linha de `LimiteGastoIndicador.tsx` (no Dashboard) clicavel, navegando para `/limites-gasto?categoriaId={id}` (filtro ja suportado pela TASK-135).
CRITERIO DE ACEITE: clicar numa categoria no Dashboard abre `/limites-gasto` com aquela categoria ja filtrada/destacada.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/dashboard/components/LimiteGastoIndicador.tsx`
NAO FAZER: nenhuma mudanca de backend.
RETORNO ESPERADO: navegacao funcional com filtro aplicado.

---

## TASK-145 — Style review Bloco J (Dashboard)

STATUS: PENDENTE
AGENT: style
DEPENDENCIAS: TASK-141, TASK-143, TASK-144
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md itens 9, 14; clean-code.md; identidade-visual.md
ESCOPO: revisao geral do bloco Dashboard (TASK-141 a 144), com atencao especial a `CardSaldoProjetado` nao ter sido alterado na semantica.
CRITERIO DE ACEITE: veredito.
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + achados.

---

## TASK-146 — Front: reconstruir LancamentosPage (mockup 04)

STATUS: PENDENTE
AGENT: hanzo
DEPENDENCIAS: nenhuma
FLUXO: Melhoria
CONTEXTO A LER: mockups `.claude/context/mockups/04 Lancamentos.dc.html`; `features/lancamentos/LancamentosPage.tsx`, `components/LancamentoItem.tsx` (codigo atual)
ESCOPO: **achado confirmado** — layout atual e seletor de conta + cards planos + botao "Novo" abrindo formulario inline (modal-like); o mockup 04 pede: navegador de mes (seta esquerda/direita + "Julho 2026"), 3 cards de resumo (Entradas/Saidas/Saldo do mes), chips de filtro (Todos/Entradas/Saidas), lista AGRUPADA POR DATA ("Hoje, 5 de julho" / "Ontem..." / datas), item de lista com icone + categoria (nao so descricao), e um FAB (botao flutuante "+") no canto inferior direito em vez do botao "Novo" no topo. Reconstruir seguindo esse layout — REGRA DE NEGOCIO E BACKEND INTOCADOS (so front/layout), reusando os hooks/mutations ja existentes (`useFluxoCaixa`, `useMarcarComoPago`, `useRemoverLancamento`, `FormLancamento`, `FormTransferencia`).
CRITERIO DE ACEITE:
1. Lista agrupada por data, com filtro Todos/Entradas/Saidas.
2. Navegacao de mes (nao mais so mes corrente fixo).
3. Nenhuma mudanca de contrato de API ou de regra de negocio.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/lancamentos/LancamentosPage.tsx`, `MyFinanceFrontEnd/src/features/lancamentos/components/LancamentoItem.tsx`, `MyFinanceFrontEnd/src/features/lancamentos/lib/filtrarPeriodo.ts`, `MyFinanceFrontEnd/src/features/lancamentos/components/FiltroTipoLancamento.tsx` (novo), `MyFinanceFrontEnd/src/features/lancamentos/components/NavegadorMes.tsx` (novo)
NAO FAZER: nao alterar `hooks/`, `api.ts`, `types.ts` (contrato de dados intocado); nao alterar `FormLancamento.tsx`/`FormTransferencia.tsx` (formularios ja aprovados, so o container/lista mudam de layout).
RETORNO ESPERADO: tela reconstruida seguindo o mockup 04, sem regressao de funcionalidade.

---

## TASK-147 — Style review Bloco K (Lancamentos)

STATUS: PENDENTE
AGENT: style
DEPENDENCIAS: TASK-146
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md itens 2, 3; clean-code.md; identidade-visual.md
ESCOPO: revisao do bloco Lancamentos (TASK-146), garantindo que a regra de sinal (item 2, CRITICA) e a exclusao de transferencia (item 3) continuam intocadas na nova apresentacao.
CRITERIO DE ACEITE: veredito.
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + achados.

---

## TASK-148 — Killua: decisao de paleta clara + mecanismo de alternancia

STATUS: PENDENTE
AGENT: killua
DEPENDENCIAS: nenhuma
FLUXO: Implementacao
CONTEXTO A LER: identidade-visual.md INTEIRO
ESCOPO: **achado confirmado** — `index.css` diz explicitamente "O app tem um unico tema (escuro); nao ha alternancia light/dark" (nao e gap incompleto, e ausencia total documentada). `identidade-visual.md` NAO define nenhum token de tema claro. Decidir a paleta clara (tokens equivalentes aos de `identidade-visual.md` "Cores — base (dark)") preservando os principios ja definidos (roxo = acao, verde = entrada, coral = saida, ambar = pendente — cores semanticas nao mudam entre temas, so as bases neutras) e o mecanismo de alternancia (classe `.light`/`.dark` no root, preferencia do SO como default + toggle manual persistido, mesmo padrao usado por bibliotecas Tailwind/shadcn). Entregar a paleta como texto pronto para o Kira aplicar em `identidade-visual.md` via skill `alterar-context` (secao nova "Cores — tema claro").
CRITERIO DE ACEITE: paleta clara completa (bg-base, bg-surface, bg-surface-alt, border, texto x4 niveis) + decisao do mecanismo de toggle, documentados.
ARQUIVOS PERMITIDOS: nenhum (killua nao escreve; entrega texto pro Kira aplicar via alterar-context)
NAO FAZER: nao implementar CSS/componente (isso e TASK-149).
RETORNO ESPERADO: secao pronta pra `identidade-visual.md` + decisao de mecanismo.

---

## TASK-149 — Front: implementar ThemeToggle + tokens claros

STATUS: PENDENTE
AGENT: hanzo
DEPENDENCIAS: TASK-148
FLUXO: Implementacao
CONTEXTO A LER: identidade-visual.md (secao nova de tema claro, aplicada pelo Kira apos TASK-148)
ESCOPO: implementar os tokens `.light` em `index.css`, componente `ThemeToggle` (visivel no `AppShell`, ex: rodape ao lado de "Sair"), persistencia da preferencia (localStorage) e deteccao de preferencia do SO como default inicial.
CRITERIO DE ACEITE: alternar tema muda toda a paleta sem reload; preferencia sobrevive a reload.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/index.css`, `MyFinanceFrontEnd/src/app/AppShell.tsx`, `MyFinanceFrontEnd/src/shared/hooks/useTheme.ts` (novo)
NAO FAZER: nao alterar tokens semanticos (positivo/negativo/alerta) — so as bases neutras mudam entre temas.
RETORNO ESPERADO: toggle funcional.

---

## TASK-150 — Front: PWA (manifest + service worker + instalavel)

STATUS: PENDENTE
AGENT: hanzo
DEPENDENCIAS: nenhuma
FLUXO: Implementacao
CONTEXTO A LER: `MyFinanceFrontEnd/public/` (confirmado: so `favicon.svg`/`icons.svg`, sem manifest); `package.json` (confirmar se `vite-plugin-pwa` ja e dependencia — achado: nao esta instalado)
ESCOPO: adicionar `vite-plugin-pwa` (ou manifest+service worker manual, a criterio do hanzo dado o setup Vite ja existente), `manifest.json` (nome, icones em pelo menos 192x192/512x512 gerados a partir do favicon/icons.svg existente, cor de tema = `--primary` de identidade-visual.md, `display: standalone`), registro do service worker (cache basico de shell, sem estrategia agressiva de cache de API — dado financeiro nao deve ficar stale offline sem indicacao clara).
CRITERIO DE ACEITE: app instalavel (prompt de instalacao no Chrome/Edge), funciona standalone (sem barra de navegador) apos instalado.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/vite.config.ts`, `MyFinanceFrontEnd/public/manifest.json` (novo), `MyFinanceFrontEnd/public/icons/*` (novo), `MyFinanceFrontEnd/package.json`, `MyFinanceFrontEnd/src/app/main.tsx` (registro do SW, se manual)
NAO FAZER: nao cachear respostas de API financeira de forma que mostre dado desatualizado sem aviso.
RETORNO ESPERADO: PWA instalavel confirmada.

---

## TASK-151 — Front: bottom tab bar mobile (mockups)

STATUS: PENDENTE
AGENT: hanzo
DEPENDENCIAS: nenhuma
FLUXO: Melhoria
CONTEXTO A LER: mockups (padrao repetido em `02 Dashboard`, `03 Contas`, `05 Cartao de Credito`, etc — barra fixa no rodape mobile com 5 itens: Inicio, Lancamentos, Cartao, Contas, Mais)
ESCOPO: **achado confirmado** — a navegacao mobile hoje (`AppShell.tsx`) e um drawer full-screen aberto por hamburguer no topo, com os 9 itens completos. O mockup usa uma BOTTOM TAB BAR fixa e sempre visivel com 5 itens (Inicio, Lancamentos, Cartao, Contas, "Mais" abrindo os 4 restantes: Investimentos, Contas fixas, Contas a receber, Categorias, Limites de gasto). Substituir o padrao mobile (< md) do `AppShell` pela bottom tab bar; o item "Mais" abre uma folha/drawer so com os itens restantes. Desktop (sidebar >= md) permanece inalterado.
CRITERIO DE ACEITE: mobile mostra barra fixa no rodape com os 5 itens do mockup; "Mais" da acesso aos demais 5 destinos sem remover nenhuma rota existente.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/app/AppShell.tsx`, `MyFinanceFrontEnd/src/app/components/BottomTabBar.tsx` (novo)
NAO FAZER: nao remover nenhum item de navegacao (so reorganizar Inicio/Lancamentos/Cartao/Contas em destaque + "Mais" pro resto); nao alterar a sidebar desktop.
RETORNO ESPERADO: bottom tab bar mobile funcional.

---

## TASK-152 — Style review Bloco L (Global/infra)

STATUS: PENDENTE
AGENT: style
DEPENDENCIAS: TASK-149, TASK-150, TASK-151
FLUXO: Implementacao
CONTEXTO A LER: identidade-visual.md; clean-code.md
ESCOPO: revisao geral do bloco Global/infra (tema, PWA, nav mobile).
CRITERIO DE ACEITE: veredito.
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + achados.

---

# Bloco M — Rendimentos (dividendo + valorizacao, regra-de-negocio.md item 8.4)

Depende do bloco Investimentos ja existente (TASK-115 a 126), ainda
PENDENTE nesta leva — `AtivoService.RegistrarAporte` (gatilho a) NAO
existe no codigo hoje. O gatilho (b) — `AtivoService.AtualizarValorAtual`
— ja existe e e o unico gatilho automatico com formula fechada (ver item
8.4). Gatilho (a) fica como NO-OP documentado ate confirmacao do usuario
(ver `[REVISAR]` em 8.4) — nenhuma task abaixo tenta calcular valorizacao
a partir de aporte.

Regra CRITICA deste bloco: calculo automatico de VALORIZACAO (delta de
`valor_atual`, gatilho b). Segue ciclo TDD completo (killua esqueleto ->
mike RED -> levi GREEN -> mike confirma -> style), mesmo padrao das
rodadas anteriores (TASK-107 a 111, TASK-115 a 119). Dividendo manual e
CRUD simples, sem TDD pesado — implementado junto no GREEN da regra
critica (mesma classe de Service) e coberto por teste HTTP de integracao.

---

## TASK-153 — Enum TipoRendimento/OrigemRendimento + Entidade Rendimento + Configuration + migration

STATUS: PENDENTE
AGENT: levi
DEPENDENCIAS: nenhuma
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 8.4 INTEIRO; `Domain/TipoAtivo.cs`/`Infrastructure/Configurations/AtivoConfiguration.cs` como padrao de estilo (enum com `ToStorageValue`/`FromStorageValue`, Configuration com `HasConversion`)
ESCOPO: criar enum `TipoRendimento` (Dividendo, Valorizacao) e `OrigemRendimento` (Manual, Automatico), storage value MAIUSCULO snake (`DIVIDENDO`, `VALORIZACAO`, `MANUAL`, `AUTOMATICO`), seguindo exatamente `TipoAtivo.cs`. Criar entidade `Rendimento` (Id, AtivoId, Tipo, Origem, Valor, Data, CriadoEm) e navegacao `Ativo.Rendimentos` (`ICollection<Rendimento>`). Criar `RendimentoConfiguration : IEntityTypeConfiguration<Rendimento>` (`ToTable("rendimento")`, FK `AtivoId -> ativo.id`, `Valor` com `HasPrecision(18,2)`, sem `OnDelete` especial — `Ativo` nunca sofre hard-delete, item 8.3). Registrar `DbSet<Rendimento>` no `MyFinancesDbContext` e gerar a migration.
CRITERIO DE ACEITE:
1. Projeto compila.
2. Migration aplicavel; tabela `rendimento` criada com FK para `ativo`.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Domain/Rendimento.cs` (novo), `MyFinances/MyFinances/Domain/TipoRendimento.cs` (novo), `MyFinances/MyFinances/Domain/OrigemRendimento.cs` (novo), `MyFinances/MyFinances/Domain/Ativo.cs`, `MyFinances/MyFinances/Infrastructure/Configurations/RendimentoConfiguration.cs` (novo), `MyFinances/MyFinances/Data/MyFinancesDbContext.cs`, `MyFinances/MyFinances/Migrations/**`
NAO FAZER: nao criar Repository/Service ainda (TASK-154/155); nao adicionar CHECK de banco para "valor > 0 se DIVIDENDO" — validacao e do Service.
RETORNO ESPERADO: migration aplicavel; build limpo.

---

## TASK-154 — Repository de Rendimento

STATUS: PENDENTE
AGENT: levi
DEPENDENCIAS: TASK-153
FLUXO: Implementacao
CONTEXTO A LER: `Repositories/IAtivoRepository.cs`/`AtivoRepository.cs` como padrao de estilo
ESCOPO: criar `IRendimentoRepository`/`RendimentoRepository` com `Adicionar(Rendimento)`, `ListarPorAtivo(Guid ativoId)` (ordenado por `Data`), `ListarTodos()` (para o resumo agregado do dashboard, TASK-160), `Salvar()`. Registrar no DI (`Program.cs`).
CRITERIO DE ACEITE: build limpo; metodos nomeados por intencao.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Repositories/IRendimentoRepository.cs` (novo), `MyFinances/MyFinances/Repositories/RendimentoRepository.cs` (novo), `MyFinances/MyFinances/Program.cs`
NAO FAZER: nao implementar nenhum calculo aqui (isso e Service/Calculator, TASK-155).
RETORNO ESPERADO: repository testavel.

---

## TASK-155 — [REGRA CRITICA] Esqueleto: RendimentoService + RendimentoValorizacaoCalculator

STATUS: PENDENTE
AGENT: killua
DEPENDENCIAS: TASK-154
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 8.4 INTEIRO; `Domain/ContaReceberSaldoCalculator.cs` como padrao arquitetural (calculadora pura estatica); `Services/AtivoService.cs` (metodo `AtualizarValorAtual`, sera o ponto de integracao)
ESCOPO: esqueleto compilavel (corpo `NotImplementedException`) para: `Domain/RendimentoValorizacaoCalculator.cs` (metodo estatico puro `decimal? Calcular(decimal valorAtualAnterior, decimal valorAtualNovo)` — retorna `null` quando os dois valores sao iguais, delta caso contrario, conforme item 8.4 gatilho b); `Services/IRendimentoService.cs`/`Services/RendimentoService.cs` com `Task<Rendimento> RegistrarDividendo(Guid ativoId, decimal valor, DateOnly data)`, `Task RegistrarValorizacaoAutomatica(Guid ativoId, decimal valorAtualAnterior, decimal valorAtualNovo, DateOnly data)` (usa o Calculator; NO-OP se retornar `null`), `Task<IEnumerable<Rendimento>> ObterHistorico(Guid ativoId)`, `Task<RendimentosResumo> ObterResumoGeral()` (record `RendimentosResumo(decimal TotalDividendos, decimal TotalValorizacao, IEnumerable<Rendimento> Historico)` para o widget do dashboard, TASK-160). Definir tambem a assinatura de integracao: `AtivoService.AtualizarValorAtual` passa a receber `IRendimentoService` via construtor e, apos capturar `valorAtualAnterior` (ANTES de sobrescrever), chama `RegistrarValorizacaoAutomatica`. Kira cria/edita os arquivos a partir deste esqueleto.
CRITERIO DE ACEITE:
1. Projeto compila com a nova assinatura, incluindo `AtivoService` recebendo `IRendimentoService` no construtor, sem logica real nova.
2. Nenhum metodo novo com logica real — todos `NotImplementedException`.
ARQUIVOS PERMITIDOS: nenhum (killua nao escreve arquivo — Kira cria a partir do esqueleto)
NAO FAZER: nao implementar a formula real (TASK-157); nao implementar nenhum caminho do gatilho (a)/aporte — esse gatilho e NO-OP explicito ate confirmacao do usuario (ver regra-de-negocio.md item 8.4).
RETORNO ESPERADO: esqueleto compilavel + confirmacao de que `AtivoService` ja tem o ponto de integracao definido.

---

## TASK-156 — [REGRA CRITICA] RED: testes de valorizacao automatica

STATUS: PENDENTE
AGENT: mike
DEPENDENCIAS: TASK-155
FLUXO: Implementacao (rodada RED)
CONTEXTO A LER: regra-de-negocio.md item 8.4 INTEIRO (gatilho b)
ESCOPO: testes cobrindo: `RendimentoValorizacaoCalculator.Calcular` com delta positivo, delta negativo (desvalorizacao) e delta zero (retorna `null`); `RendimentoService.RegistrarValorizacaoAutomatica` cria `Rendimento(VALORIZACAO, origem=AUTOMATICO, valor=delta)` quando delta != 0, e NAO cria nada quando delta == 0; `AtivoService.AtualizarValorAtual`, ao ser chamado, gera exatamente UM `Rendimento(VALORIZACAO)` com o delta correto (integracao real, nao mock do calculator); chamar `AtualizarValorAtual` duas vezes com o MESMO valor nao duplica registro na segunda vez; duas edicoes sucessivas com valores diferentes geram dois registros distintos, `ObterHistorico` retorna ambos ordenados por `Data`. NAO escrever testes de `RegistrarDividendo` aqui (CRUD simples, sem TDD pesado — cobertura vem via TASK-161, testes HTTP).
CRITERIO DE ACEITE: testes compilam e falham por `NotImplementedException`, nunca por erro de compilacao.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances.Tests/Domain/RendimentoValorizacaoCalculatorTests.cs` (novo), `MyFinances/MyFinances.Tests/Services/RendimentoServiceTests.cs` (novo), `MyFinances/MyFinances.Tests/Services/AtivoServiceTests.cs`
NAO FAZER: nao implementar logica real; nao testar o gatilho (a)/aporte (NO-OP, fora de escopo ate confirmacao do usuario).
RETORNO ESPERADO: RED confirmado.

---

## TASK-157 — [REGRA CRITICA] GREEN: implementar RendimentoService (valorizacao automatica + dividendo manual)

STATUS: PENDENTE
AGENT: levi
DEPENDENCIAS: TASK-156
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 8.4 INTEIRO; testes da TASK-156 (leitura, nunca escrita)
ESCOPO: implementar `RendimentoValorizacaoCalculator.Calcular` e `RendimentoService.RegistrarValorizacaoAutomatica`/`ObterHistorico`/`ObterResumoGeral` ate os testes da TASK-156 ficarem GREEN; integrar a chamada em `AtivoService.AtualizarValorAtual` (capturar `valorAtualAnterior` ANTES de sobrescrever `ativo.ValorAtual`, chamar `RegistrarValorizacaoAutomatica` DEPOIS de persistir). Implementar TAMBEM (sem RED dedicado, CRUD simples) `RendimentoService.RegistrarDividendo`: valida `valor > 0` (`ValorInvalidoException`, reaproveitar), valida `ativo` existente E `Ativa == true` (`AtivoNaoEncontradoException`, reaproveitar), cria `Rendimento(DIVIDENDO, origem=MANUAL)`.
CRITERIO DE ACEITE:
1. Todos os testes da TASK-156 GREEN.
2. `RegistrarDividendo` rejeita valor <= 0 e ativo inexistente/desativado.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Domain/RendimentoValorizacaoCalculator.cs`, `MyFinances/MyFinances/Services/RendimentoService.cs`, `MyFinances/MyFinances/Services/IRendimentoService.cs`, `MyFinances/MyFinances/Services/AtivoService.cs`, `MyFinances/MyFinances/Services/IAtivoService.cs` (so se a assinatura do construtor precisar ajuste real), `MyFinances/MyFinances/Program.cs`
NAO FAZER: nao alterar `MyFinances.Tests/**`; nao implementar nenhum caminho do gatilho (a)/aporte.
RETORNO ESPERADO: implementacao completa, GREEN.

---

## TASK-158 — Confirmar GREEN valorizacao (mike)

STATUS: PENDENTE
AGENT: mike
DEPENDENCIAS: TASK-157
FLUXO: Implementacao (rodada GREEN)
CONTEXTO A LER: nenhum — so roda a suite da TASK-156
ESCOPO: rodar `RendimentoValorizacaoCalculatorTests`/`RendimentoServiceTests`/`AtivoServiceTests` (parte nova) e confirmar GREEN.
CRITERIO DE ACEITE: 100% GREEN ou relatorio de bug.
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao reescrever teste.
RETORNO ESPERADO: confirmacao GREEN ou relatorio estruturado.

---

## TASK-159 — Style: revisao RendimentoService (regra critica + dividendo)

STATUS: PENDENTE
AGENT: style
DEPENDENCIAS: TASK-158
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 8.4; clean-code.md
ESCOPO: validar a formula de valorizacao (delta, aceita negativo, NO-OP em delta zero), a integracao com `AtivoService.AtualizarValorAtual` (ordem de captura do valor anterior), e a validacao de `RegistrarDividendo` (valor>0, ativo existente/ativo).
CRITERIO DE ACEITE: veredito (APROVADO ou tarefa de correcao no esquema padrao).
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + achados.

---

## TASK-160 — Controller/DTOs de Rendimento (dividendo + historico + resumo)

STATUS: PENDENTE
AGENT: levi
DEPENDENCIAS: TASK-159
FLUXO: Implementacao
CONTEXTO A LER: clean-code.md "Organizacao (.NET)"; `Controllers/AtivosController.cs` (extender, padrao de excecao tipada -> status HTTP)
ESCOPO: adicionar em `AtivosController.cs`: `POST /api/ativos/{id}/rendimentos` (`RegistrarDividendoRequest{ valor, data }` — SO tipo DIVIDENDO aceito no body, `VALORIZACAO` nunca vem de fora), `GET /api/ativos/{id}/rendimentos` (historico do ativo, os dois tipos juntos, ordenado por data), `GET /api/ativos/rendimentos-resumo` (agregado para o widget do dashboard: `{ totalDividendos, totalValorizacao, historico: RendimentoResponse[] }` combinando TODOS os ativos). DTOs: `RegistrarDividendoRequest`, `RendimentoResponse` (Id, AtivoId, Tipo, Origem, Valor, Data), `RendimentosResumoResponse`. Traducao de excecoes: `AtivoNaoEncontradoException`->404, `ValorInvalidoException`->400.
CRITERIO DE ACEITE:
1. Contrato documentado (rota, verbo, body, shape) para os 3 endpoints.
2. `POST` rejeita qualquer tentativa de enviar `tipo` no body (nao existe campo tipo no request).
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Controllers/AtivosController.cs`, `MyFinances/MyFinances/DTOs/Rendimento/*.cs` (novo)
NAO FAZER: nao colocar regra de negocio no controller.
RETORNO ESPERADO: contrato de API dos 3 endpoints.

---

## TASK-161 — Testes HTTP dos endpoints de Rendimento (cobre dividendo CRUD)

STATUS: PENDENTE
AGENT: mike
DEPENDENCIAS: TASK-160
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 8.4
ESCOPO: testes HTTP cobrindo: registrar dividendo (201, valor/data corretos, origem=MANUAL); dividendo com valor<=0 -> 400; dividendo em ativo inexistente/desativado -> 404; `GET .../rendimentos` retorna dividendo + valorizacao juntos, ordenados; `PATCH .../valor-atual` seguido de `GET .../rendimentos` mostra o `Rendimento(VALORIZACAO)` gerado automaticamente (prova end-to-end do gatilho b via HTTP); `GET /api/ativos/rendimentos-resumo` soma corretamente totais de dividendo e valorizacao de multiplos ativos.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances.Tests/Controllers/AtivosControllerTests.cs`
NAO FAZER: nao alterar controller/service sem reportar.
RETORNO ESPERADO: testes passando ou relatorio de bug.

---

## TASK-162 — Front: camada de dados (types/api/hooks) de Rendimento

STATUS: PENDENTE
AGENT: hanzo
DEPENDENCIAS: TASK-160
FLUXO: Implementacao
CONTEXTO A LER: stack.md "Frontend (React)"; `features/investimentos/{types.ts,api.ts,query-keys.ts}` (padrao existente)
ESCOPO: adicionar em `types.ts` (`TipoRendimento`, `RendimentoResponse`, `RegistrarDividendoRequest`, `RendimentosResumoResponse`), `api.ts` (`registrarDividendo`, `listarRendimentosDoAtivo`, `buscarRendimentosResumo`), `query-keys.ts` (`investimentosKeys.rendimentos(ativoId)`, `investimentosKeys.rendimentosResumo()`), novos hooks `useRegistrarDividendo` (invalida `rendimentos(ativoId)` + `rendimentosResumo`), `useHistoricoRendimentos(ativoId)`, `useRendimentosResumo`.
CRITERIO DE ACEITE: hooks tipados, sem `any`, invalidacao de cache cruzada apos registrar dividendo.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/investimentos/types.ts`, `MyFinanceFrontEnd/src/features/investimentos/api.ts`, `MyFinanceFrontEnd/src/features/investimentos/query-keys.ts`, `MyFinanceFrontEnd/src/features/investimentos/hooks/useRegistrarDividendo.ts` (novo), `MyFinanceFrontEnd/src/features/investimentos/hooks/useHistoricoRendimentos.ts` (novo), `MyFinanceFrontEnd/src/features/investimentos/hooks/useRendimentosResumo.ts` (novo)
NAO FAZER: nao renderizar UI aqui.
RETORNO ESPERADO: hooks tipados prontos para consumo.

---

## TASK-163 — Front: formulario de cadastro de dividendo vinculado ao ativo

STATUS: PENDENTE
AGENT: hanzo
DEPENDENCIAS: TASK-162
FLUXO: Implementacao
CONTEXTO A LER: identidade-visual.md; regra-de-negocio.md item 8.4
ESCOPO: `FormRegistrarDividendo.tsx` (valor + data), acao disponivel a partir de `AtivoItem.tsx`/`AtivoCard.tsx` ("Registrar dividendo"), disparando `useRegistrarDividendo`. Validacao client-side de valor > 0.
CRITERIO DE ACEITE: dividendo registrado aparece no historico do ativo apos sucesso (cache invalidado).
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/investimentos/components/FormRegistrarDividendo.tsx` (novo), `MyFinanceFrontEnd/src/features/investimentos/lib/validarDividendo.ts` (novo), `MyFinanceFrontEnd/src/features/investimentos/components/AtivoItem.tsx`, `MyFinanceFrontEnd/src/features/investimentos/components/AtivoCard.tsx`
NAO FAZER: nao expor campo `tipo`/`origem` no form (sempre DIVIDENDO/MANUAL, decidido no backend).
RETORNO ESPERADO: formulario funcional integrado a tela de ativo.

---

## TASK-164 — Front: grafico de rendimento por tipo (dividendo vs valorizacao)

STATUS: PENDENTE
AGENT: hanzo
DEPENDENCIAS: TASK-162, TASK-124, TASK-125
FLUXO: Implementacao
CONTEXTO A LER: stack.md ("Grafico (frontend): Recharts"); `features/investimentos/components/GraficoHistoricoAportes.tsx` (TASK-124) e `GraficoConsolidadoAtivos.tsx` (TASK-125) como padrao de estilo — NAO reusa-los diretamente (dimensoes diferentes: aporte e por ativo/quantidade, consolidado e por `TipoAtivo` RENDA_FIXA/RENDA_VARIAVEL; rendimento e por `TipoRendimento` DIVIDENDO/VALORIZACAO — misturar as taxonomias no mesmo componente confundiria o eixo do grafico)
ESCOPO: novo componente `GraficoRendimentosPorTipo.tsx` (Recharts, barras empilhadas por mes: dividendo vs valorizacao), consumindo `useRendimentosResumo` (agregado de todos os ativos) para a tela `ListaAtivosPage.tsx`; funcao pura `agruparRendimentosPorMes.ts` em `lib/` (testavel, recebe `historico` flat do backend e bucketiza por mes/tipo — logica de agrupamento fora do componente, conforme clean-code.md "Logica de calculo NAO vive no componente").
CRITERIO DE ACEITE:
1. Grafico mostra dividendo e valorizacao separados visualmente (empilhado ou series distintas).
2. Nenhum calculo de agregacao roda dentro do JSX do componente (vem de `lib/agruparRendimentosPorMes.ts`).
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/investimentos/components/GraficoRendimentosPorTipo.tsx` (novo), `MyFinanceFrontEnd/src/features/investimentos/lib/agruparRendimentosPorMes.ts` (novo), `MyFinanceFrontEnd/src/features/investimentos/ListaAtivosPage.tsx`
NAO FAZER: nao inventar serie de cotacao; nao misturar `TipoAtivo` com `TipoRendimento` no mesmo grafico.
RETORNO ESPERADO: componente de grafico consumindo dado real, integrado a pagina de Investimentos.

---

## TASK-165 — Style review Bloco M (Rendimentos)

STATUS: PENDENTE
AGENT: style
DEPENDENCIAS: TASK-160, TASK-161, TASK-163, TASK-164
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 8.4 INTEIRO; clean-code.md; identidade-visual.md
ESCOPO: revisao geral do bloco Rendimentos (backend TASK-153/154/157/159 + front TASK-162/163/164), com atencao especial a: Rendimento nunca influenciar `saldo_projetado`/saldo de conta, gatilho (a)/aporte permanecer NO-OP documentado, e `POST /rendimentos` nao aceitar `tipo`/`origem` de fora.
CRITERIO DE ACEITE: veredito (APROVADO ou tarefa de correcao no esquema padrao).
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + achados.
