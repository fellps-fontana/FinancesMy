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

STATUS: PENDENTE
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

STATUS: PENDENTE
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

STATUS: PENDENTE
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

STATUS: PENDENTE
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

STATUS: PENDENTE
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

STATUS: PENDENTE
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

STATUS: PENDENTE
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

STATUS: PENDENTE
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

STATUS: PENDENTE
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

STATUS: PENDENTE
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

STATUS: PENDENTE
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

STATUS: PENDENTE
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

STATUS: PENDENTE
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

STATUS: PENDENTE
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

STATUS: PENDENTE
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

STATUS: PENDENTE
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

STATUS: PENDENTE
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

STATUS: PENDENTE
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

STATUS: PENDENTE
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
