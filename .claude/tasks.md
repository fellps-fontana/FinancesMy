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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
HISTORICO: SubtipoConta (enum Corrente/Poupanca/DinheiroFisico), Icone e Cor adicionados a Conta, DTOs (CriarContaRequest/ContaResponse), ContaConfiguration (mapeamento EF) e migration aditiva AddSubtipoIconeCorToConta (colunas nullable). Validado com dotnet build limpo e dotnet test --filter FullyQualifiedName~Contas: 45/45 verdes.
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

STATUS: CONCLUIDA
HISTORICO: criada features/contas/ (ContasPage.tsx, types.ts, api.ts, query-keys.ts, hooks/useContas.ts, components/{ContaItem,ContaIcone,ContaBadgeOrigem,PatrimonioTotalResumo}.tsx, lib/{calcularPatrimonioTotal,obterIconeConta,obterLabelOrigemConta,obterSubtituloConta}.ts). Lista contas Banco+Investimento combinando GET /api/contas?tipo=banco e ?tipo=investimento, patrimonio total somado, icone/cor exibidos. npx tsc --noEmit limpo.
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

STATUS: CONCLUIDA
HISTORICO: FormNovaConta.tsx (mascara de moeda, seletor de icone Lucide, paleta de cor fixa) + mascaraMoeda.ts; rota /contas trocada em routes.tsx para ContasPage; ListaContasSimplesPage.tsx removida. Correcao inline do Kira: link cruzado "Ver investimentos (ativos)" nao tinha sido preservado na nova pagina — adicionado. Style (TASK-130, 1a rodada) achou 3 problemas alem do escopo original: contradicao de saldo negativo entre front (bloqueava) e back (aceita e testa negativo) — regra-de-negocio.md item 10 era omisso, usuario decidiu permitir negativo em toda conta manual; import cruzado features/contas -> features/investimentos/lib (formatarMoeda); catalogo de icone duplicado entre obterIconeConta.ts e FormNovaConta.tsx. Corrigido pelo mesmo executor (hanzo): shared/lib/{formatarMoeda,validarSaldo}.ts como fonte unica (investimentos/lib passam a reexportar), validarNovaConta.ts removido (dead code, unico consumidor era ListaContasSimplesPage.tsx ja removida), CATALOGO_ICONES derivado de ICONE_POR_NOME. 2a rodada do style: APROVADO. regra-de-negocio.md item 10/10.1 atualizada (killua/alterar-context) documentando saldo negativo permitido e subtipo so aplicavel a Banco.
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

STATUS: CONCLUIDA
HISTORICO: 1a rodada — PRECISA CORRIGIR: 3 achados (contradicao saldo negativo front/back, import cruzado features/contas->features/investimentos, catalogo de icone duplicado) — ver HISTORICO da TASK-129 para a correcao aplicada. 2a rodada, apos correcao: APROVADO. Confirmado pelo style com execucao propria: npx tsc --noEmit limpo, dotnet test --filter FullyQualifiedName~Contas 45/45. Achado remanescente nao bloqueante: texto do item 10.1 da regra-de-negocio.md dizia "rejeitado" quando o comportamento real e testado e "ignorado silenciosamente" (subtipo em conta nao-Banco) — corrigido pelo Kira direto no doc.
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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
GAP CONHECIDO: `AcoesRapidas.tsx` navega com `?novo=lancamento`/`?novo=transferencia`, mas `LancamentosPage.tsx` (fora de ARQUIVOS PERMITIDOS desta task) ainda nao le esse param — o segmented control chega FECHADO, nao pre-selecionado. Precisa de task futura tocando `LancamentosPage.tsx` pra ler o param e abrir o form certo.

---

## TASK-142 — Front: widget "ultimos lancamentos" no Dashboard (resolve bug de contraste no hover)

STATUS: CONCLUIDA
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
NOTA DE EXECUCAO: garantia de contraste cumprida por design — o widget nao tem NENHUM estado `hover:` (itens nao sao clicaveis nesta leva), entao o bug relatado nao tem onde se repetir. Nao foi encontrado o bug reproduzivel em nenhum outro arquivo estatico (`LancamentoItem.tsx`/`Card` nao tinham classe hover alguma antes desta task). GAP vs mockup: o mockup 02 mostra um icone de categoria por linha (ex: casa, carrinho) — o widget implementado nao renderiza icone, so descricao/data/valor/status. Nao bloqueia o criterio de aceite, mas fica registrado pro `style` (TASK-145) avaliar se PRECISA CORRIGIR.

---

## TASK-143 — Front: dashboard com widgets configuraveis

STATUS: CONCLUIDA
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
HISTORICO: SeletorWidgets.tsx + lib/preferenciaWidgets.ts (localStorage) criados; DashboardPage.tsx passou a envolver cada widget (incl. CardSaldoProjetado, GraficoConsolidadoAtivos, GraficoRendimentosPorTipo) num toggle condicional. CardSaldoProjetado confirmado intocado na logica pelo style (rodada 1 e 2). tsc --noEmit e lint limpos.

---

## TASK-144 — Front: clique em categoria do LimiteGastoIndicador navega filtrando

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: TASK-135
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 14
ESCOPO: tornar cada linha de `LimiteGastoIndicador.tsx` (no Dashboard) clicavel, navegando para `/limites-gasto?categoriaId={id}` (filtro ja suportado pela TASK-135).
CRITERIO DE ACEITE: clicar numa categoria no Dashboard abre `/limites-gasto` com aquela categoria ja filtrada/destacada.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/dashboard/components/LimiteGastoIndicador.tsx`
NAO FAZER: nenhuma mudanca de backend.
RETORNO ESPERADO: navegacao funcional com filtro aplicado.
HISTORICO: LimiteGastoIndicador.tsx: linha de categoria virou Link para /limites-gasto?categoriaId={id}, consumido por ComparativoLimiteGastoPage.tsx ja existente. Rodou em paralelo com TASK-143 (arquivos disjuntos).

---

## TASK-145 — Style review Bloco J (Dashboard)

STATUS: CONCLUIDA
AGENT: style
DEPENDENCIAS: TASK-141, TASK-143, TASK-144
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md itens 9, 14; clean-code.md; identidade-visual.md
ESCOPO: revisao geral do bloco Dashboard (TASK-141 a 144), com atencao especial a `CardSaldoProjetado` nao ter sido alterado na semantica.
CRITERIO DE ACEITE: veredito.
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + achados.
HISTORICO: Rodada 1 reprovou por queryKey magica em useUltimosLancamentos.ts (nao usava dashboardKeys centralizado, ao contrario de useProjecaoMes.ts na mesma feature). Redespachado a hanzo: adicionou dashboardKeys.ultimosLancamentos(quantidade) em query-keys.ts. Rodada 2 confirmou a correcao pontual sem reabrir o restante (CardSaldoProjetado intocado, item 14 navegacao funcional, item 8.4 rendimentos sem fonte externa, persistencia so localStorage). APROVADO. Bloco J (TASK-141 a 145) fechado.

---

## TASK-146 — Front: reconstruir LancamentosPage (mockup 04)

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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

STATUS: CONCLUIDA
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
HISTORICO: levi criou enums/entidade/Configuration/migration seguindo TipoAtivo.cs.
Build limpo, migration 20260727215314_AddRendimento aplicada. Commit 0373ab4
(apos merge do Bloco F).

---

## TASK-154 — Repository de Rendimento

STATUS: CONCLUIDA
AGENT: levi
DEPENDENCIAS: TASK-153
FLUXO: Implementacao
CONTEXTO A LER: `Repositories/IAtivoRepository.cs`/`AtivoRepository.cs` como padrao de estilo
ESCOPO: criar `IRendimentoRepository`/`RendimentoRepository` com `Adicionar(Rendimento)`, `ListarPorAtivo(Guid ativoId)` (ordenado por `Data`), `ListarTodos()` (para o resumo agregado do dashboard, TASK-160), `Salvar()`. Registrar no DI (`Program.cs`).
CRITERIO DE ACEITE: build limpo; metodos nomeados por intencao.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Repositories/IRendimentoRepository.cs` (novo), `MyFinances/MyFinances/Repositories/RendimentoRepository.cs` (novo), `MyFinances/MyFinances/Program.cs`
NAO FAZER: nao implementar nenhum calculo aqui (isso e Service/Calculator, TASK-155).
RETORNO ESPERADO: repository testavel.
HISTORICO: levi criou IRendimentoRepository/RendimentoRepository + DI. 1a tentativa
falhou por limite de sessao antes de escrever arquivo (sem impacto); retry concluiu
limpo. Commit 833f7c3.

---

## TASK-155 — [REGRA CRITICA] Esqueleto: RendimentoService + RendimentoValorizacaoCalculator

STATUS: CONCLUIDA
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
HISTORICO: killua desenhou o esqueleto (RendimentoValorizacaoCalculator,
IRendimentoService/RendimentoService, integracao em AtivoService.AtualizarValorAtual
recebendo IRendimentoService); Kira criou os arquivos. Projeto principal compilou
limpo; projeto de testes quebrou temporariamente (esperado, corrigido no RED
da TASK-156). Commit 659cf37.

---

## TASK-156 — [REGRA CRITICA] RED: testes de valorizacao automatica

STATUS: CONCLUIDA
AGENT: mike
DEPENDENCIAS: TASK-155
FLUXO: Implementacao (rodada RED)
CONTEXTO A LER: regra-de-negocio.md item 8.4 INTEIRO (gatilho b)
ESCOPO: testes cobrindo: `RendimentoValorizacaoCalculator.Calcular` com delta positivo, delta negativo (desvalorizacao) e delta zero (retorna `null`); `RendimentoService.RegistrarValorizacaoAutomatica` cria `Rendimento(VALORIZACAO, origem=AUTOMATICO, valor=delta)` quando delta != 0, e NAO cria nada quando delta == 0; `AtivoService.AtualizarValorAtual`, ao ser chamado, gera exatamente UM `Rendimento(VALORIZACAO)` com o delta correto (integracao real, nao mock do calculator); chamar `AtualizarValorAtual` duas vezes com o MESMO valor nao duplica registro na segunda vez; duas edicoes sucessivas com valores diferentes geram dois registros distintos, `ObterHistorico` retorna ambos ordenados por `Data`. NAO escrever testes de `RegistrarDividendo` aqui (CRUD simples, sem TDD pesado — cobertura vem via TASK-161, testes HTTP).
CRITERIO DE ACEITE: testes compilam e falham por `NotImplementedException`, nunca por erro de compilacao.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances.Tests/Domain/RendimentoValorizacaoCalculatorTests.cs` (novo), `MyFinances/MyFinances.Tests/Services/RendimentoServiceTests.cs` (novo), `MyFinances/MyFinances.Tests/Services/AtivoServiceTests.cs`
NAO FAZER: nao implementar logica real; nao testar o gatilho (a)/aporte (NO-OP, fora de escopo ate confirmacao do usuario).
RETORNO ESPERADO: RED confirmado.
HISTORICO: mike escreveu 20 testes (RendimentoValorizacaoCalculatorTests,
RendimentoServiceTests, integracao em AtivoServiceTests). 2 tentativas cairam por
limite de sessao no meio da escrita (retomadas do ponto onde pararam, sem retrabalho
duplicado). RED confirmado: 20/20 falhando por NotImplementedException, 0 erro de
compilacao. Commit 51a5d43.

---

## TASK-157 — [REGRA CRITICA] GREEN: implementar RendimentoService (valorizacao automatica + dividendo manual)

STATUS: CONCLUIDA
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
HISTORICO: levi implementou Calculator.Calcular (delta=novo-anterior, null se zero),
RegisterValorizacaoAutomatica/ObterHistorico/ObterResumoGeral e RegistrarDividendo
(valor>0, ativo existente/ativo, AtivoInativoException). 20/20 testes da TASK-156
GREEN, 476/476 suite completa sem regressao. Commit f072a8b.

---

## TASK-158 — Confirmar GREEN valorizacao (mike)

STATUS: CONCLUIDA
AGENT: mike
DEPENDENCIAS: TASK-157
FLUXO: Implementacao (rodada GREEN)
CONTEXTO A LER: nenhum — so roda a suite da TASK-156
ESCOPO: rodar `RendimentoValorizacaoCalculatorTests`/`RendimentoServiceTests`/`AtivoServiceTests` (parte nova) e confirmar GREEN.
CRITERIO DE ACEITE: 100% GREEN ou relatorio de bug.
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao reescrever teste.
RETORNO ESPERADO: confirmacao GREEN ou relatorio estruturado.
HISTORICO: mike confirmou GREEN — 20/20 filtro Rendimento, 476/476 suite completa.
Sem regressao.

---

## TASK-159 — Style: revisao RendimentoService (regra critica + dividendo)

STATUS: CONCLUIDA
AGENT: style
DEPENDENCIAS: TASK-158
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 8.4; clean-code.md
ESCOPO: validar a formula de valorizacao (delta, aceita negativo, NO-OP em delta zero), a integracao com `AtivoService.AtualizarValorAtual` (ordem de captura do valor anterior), e a validacao de `RegistrarDividendo` (valor>0, ativo existente/ativo).
CRITERIO DE ACEITE: veredito (APROVADO ou tarefa de correcao no esquema padrao).
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + achados.
HISTORICO: style APROVADO sem ressalvas. Confirmou formula/NO-OP em delta zero,
atomicidade da integracao com AtivoService (mesmo DbContext scoped), validacao de
RegistrarDividendo, isolamento total de Rendimento vs saldo/Conta, e gatilho (a)
NO-OP.

---

## TASK-160 — Controller/DTOs de Rendimento (dividendo + historico + resumo)

STATUS: CONCLUIDA
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
HISTORICO: levi implementou os 3 endpoints (POST/GET /{id}/rendimentos, GET
/rendimentos-resumo) + DTOs RegistrarDividendoRequest/RendimentoResponse/
RendimentosResumoResponse. AtivoInativoException->404. Build limpo. Commit 21aeeff.

---

## TASK-161 — Testes HTTP dos endpoints de Rendimento (cobre dividendo CRUD)

STATUS: CONCLUIDA
AGENT: mike
DEPENDENCIAS: TASK-160
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 8.4
ESCOPO: testes HTTP cobrindo: registrar dividendo (201, valor/data corretos, origem=MANUAL); dividendo com valor<=0 -> 400; dividendo em ativo inexistente/desativado -> 404; `GET .../rendimentos` retorna dividendo + valorizacao juntos, ordenados; `PATCH .../valor-atual` seguido de `GET .../rendimentos` mostra o `Rendimento(VALORIZACAO)` gerado automaticamente (prova end-to-end do gatilho b via HTTP); `GET /api/ativos/rendimentos-resumo` soma corretamente totais de dividendo e valorizacao de multiplos ativos.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances.Tests/Controllers/AtivosControllerTests.cs`
NAO FAZER: nao alterar controller/service sem reportar.
RETORNO ESPERADO: testes passando ou relatorio de bug.
HISTORICO: mike escreveu 7 testes HTTP cobrindo dividendo (sucesso/valor invalido/
ativo inexistente/desativado), historico combinado, valorizacao automatica end-to-end
via PATCH, e resumo agregado multi-ativo. 25/25 GREEN, 483/483 suite completa.
Commit 55633a3.

---

## TASK-162 — Front: camada de dados (types/api/hooks) de Rendimento

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: TASK-160
FLUXO: Implementacao
CONTEXTO A LER: stack.md "Frontend (React)"; `features/investimentos/{types.ts,api.ts,query-keys.ts}` (padrao existente)
ESCOPO: adicionar em `types.ts` (`TipoRendimento`, `RendimentoResponse`, `RegistrarDividendoRequest`, `RendimentosResumoResponse`), `api.ts` (`registrarDividendo`, `listarRendimentosDoAtivo`, `buscarRendimentosResumo`), `query-keys.ts` (`investimentosKeys.rendimentos(ativoId)`, `investimentosKeys.rendimentosResumo()`), novos hooks `useRegistrarDividendo` (invalida `rendimentos(ativoId)` + `rendimentosResumo`), `useHistoricoRendimentos(ativoId)`, `useRendimentosResumo`.
CRITERIO DE ACEITE: hooks tipados, sem `any`, invalidacao de cache cruzada apos registrar dividendo.
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/investimentos/types.ts`, `MyFinanceFrontEnd/src/features/investimentos/api.ts`, `MyFinanceFrontEnd/src/features/investimentos/query-keys.ts`, `MyFinanceFrontEnd/src/features/investimentos/hooks/useRegistrarDividendo.ts` (novo), `MyFinanceFrontEnd/src/features/investimentos/hooks/useHistoricoRendimentos.ts` (novo), `MyFinanceFrontEnd/src/features/investimentos/hooks/useRendimentosResumo.ts` (novo)
NAO FAZER: nao renderizar UI aqui.
RETORNO ESPERADO: hooks tipados prontos para consumo.
HISTORICO: hanzo criou types/api/query-keys + useRegistrarDividendo/
useHistoricoRendimentos/useRendimentosResumo. Build/typecheck limpo. Commit
15d570a. useHistoricoRendimentos removido depois na TASK-165 (achado de codigo
orfao, sem consumidor).

---

## TASK-163 — Front: formulario de cadastro de dividendo vinculado ao ativo

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: TASK-162
FLUXO: Implementacao
CONTEXTO A LER: identidade-visual.md; regra-de-negocio.md item 8.4
ESCOPO: `FormRegistrarDividendo.tsx` (valor + data), acao disponivel a partir de `AtivoItem.tsx`/`AtivoCard.tsx` ("Registrar dividendo"), disparando `useRegistrarDividendo`. Validacao client-side de valor > 0.
CRITERIO DE ACEITE: dividendo registrado aparece no historico do ativo apos sucesso (cache invalidado).
ARQUIVOS PERMITIDOS: `MyFinanceFrontEnd/src/features/investimentos/components/FormRegistrarDividendo.tsx` (novo), `MyFinanceFrontEnd/src/features/investimentos/lib/validarDividendo.ts` (novo), `MyFinanceFrontEnd/src/features/investimentos/components/AtivoItem.tsx`, `MyFinanceFrontEnd/src/features/investimentos/components/AtivoCard.tsx`
NAO FAZER: nao expor campo `tipo`/`origem` no form (sempre DIVIDENDO/MANUAL, decidido no backend).
RETORNO ESPERADO: formulario funcional integrado a tela de ativo.
HISTORICO: hanzo criou FormRegistrarDividendo.tsx + validarDividendo.ts, integrado
como acao "Registrar dividendo" em AtivoCard/AtivoItem (modal, mesmo padrao de
ModalNovoAtivo). Build/typecheck limpo. Commit 82610b0. Nao houve validacao visual
em browser real (sem Postgres local no ambiente); cobertura vem do build + testes
HTTP que exercitam o mesmo contrato.

---

## TASK-164 — Front: grafico de rendimento por tipo (dividendo vs valorizacao)

STATUS: CONCLUIDA
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
HISTORICO: bloqueada ate TASK-124/125 (Bloco F) fecharem; PR #54 do Bloco F mergeado
em main enquanto esta sessao rodava. Kira fez merge de origin/main no worktree
(8 arquivos com conflito: AtivoService.cs, Program.cs, AtivosController.cs,
AtivoServiceTests.cs, AtivosControllerTests.cs, api.ts, AtivoCard.tsx, AtivoItem.tsx
— resolvidos preservando logica de rendimento E aporte, 541/541 testes apos merge).
Com 124/125 ja concluidas pelo merge, hanzo criou GraficoRendimentosPorTipo.tsx +
agruparRendimentosPorMes.ts (barras empilhadas, sem misturar TipoAtivo/TipoRendimento).
Build limpo. Commit f75ca2f.

---

## TASK-165 — Style review Bloco M (Rendimentos)

STATUS: CONCLUIDA
AGENT: style
DEPENDENCIAS: TASK-160, TASK-161, TASK-163, TASK-164
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 8.4 INTEIRO; clean-code.md; identidade-visual.md
ESCOPO: revisao geral do bloco Rendimentos (backend TASK-153/154/157/159 + front TASK-162/163/164), com atencao especial a: Rendimento nunca influenciar `saldo_projetado`/saldo de conta, gatilho (a)/aporte permanecer NO-OP documentado, e `POST /rendimentos` nao aceitar `tipo`/`origem` de fora.
CRITERIO DE ACEITE: veredito (APROVADO ou tarefa de correcao no esquema padrao).
ARQUIVOS PERMITIDOS: nenhum
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + achados.
HISTORICO: style APROVADO com ressalva nao-bloqueante (hook
useHistoricoRendimentos.ts orfao, sem consumidor). Confirmou build/541 testes
verdes/front OK, ordem correta em AtualizarValorAtual pos-merge, gatilho (a)/aporte
sem tocar Rendimento, POST /rendimentos sem campo tipo/origem exposto, isolamento
de saldo, e grafico sem misturar taxonomias. Kira removeu o hook orfao inline
(commit 08ba273) e fechou o bloco.

---

## TASK-166 — Modelagem e Esqueleto: Assinatura de Cartao (recorrencia em cartao)

STATUS: CONCLUIDA
AGENT: killua
DEPENDENCIAS: vazio
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 6 (Conta fixa) e item 12 (Cartao de credito) INTEIROS; schema.dbml; stack.md
ESCOPO: Modelar uma recorrencia de compra vinculada a uma conta CARTAO (assinatura tipo Netflix/Spotify) que gere automaticamente uma Compra em cada fatura futura — hoje nao existe: criar_compra_cartao e avulsa, criar_compra_parcelada tem fim definido (N parcelas com valor dividido), e criar_conta_fixa gera lancamento de fluxo de caixa em conta comum, nao compra de fatura de cartao.
CRITERIO DE ACEITE:
1. Nova entidade modelada (assinatura_cartao: conta_id restrito a tipo CARTAO, descricao, valor, dia_referencia, categoria_id opcional, ativa) documentada em schema.dbml e como novo item 16 da regra-de-negocio.md.
2. Regra de geracao definida e documentada: fatura resolvida via FaturaCicloService, gerando ocorrencia atual com idempotencia por ciclo/mes; desativar mantem compras ja geradas intocadas (fato historico).
3. Esqueleto de assinatura compilavel (Domain + DTOs + contrato de Service/Controller com NotImplementedException, Repositories, Configurations, DbContext e DI) entregue nos caminhos corretos conforme stack.md.
ARQUIVOS PERMITIDOS: context/regra-de-negocio.md, context/schema.dbml, esqueleto backend nos caminhos que killua definir conforme stack.md
NAO FAZER: Nao implementar logica de geracao real (fica para levi, apos TDD com mike). Nao decidir sozinho pontos que dependem do usuario — registrar como [REVISAR: ...] e sinalizar.
RETORNO ESPERADO: schema.dbml atualizado, novo item 16 documentado, esqueleto compilavel, e lista de pontos em aberto.
HISTORICO: killua modelou a entidade AssinaturaCartao, adicionou vinculo assinatura_cartao_id em lancamento e schema.dbml, documentou Item 16 em regra-de-negocio.md com 4 pontos [REVISAR: ...] identificados. Entregou esqueleto compilavel completo (Domain/AssinaturaCartao.cs, DTOs/AssinaturaCartao, Exceptions/AssinaturaCartaoNaoEncontradaException.cs, Repositories/IAssinaturaCartaoRepository.cs, Repositories/AssinaturaCartaoRepository.cs, Services/IAssinaturaCartaoService.cs, Services/AssinaturaCartaoService.cs, Controllers/AssinaturaCartaoController.cs, Configurations/AssinaturaCartaoConfiguration.cs, DbContext e DI). Build limpo e 630/630 testes verdes.

---

## TASK-167 — Testes RED: regra de Assinatura de Cartao (mike)

STATUS: CONCLUIDA (22 testes unitarios escritos, RED confirmado por NotImplementedException; 630 testes legados verdes)
AGENT: mike
DEPENDENCIAS: TASK-166
FLUXO: Implementacao (rodada RED — testes devem falhar por NotImplementedException)
CONTEXTO A LER: regra-de-negocio.md item 16 INTEIRO (decisoes do PO registradas); docs/assinatura-cartao.md; `AssinaturaCartaoService.cs`
ESCOPO: Escrever testes unitarios cobrindo:
1. Validacao de entrada: conta inexistente, conta inativa, conta nao-cartao (Banco ou Investimento) rejeitadas; valor <= 0 rejeitado; dia_referencia fora de 1-31 rejeitado; descricao vazia rejeitada.
2. Criacao e geracao no ciclo corrente: cria AssinaturaCartao com Ativa=true, gera Compra (Lancamento Debit, Status Pago, Manual true, FaturaId resolvido, AssinaturaCartaoId preenchido).
3. Idempotencia: chamar geracao 2x para o mesmo mes/ano nao duplica a compra.
4. Clamp de data: dia_referencia 31 em mes de 30 dias (ex: abril) gera compra no dia 30; dia 31 em fevereiro gera 28/29.
5. Edicao e propagacao: editar valor/descricao/categoria atualiza a compra vinculada SE a fatura estiver ABERTA; compra em fatura FECHADA ou PAGA nunca e alterada.
6. Desativacao: desativar assinatura mantem compras ja geradas intocadas.
7. Reativacao: reativar volta a gerar para o ciclo atual respeitando idempotencia.
8. Garantir geracao sob demanda ao consultar ciclo/fatura (`GarantirAssinaturasDoCicloAsync`).
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances.Tests/Services/AssinaturaCartaoServiceTests.cs` (novo)
NAO FAZER: Nao implementar logica no Service para fazer passar (isso e de levi na TASK-168).
RETORNO ESPERADO: Suite de testes compilando e falhando com NotImplementedException (RED confirmado).
HISTORICO: mike escreveu a suite completa de 22 testes em AssinaturaCartaoServiceTests.cs cobrindo todas as regras do Item 16: validacoes de conta/tipo/status, validacao de campos (valor, dia, descricao), geracao imediata de compra no ciclo, clamp de dia, idempotencia por mes/ano, propagacao de edicao apenas para fatura ABERTA (fechada/paga intocada), desativacao preservando fatos historicos, reativacao e geracao sob demanda (GarantirAssinaturasDoCicloAsync). 22/22 RED confirmados.

---

## TASK-168 — Implementacao GREEN: AssinaturaCartaoService e geracao de compras (levi)

STATUS: CONCLUIDA (652/652 testes verdes, 22/22 novos testes GREEN sem regressoes)
AGENT: levi
DEPENDENCIAS: TASK-167
FLUXO: Implementacao (rodada GREEN)
CONTEXTO A LER: regra-de-negocio.md item 16 INTEIRO; testes da TASK-167
ESCOPO: Implementar `AssinaturaCartaoService` contra os testes RED da TASK-167 ate todos ficarem GREEN.
Integrar a geracao sob demanda (`GarantirAssinaturasDoCicloAsync`) no fluxo de consulta de faturas (`FaturasController.ListarFaturas` / `FaturaCicloService`).
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Services/AssinaturaCartaoService.cs`, `MyFinances/MyFinances/Services/IAssinaturaCartaoService.cs`, `MyFinances/MyFinances/Services/FaturaCicloService.cs`, `MyFinances/MyFinances/Controllers/FaturasController.cs`
NAO FAZER: Nao alterar os arquivos de teste; nao quebrar os 630 testes existentes.
RETORNO ESPERADO: 100% dos testes da TASK-167 e da suite geral passando (GREEN).
HISTORICO: levi implementou AssinaturaCartaoService com injecao de repositorios e FaturaCicloService. Validacoes de entrada (campos obrigatorios, tipos de conta, dia 1-31), geracao de compra imediata com clamp de data, propagacao em edicao restrita a faturas abertas, desativacao preservando historico, reativacao com idempotencia e geracao sob demanda integrados. Integrado no FaturasController.ListarFaturas com injecao opcional retrocompativel. 652/652 testes verdes.

---

## TASK-169 — Style review: Assinatura de Cartao (style)

STATUS: CONCLUIDA (Veredito: APROVADO)
AGENT: style
DEPENDENCIAS: TASK-168
FLUXO: Implementacao
CONTEXTO A LER: regra-de-negocio.md item 16; clean-code.md; stack.md
ESCOPO: Revisar o codigo de `AssinaturaCartao` contra as regras de negocio e padroes de clean code (ausencia de logica no controller, DTOs corretos, isolamento de fatura fechada/paga, idempotencia sem race condition critica).
CRITERIO DE ACEITE: Veredito APROVADO ou tarefas de correcao.
ARQUIVOS PERMITIDOS: nenhum (apenas leitura e relatorio).
RETORNO ESPERADO: Veredito e apontamentos de style.
HISTORICO: style revisou o modulo de AssinaturaCartao. Controller limpo sem logica de dominio; DTOs corretos na pasta DTOs/AssinaturaCartao; isolamento estrito de faturas fechadas/pagas garantido na propagacao de edicao; desativacao preservando compras historicas; clamp de data robusto; idempotencia checada via ExisteCompraGerada; comentarios sem acentuacao; suite 100% verde (652 testes). Veredito: APROVADO sem pendencias.

---

## TASK-170 — Ferramentas MCP para Assinatura de Cartao (levi)

STATUS: CONCLUIDA (5 ferramentas criadas e registradas, build TypeScript 0 erros)
AGENT: levi
DEPENDENCIAS: TASK-168, TASK-169
FLUXO: Implementacao
CONTEXTO A LER: docs/assinatura-cartao.md; `mcp-server/src/tools/cartao.ts`; `mcp-server/src/tools/contasFixas.ts`
ESCOPO: Adicionar ferramentas no servidor MCP para assinaturas de cartao: `criar_assinatura_cartao`, `listar_assinaturas_cartao`, `editar_assinatura_cartao`, `desativar_assinatura_cartao`, `reativar_assinatura_cartao`.
ARQUIVOS PERMITIDOS: `mcp-server/src/tools/cartao.ts` (ou `mcp-server/src/tools/assinaturasCartao.ts`), `mcp-server/src/server.ts`
RETORNO ESPERADO: Ferramentas registradas e testadas via MCP.
HISTORICO: levi criou mcp-server/src/tools/assinaturasCartao.ts com as 5 ferramentas (criar, listar, editar, desativar, reativar) e formatacao propria; registrou em mcp-server/src/server.ts via registerAssinaturasCartaoTools. Build do TypeScript (npm run build) executado com 0 erros.





---

# Bloco Regras 2026-10-08 + realinhamento UI ao mockup (TASK-171..236)

## TASK-171 — Esqueleto: Apagar conta fixa e motor unico de limpeza de ocorrencias (item 6)

STATUS: CONCLUIDA
AGENT: killua
DEPENDENCIAS: vazio
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/regra-de-negocio.md (monolitico; itens 6 e 12), .claude/context/stack.md, .claude/context/clean-code.md, .claude/context/schema.dbml, MyFinances/MyFinances/Services/ContaFixaService.cs, MyFinances/MyFinances/Services/IContaFixaService.cs, MyFinances/MyFinances/Services/RecorrenciaGeradorService.cs, MyFinances/MyFinances/Services/IRecorrenciaGeradorService.cs, MyFinances/MyFinances/Controllers/ContaFixaController.cs
ESCOPO: Devolver esqueleto compilavel (Kira cria os arquivos) com `IContaFixaService.ApagarAsync(Guid contaFixaId) -> Task<(bool Sucesso, string? Erro)>`, `IRecorrenciaGeradorService.RemoverOcorrenciasNaoConsumadasAsync(Guid contaFixaId) -> Task<int>` (banco: Lancamento.Status=Pendente; cartao: Fatura.Status != Paga; reaproveitar o criterio da limpeza de periodicidade) e `DesvincularOcorrenciasConsumadasAsync(Guid contaFixaId) -> Task<int>` (ContaFixaId=null nas Pago / fatura Paga, valor/data/categoria intactos), corpos NotImplementedException; registrar que DesativarAsync hoje ignora compra de cartao (divergencia do item 6) e deve passar a usar o motor.
CRITERIO DE ACEITE: 1) assinaturas finais de interface e classe compilam (build limpo, 0 testes novos); 2) contrato descreve a ordem exata do Apagar: remover nao consumadas -> desvincular consumadas -> remover molde; 3) lista de testes que mike deve cobrir (banco, cartao, inexistente, ja desativada, Desativar corrigido).
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Services/IContaFixaService.cs, MyFinances/MyFinances/Services/ContaFixaService.cs, MyFinances/MyFinances/Services/IRecorrenciaGeradorService.cs, MyFinances/MyFinances/Services/RecorrenciaGeradorService.cs
NAO FAZER: nao implementar logica real; nao tocar controller; nao editar .claude/tasks.md; nao alterar regra-de-negocio.md.
RETORNO ESPERADO: texto dos esqueletos + contrato do fluxo + lista de casos de teste.
HISTORICO: Esqueleto gravado pelo Kira (ApagarAsync, RemoverOcorrenciasNaoConsumadasAsync, DesvincularOcorrenciasConsumadasAsync). Build ok. Achados killua: IContaFixaRepository sem Remover (liberar p/ levi na 173); predicado nao-consumada atual remove cartao Pendente em fatura Paga (corrigir com EhNaoConsumada).

---

## TASK-172 — Testes RED: Apagar conta fixa e Desativar corrigido (mike)

STATUS: CONCLUIDA
AGENT: mike
DEPENDENCIAS: TASK-171
FLUXO: Implementacao (rodada RED)
CONTEXTO A LER: .claude/context/regra-de-negocio.md (itens 6 e 12), MyFinances/MyFinances/Services/ContaFixaService.cs, MyFinances/MyFinances/Services/RecorrenciaGeradorService.cs, MyFinances/MyFinances.Tests/Services/ContaFixaServiceTests.cs, MyFinances/MyFinances.Tests/Services/RecorrenciaGeradorServiceTests.cs
ESCOPO: Escrever testes que falham por NotImplementedException: (1) apagar destino banco remove Lancamento Pendente, mantem Pago com ContaFixaId=null (valor/data/categoria iguais) e remove o molde; (2) apagar destino cartao remove compra em fatura Aberta/Fechada, mantem compra em fatura Paga desvinculada; (3) conta fixa inexistente -> (false, "Conta fixa nao encontrada"); (4) apagar conta ja desativada funciona; (5) nao toca ocorrencias de outra conta fixa; (6) DesativarAsync corrigido: cartao em fatura nao paga removido, Pago/fatura Paga intocados, molde fica Ativa=false.
CRITERIO DE ACEITE: 1) testes compilam e falham por logica ausente (NotImplementedException), nunca por erro de compilacao; 2) suite legada continua verde exceto testes existentes que contradigam o item 6 (reportar quais); 3) cada caso acima mapeado a um teste nomeado.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances.Tests/Services/ContaFixaApagarServiceTests.cs (novo)
NAO FAZER: nao implementar no service; nao editar testes existentes; nao editar .claude/tasks.md.
RETORNO ESPERADO: lista de testes + saida `dotnet test` mostrando RED + testes legados que conflitam com a regra.
HISTORICO: mike RED: ContaFixaApagarServiceTests (18 testes; 17 RED + 1 guarda). Causa das 2 falhas pre-existentes identificada (data fixa 2026-08-26).

---

## TASK-173 — GREEN: ApagarAsync + motor unico de limpeza + Desativar corrigido (levi)

STATUS: CONCLUIDA
AGENT: levi
DEPENDENCIAS: TASK-172
FLUXO: Implementacao (rodada GREEN)
CONTEXTO A LER: .claude/context/regra-de-negocio.md (itens 6 e 12), .claude/context/stack.md, .claude/context/clean-code.md, MyFinances/MyFinances.Tests/Services/ContaFixaApagarServiceTests.cs (leitura), MyFinances/MyFinances/Services/ContaFixaService.cs, MyFinances/MyFinances/Services/RecorrenciaGeradorService.cs, MyFinances/MyFinances/Repositories/IContaFixaRepository.cs, MyFinances/MyFinances/Repositories/ContaFixaRepository.cs
ESCOPO: Implementar ApagarAsync, RemoverOcorrenciasNaoConsumadasAsync e DesvincularOcorrenciasConsumadasAsync; DesativarAsync passa a usar o motor (banco Pendente + cartao fatura != Paga); adicionar Remover ao IContaFixaRepository se faltar.
CRITERIO DE ACEITE: 1) todos os testes da TASK-172 GREEN; 2) suite legada verde (ajustar teste legado so se contradizer o item 6, reportando); 3) service nao acessa DbContext direto.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Services/ContaFixaService.cs, MyFinances/MyFinances/Services/RecorrenciaGeradorService.cs, MyFinances/MyFinances/Services/IContaFixaService.cs, MyFinances/MyFinances/Services/IRecorrenciaGeradorService.cs, MyFinances/MyFinances/Repositories/IContaFixaRepository.cs, MyFinances/MyFinances/Repositories/ContaFixaRepository.cs
NAO FAZER: nao alterar o arquivo de teste da TASK-172; nao criar endpoint; nao criar migration (FK ja e SetNull); nao editar .claude/tasks.md.
RETORNO ESPERADO: arquivos tocados + saida `dotnet test` verde + divergencias com testes legados.
HISTORICO: levi GREEN 18/18, commit 4f38b10. EhNaoConsumada compartilhado (corrige bug cartao Pendente em fatura Paga na limpeza). Desvincular usa EhConsumada explicito: SUGERIDO em banco nao e removido nem desvinculado (pergunta aberta ao usuario). mike ajustou teste legado DesativarAsync_ExcluiLancamentoPendenteMantemPago e tornou 2 testes de RecorrenciaGerador independentes de data (32/32 verdes).

---

## TASK-174 — Controller: DELETE /api/contas-fixas/{id} (levi)

STATUS: CONCLUIDA
AGENT: levi
DEPENDENCIAS: TASK-173
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/regra-de-negocio.md (item 6), .claude/context/stack.md, MyFinances/MyFinances/Controllers/ContaFixaController.cs, MyFinances/MyFinances/Services/IContaFixaService.cs
ESCOPO: Adicionar `[HttpDelete("api/contas-fixas/{id}")]` chamando ApagarAsync: 204 no sucesso, 404 {erro} se nao encontrada.
CRITERIO DE ACEITE: 1) DELETE existente -> 204; 2) inexistente -> 404 com `{ erro }`; 3) controller sem logica de negocio.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Controllers/ContaFixaController.cs
NAO FAZER: nao alterar service; nao editar .claude/tasks.md.
RETORNO ESPERADO: diff do controller + build limpo.
HISTORICO: levi: DELETE /api/contas-fixas/{id} (204/404). Build ok.

---

## TASK-175 — Mike GREEN + testes HTTP: Apagar conta fixa

STATUS: CONCLUIDA
AGENT: mike
DEPENDENCIAS: TASK-174
FLUXO: Implementacao (rodada GREEN + HTTP)
CONTEXTO A LER: .claude/context/regra-de-negocio.md (item 6), MyFinances/MyFinances.Tests/Services/ContaFixaApagarServiceTests.cs, MyFinances/MyFinances.Tests/Controllers/ContaFixaControllerTests.cs, MyFinances/MyFinances/Controllers/ContaFixaController.cs
ESCOPO: Rodar os testes da TASK-172 (confirmar GREEN sem reescrever) e escrever testes HTTP do DELETE: 204 apaga e GET posterior 404; 404 inexistente; Pago permanece listado sem conta fixa; nao consumado some.
CRITERIO DE ACEITE: 1) testes da TASK-172 GREEN; 2) novos testes HTTP GREEN; 3) suite completa verde.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances.Tests/Controllers/ContaFixaApagarControllerTests.cs (novo)
NAO FAZER: nao alterar codigo de producao; se achar bug, reportar (relatorio de bug) em vez de corrigir; nao editar .claude/tasks.md.
RETORNO ESPERADO: saida `dotnet test` + relatorio de bug se houver.
HISTORICO: mike: 18 servico GREEN + 4 HTTP GREEN (22/22). Commit 2dcd80f.

---

## TASK-176 — Style review: Apagar conta fixa (style)

STATUS: CONCLUIDA
AGENT: style
DEPENDENCIAS: TASK-175
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/regra-de-negocio.md (itens 6 e 12), .claude/context/clean-code.md, .claude/context/stack.md, MyFinances/MyFinances/Services/ContaFixaService.cs, MyFinances/MyFinances/Services/RecorrenciaGeradorService.cs, MyFinances/MyFinances/Controllers/ContaFixaController.cs
ESCOPO: Revisar contra regra e clean-code: ordem remover->desvincular->apagar, criterio de nao consumada unico (sem duplicacao entre Desativar/Apagar/limpeza de periodicidade), controller magro.
CRITERIO DE ACEITE: veredito APROVADO ou tarefa(s) de correcao no esquema da Secao 4.
ARQUIVOS PERMITIDOS: nenhum (somente leitura)
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + achados.
HISTORICO: style: APROVADO. Ressalvas: Salvar explicito em DesativarAsync (despachado ao levi); 3 Salvar sem transacao no Apagar (risco aceito, idempotente). SUGERIDO em banco fica fora de remover/desvincular e e desvinculado pelo SetNull (decisao pendente do usuario).

---

## TASK-177 — Front: camada de dados de apagar conta fixa (hanzo)

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: TASK-174
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/stack.md ("Frontend (React)"), MyFinanceFrontEnd/src/features/contas-fixas/api.ts, MyFinanceFrontEnd/src/features/contas-fixas/query-keys.ts, MyFinanceFrontEnd/src/features/contas-fixas/hooks/useDesativarContaFixa.ts
ESCOPO: Adicionar `apagarContaFixa(id)` em api.ts e hook `useApagarContaFixa` (invalida contas-fixas e lancamentos/fluxo de caixa/projecao).
CRITERIO DE ACEITE: 1) hook tipado sem any; 2) invalidacao cruzada de cache (contas fixas + lancamentos + dashboard); 3) typecheck limpo.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/contas-fixas/api.ts, MyFinanceFrontEnd/src/features/contas-fixas/hooks/useApagarContaFixa.ts (novo)
NAO FAZER: nao renderizar UI; nao editar .claude/tasks.md.
RETORNO ESPERADO: hook pronto + typecheck.
HISTORICO: hanzo: apagarContaFixa + useApagarContaFixa (invalida contas-fixas, lancamentos, dashboard). Commit d412c4f.

---

## TASK-178 — Esqueleto: lancamento avulso x vinculado (guard de editar/excluir, item 18)

STATUS: CONCLUIDA
AGENT: killua
DEPENDENCIAS: vazio
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/regra-de-negocio.md (itens 5, 17 e 18), .claude/context/stack.md, .claude/context/clean-code.md, MyFinances/MyFinances/Services/LancamentoManualService.cs, MyFinances/MyFinances/Services/ILancamentoManualService.cs, MyFinances/MyFinances/Controllers/LancamentosController.cs, MyFinances/MyFinances/DTOs/LancamentoResponseDto.cs, MyFinances/MyFinances/DTOs/EditarLancamentoRequest.cs, MyFinances/MyFinances/Domain/Lancamento.cs
ESCOPO: Devolver esqueleto com: `Domain/LancamentoVinculoResolver` estatico (`EhAvulso(Lancamento)` = Manual e todos vinculos nulos; `ResolverOrigem(Lancamento)` -> enum OrigemVinculoLancamento {Avulso, ContaFixa, CompraParcelada, Cartao, ContaReceber, Transferencia, Conciliado, NaoManual} com precedencia documentada; storage AVULSO|CONTA_FIXA|COMPRA_PARCELADA|CARTAO|CONTA_RECEBER|TRANSFERENCIA|CONCILIADO|NAO_MANUAL), `Exceptions/LancamentoNaoAvulsoException(origem)`, campos novos em `LancamentoResponseDto` (Avulso, OrigemVinculo, CompraParceladaId, ParcelaNumero, TotalParcelas) e `EditarLancamentoRequest.ContaId` opcional; novas dependencias de `LancamentoManualService` (ICategoriaRepository); regra de erro HTTP (409 com `{ erro, origemVinculo }`).
CRITERIO DE ACEITE: 1) esqueleto compila (corpo NotImplementedException) com assinaturas finais; 2) documenta a regra de troca de tipo (categoria incompativel -> 400, UI envia null) e troca de conta (so conta manual ativa nao-Cartao); 3) lista de casos de teste para mike.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Domain/LancamentoVinculoResolver.cs (novo), MyFinances/MyFinances/Domain/OrigemVinculoLancamento.cs (novo), MyFinances/MyFinances/Exceptions/LancamentoNaoAvulsoException.cs (novo), MyFinances/MyFinances/DTOs/LancamentoResponseDto.cs, MyFinances/MyFinances/DTOs/EditarLancamentoRequest.cs, MyFinances/MyFinances/Services/ILancamentoManualService.cs
NAO FAZER: nao implementar logica; nao alterar LancamentoManualService.cs nesta task; nao editar .claude/tasks.md.
RETORNO ESPERADO: texto dos esqueletos + contrato de erros + casos de teste.
HISTORICO: Esqueleto gravado (LancamentoVinculoResolver, OrigemVinculoLancamento, LancamentoNaoAvulsoException, campos novos em LancamentoResponseDto/EditarLancamentoRequest). Decisao usuario: categoria x tipo tambem na criacao. Levi precisa ICategoriaRepository no construtor de LancamentoManualService.

---

## TASK-179 — Testes RED: guard de avulso, editar e excluir lancamento (mike)

STATUS: CONCLUIDA
AGENT: mike
DEPENDENCIAS: TASK-178
FLUXO: Implementacao (rodada RED)
CONTEXTO A LER: .claude/context/regra-de-negocio.md (itens 5, 17 e 18), MyFinances/MyFinances/Domain/LancamentoVinculoResolver.cs, MyFinances/MyFinances/Services/LancamentoManualService.cs, MyFinances/MyFinances.Tests/Services/LancamentoManualServiceTests.cs, MyFinances/MyFinances.Tests/DTOs/LancamentoResponseDtoTests.cs
ESCOPO: Cobrir: resolver (um caso por vinculo: transferencia, fatura, conta_fixa, compra_parcelada, conta_receber, assinatura_cartao, conciliado_com; Manual=false -> nao avulso; precedencia com varios vinculos); EditarAsync/RemoverAsync com nao-avulso lancam LancamentoNaoAvulsoException sem persistir; avulso edita todos os campos; troca DEBIT->CREDIT com categoria DESPESA rejeitada e com null aceita; CREDIT so RECEITA; troca de conta para Cartao/inativa/inexistente rejeitada; Pago<->Pendente nos dois sentidos; Sugerido rejeitado; editar/excluir NAO altera Conta.SaldoManual; mapeamento dos campos novos do DTO.
CRITERIO DE ACEITE: 1) RED por NotImplementedException; 2) um teste nomeado por caso; 3) suite legada verde.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances.Tests/Domain/LancamentoVinculoResolverTests.cs (novo), MyFinances/MyFinances.Tests/Services/LancamentoAvulsoGuardServiceTests.cs (novo), MyFinances/MyFinances.Tests/DTOs/LancamentoResponseDtoVinculoTests.cs (novo)
NAO FAZER: nao implementar; nao editar testes existentes; nao editar .claude/tasks.md.
RETORNO ESPERADO: lista de testes + saida RED.
HISTORICO: mike RED: 80 testes (61 RED, 19 guardas). Casos categoria x tipo dependiam do construtor (PENDENTE-CONSTRUTOR). 2 testes legados com setup sem Manual=true: correcao autorizada ao levi na 180.

---

## TASK-180 — GREEN: guard de avulso e edicao completa de lancamento (levi)

STATUS: CONCLUIDA
AGENT: levi
DEPENDENCIAS: TASK-179
FLUXO: Implementacao (rodada GREEN)
CONTEXTO A LER: .claude/context/regra-de-negocio.md (itens 5 e 18), .claude/context/clean-code.md, MyFinances/MyFinances.Tests/Domain/LancamentoVinculoResolverTests.cs, MyFinances/MyFinances.Tests/Services/LancamentoAvulsoGuardServiceTests.cs, MyFinances/MyFinances.Tests/DTOs/LancamentoResponseDtoVinculoTests.cs, MyFinances/MyFinances/Services/LancamentoManualService.cs, MyFinances/MyFinances/Repositories/LancamentoRepository.cs
ESCOPO: Implementar resolver, DTO e LancamentoManualService.EditarAsync/RemoverAsync (guard, troca de conta e de tipo, validacao categoria x tipo); garantir Include de CompraParcelada para TotalParcelas.
CRITERIO DE ACEITE: 1) testes da TASK-179 GREEN; 2) MarcarComoPagoAsync continua permitido para lancamento vinculado (conta fixa pendente); 3) suite verde.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Domain/LancamentoVinculoResolver.cs, MyFinances/MyFinances/DTOs/LancamentoResponseDto.cs, MyFinances/MyFinances/Services/LancamentoManualService.cs, MyFinances/MyFinances/Repositories/ILancamentoRepository.cs, MyFinances/MyFinances/Repositories/LancamentoRepository.cs
NAO FAZER: nao alterar testes da TASK-179; nao mexer no controller; nao editar .claude/tasks.md.
RETORNO ESPERADO: arquivos tocados + `dotnet test` verde.
HISTORICO: levi implementou guard de avulso + edicao completa (ValidadorLancamentoManual). Commit 1cf674a.

---

## TASK-181 — Controller: 409 para lancamento nao avulso e ContaId na edicao (levi)

STATUS: CONCLUIDA
AGENT: levi
DEPENDENCIAS: TASK-180
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/regra-de-negocio.md (item 18), MyFinances/MyFinances/Controllers/LancamentosController.cs, MyFinances/MyFinances/Exceptions/LancamentoNaoAvulsoException.cs
ESCOPO: PUT e DELETE de `/api/contas/{contaId}/lancamentos/{lancamentoId}` retornam 409 `{ erro, origemVinculo }` ao capturar LancamentoNaoAvulsoException; PUT aceita `contaId` opcional no corpo; fluxo-caixa expoe os campos novos do DTO.
CRITERIO DE ACEITE: 1) nao avulso -> 409 com origemVinculo; 2) avulso -> 200/200; 3) demais erros seguem 400 `{ erro }`.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Controllers/LancamentosController.cs
NAO FAZER: nao alterar service; nao editar .claude/tasks.md.
RETORNO ESPERADO: diff + build limpo.
HISTORICO: levi: PUT/DELETE -> 409 { erro, origemVinculo }. Build ok.

---

## TASK-182 — Mike GREEN + testes HTTP: guard de avulso

STATUS: CONCLUIDA
AGENT: mike
DEPENDENCIAS: TASK-181
FLUXO: Implementacao (rodada GREEN + HTTP)
CONTEXTO A LER: .claude/context/regra-de-negocio.md (item 18), MyFinances/MyFinances.Tests/Controllers/LancamentosControllerTests.cs, MyFinances/MyFinances/Controllers/LancamentosController.cs
ESCOPO: Rodar testes da TASK-179 (GREEN) e escrever testes HTTP: editar/excluir lancamento de conta fixa, transferencia, fatura e conta a receber -> 409 com origemVinculo; avulso edita (incluindo trocar conta) e exclui; GET fluxo-caixa traz Avulso/OrigemVinculo/ParcelaNumero/TotalParcelas.
CRITERIO DE ACEITE: 1) TASK-179 GREEN; 2) testes HTTP GREEN; 3) suite completa verde.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances.Tests/Controllers/LancamentosAvulsoControllerTests.cs (novo)
NAO FAZER: nao alterar producao (reportar bug); nao editar .claude/tasks.md.
RETORNO ESPERADO: saida `dotnet test` + relatorio de bug se houver.
HISTORICO: mike GREEN + testes HTTP do guard de avulso. Commit 1cf674a.

---

## TASK-183 — Style review: guard de avulso (style)

STATUS: CONCLUIDA
AGENT: style
DEPENDENCIAS: TASK-182
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/regra-de-negocio.md (itens 5 e 18), .claude/context/clean-code.md, .claude/context/stack.md, MyFinances/MyFinances/Domain/LancamentoVinculoResolver.cs, MyFinances/MyFinances/Services/LancamentoManualService.cs, MyFinances/MyFinances/Controllers/LancamentosController.cs, MyFinances/MyFinances/DTOs/LancamentoResponseDto.cs
ESCOPO: Revisar regra (so avulso edita/exclui; categoria x tipo; conta destino; saldo_manual intocado) e clean-code (resolver unico, sem duplicar a checagem de vinculo).
CRITERIO DE ACEITE: veredito APROVADO ou tarefa(s) de correcao.
ARQUIVOS PERMITIDOS: nenhum (somente leitura)
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + achados.
HISTORICO: style aprovou na 2a rodada. Commit 1cf674a.

---

## TASK-184 — Esqueleto: lancamento parcelado em conta banco (item 17)

STATUS: CONCLUIDA
AGENT: killua
DEPENDENCIAS: TASK-178
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/regra-de-negocio.md (itens 5, 6, 12 subsecao Parcelamento, 17 e 18), .claude/context/stack.md, .claude/context/clean-code.md, .claude/context/schema.dbml, MyFinances/MyFinances/Services/ParcelamentoCalculator.cs, MyFinances/MyFinances/Services/ComprasParceladasService.cs, MyFinances/MyFinances/Domain/CompraParcelada.cs, MyFinances/MyFinances/Repositories/ICompraParceladaRepository.cs, MyFinances/MyFinances/Controllers/CartaoComprasParceladasController.cs
ESCOPO: Devolver esqueleto compilavel de `ILancamentoParceladoService/LancamentoParceladoService` (`CriarAsync(Guid contaId, CriarLancamentoParceladoRequest) -> (bool, LancamentoParceladoResponse?, string?)`; `CancelarAsync(Guid contaId, Guid compraParceladaId) -> (bool, int Excluidas, int Mantidas, string?)`), calculo puro de datas `Domain/ParcelamentoContaBancoDatas.Calcular(DateOnly primeira, int quantidade)` (dia da 1a parcela com clamp ao ultimo dia do mes, sem deriva: 31/jan -> 28/fev -> 31/mar), DTOs (CriarLancamentoParceladoRequest{Descricao, ValorTotal, QuantidadeParcelas, DataPrimeiraParcela, CategoriaId?, Status da 1a}, LancamentoParceladoResponse{CompraParceladaId, Parcelas[]}), rotas `POST /api/contas/{contaId}/lancamentos-parcelados` e `DELETE /api/contas/{contaId}/lancamentos-parcelados/{compraParceladaId}`, regra de transacao (BeginTransactionAsync do repositorio) e DI em Program.cs.
CRITERIO DE ACEITE: 1) esqueleto compila; 2) contrato cobre: so DEBIT, quantidade>=2, conta manual ativa nao-Cartao, 1a parcela com status do formulario (PENDENTE|PAGO) e demais PENDENTE, fatura_id null, reuso de ParcelamentoCalculator; 3) cancelamento exclui so Pendente, nao exclui parcela isolada, recusa compra de cartao (parcelas com fatura_id) e erro se nao ha Pendente.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Services/ILancamentoParceladoService.cs (novo), MyFinances/MyFinances/Services/LancamentoParceladoService.cs (novo), MyFinances/MyFinances/Domain/ParcelamentoContaBancoDatas.cs (novo), MyFinances/MyFinances/DTOs/CriarLancamentoParceladoRequest.cs (novo), MyFinances/MyFinances/DTOs/LancamentoParceladoResponse.cs (novo), MyFinances/MyFinances/Controllers/LancamentosParceladosController.cs (novo, so assinatura), MyFinances/MyFinances/Program.cs
NAO FAZER: nao implementar logica; nao mudar CriarLancamentoRequest; nao editar .claude/tasks.md.
RETORNO ESPERADO: texto dos esqueletos + casos de teste.
HISTORICO: Esqueleto gravado pelo Kira (ParcelamentoContaBancoDatas, ILancamentoParceladoService/LancamentoParceladoService, DTOs, LancamentosParceladosController, DI). Build ok. Ponto aberto: extrair validacao conta/categoria compartilhada com LancamentoManualService (decidir na 186).

---

## TASK-185 — Testes RED: lancamento parcelado em conta banco (mike)

STATUS: CONCLUIDA
AGENT: mike
DEPENDENCIAS: TASK-184
FLUXO: Implementacao (rodada RED)
CONTEXTO A LER: .claude/context/regra-de-negocio.md (itens 12 subsecao Parcelamento e 17), MyFinances/MyFinances/Services/LancamentoParceladoService.cs, MyFinances/MyFinances/Domain/ParcelamentoContaBancoDatas.cs, MyFinances/MyFinances.Tests/Services/ParcelamentoCalculatorTests.cs, MyFinances/MyFinances.Tests/Services/ComprasParceladasServiceIntegrationTests.cs
ESCOPO: Cobrir: datas (31/jan -> 28/fev -> 31/mar; 29/fev em ano bissexto; mesmo dia nos demais); criar N lancamentos DEBIT Manual fatura_id null, mesmo compra_parcelada_id, parcela_numero 1..N, descricao/categoria/conta iguais; R$100 em 3x = 33,33+33,33+33,34; 1a parcela com status pedido e demais Pendente; CompraParcelada com metadados; quantidade<2, valor<=0, descricao vazia, conta Cartao/inativa/inexistente rejeitadas; Sugerido rejeitado; falha no meio faz rollback; cancelar exclui so Pendente e mantem Pago; sem Pendente -> erro; compra de cartao (fatura_id) rejeitada; compra de outra conta -> nao encontrada.
CRITERIO DE ACEITE: 1) RED por NotImplementedException; 2) um teste por caso; 3) suite legada verde.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances.Tests/Domain/ParcelamentoContaBancoDatasTests.cs (novo), MyFinances/MyFinances.Tests/Services/LancamentoParceladoServiceTests.cs (novo)
NAO FAZER: nao implementar; nao editar testes existentes; nao editar .claude/tasks.md.
RETORNO ESPERADO: lista de testes + saida RED.
HISTORICO: mike RED do LancamentoParceladoService + ParcelamentoContaBancoDatas. Commit 1cf674a.

---

## TASK-186 — GREEN: LancamentoParceladoService (levi)

STATUS: CONCLUIDA
AGENT: levi
DEPENDENCIAS: TASK-185, TASK-180
FLUXO: Implementacao (rodada GREEN)
CONTEXTO A LER: .claude/context/regra-de-negocio.md (itens 12 subsecao Parcelamento e 17), .claude/context/clean-code.md, MyFinances/MyFinances.Tests/Domain/ParcelamentoContaBancoDatasTests.cs, MyFinances/MyFinances.Tests/Services/LancamentoParceladoServiceTests.cs, MyFinances/MyFinances/Services/ComprasParceladasService.cs, MyFinances/MyFinances/Services/ParcelamentoCalculator.cs, MyFinances/MyFinances/Repositories/ILancamentoRepository.cs, MyFinances/MyFinances/Repositories/ICompraParceladaRepository.cs
ESCOPO: Implementar ParcelamentoContaBancoDatas e LancamentoParceladoService (criar com transacao, cancelar so Pendente); adicionar a LancamentoRepository o que faltar (listar parcelas por compra_parcelada_id) e a CompraParceladaRepository (obter com lancamentos).
CRITERIO DE ACEITE: 1) testes da TASK-185 GREEN; 2) lancamento com compra_parcelada_id segue recusado por editar/excluir isolado (guard da TASK-180); 3) suite verde.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Domain/ParcelamentoContaBancoDatas.cs, MyFinances/MyFinances/Services/LancamentoParceladoService.cs, MyFinances/MyFinances/Services/ILancamentoParceladoService.cs, MyFinances/MyFinances/Repositories/ILancamentoRepository.cs, MyFinances/MyFinances/Repositories/LancamentoRepository.cs, MyFinances/MyFinances/Repositories/ICompraParceladaRepository.cs, MyFinances/MyFinances/Repositories/CompraParceladaRepository.cs
NAO FAZER: nao alterar testes da TASK-185; nao implementar controller; nao editar .claude/tasks.md.
RETORNO ESPERADO: arquivos tocados + `dotnet test` verde.
HISTORICO: levi GREEN do service. Commit 1cf674a.

---

## TASK-187 — Controller: lancamentos parcelados (criar e cancelar) (levi)

STATUS: CONCLUIDA
AGENT: levi
DEPENDENCIAS: TASK-186, TASK-181
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/regra-de-negocio.md (item 17), MyFinances/MyFinances/Controllers/LancamentosParceladosController.cs, MyFinances/MyFinances/Services/ILancamentoParceladoService.cs
ESCOPO: Implementar `POST /api/contas/{contaId}/lancamentos-parcelados` (201 com LancamentoParceladoResponse; 400 `{ erro }`) e `DELETE /api/contas/{contaId}/lancamentos-parcelados/{compraParceladaId}` (200 `{ excluidas, mantidas }`; 400 `{ erro }`).
CRITERIO DE ACEITE: 1) contratos acima; 2) controller sem logica; 3) Program.cs registra o service (se ainda nao).
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Controllers/LancamentosParceladosController.cs, MyFinances/MyFinances/Program.cs
NAO FAZER: nao alterar service; nao editar .claude/tasks.md.
RETORNO ESPERADO: diff + build limpo.
HISTORICO: levi: POST 201/400, DELETE 200 {excluidas, mantidas}/404/400. Bug de mantidas negativo corrigido (calculado antes do Remover). Commit 946b4e7.

---

## TASK-188 — Mike GREEN + testes HTTP: lancamentos parcelados

STATUS: CONCLUIDA
AGENT: mike
DEPENDENCIAS: TASK-187
FLUXO: Implementacao (rodada GREEN + HTTP)
CONTEXTO A LER: .claude/context/regra-de-negocio.md (item 17), MyFinances/MyFinances.Tests/Controllers/CartaoComprasParceladasControllerTests.cs, MyFinances/MyFinances/Controllers/LancamentosParceladosController.cs
ESCOPO: Rodar testes da TASK-185 (GREEN) e escrever testes HTTP: POST cria N lancamentos visiveis no fluxo-caixa com ParcelaNumero/TotalParcelas; validacoes -> 400; DELETE cancela so Pendente; DELETE de uma parcela isolada pelo endpoint de lancamento -> 409; compra de cartao -> 400.
CRITERIO DE ACEITE: 1) TASK-185 GREEN; 2) HTTP GREEN; 3) suite verde.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances.Tests/Controllers/LancamentosParceladosControllerTests.cs (novo)
NAO FAZER: nao alterar producao (reportar bug); nao editar .claude/tasks.md.
RETORNO ESPERADO: saida `dotnet test` + relatorio de bug se houver.
HISTORICO: mike: LancamentosParceladosControllerTests (15) verdes. Commit 946b4e7.

---

## TASK-189 — Style review: lancamento parcelado em conta banco (style)

STATUS: CONCLUIDA
AGENT: style
DEPENDENCIAS: TASK-188
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/regra-de-negocio.md (itens 12 subsecao Parcelamento e 17), .claude/context/clean-code.md, .claude/context/stack.md, MyFinances/MyFinances/Services/LancamentoParceladoService.cs, MyFinances/MyFinances/Domain/ParcelamentoContaBancoDatas.cs, MyFinances/MyFinances/Controllers/LancamentosParceladosController.cs
ESCOPO: Revisar regra (sem edicao de valor/quantidade, cancelar so Pendente, sem antecipacao em banco, reuso de ParcelamentoCalculator, transacao) e clean-code.
CRITERIO DE ACEITE: veredito APROVADO ou tarefa(s) de correcao.
ARQUIVOS PERMITIDOS: nenhum (somente leitura)
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + achados.
HISTORICO: style APROVADO (ressalva: Created aponta para rota sem GET, padrao ja existente). Junto foi aprovado e commitado o GET /faturas/{faturaId}/compras (57cd3a7; ressalva: [FromServices] no FaturasController, migrar ao construtor quando mexer de novo).

---

## TASK-190 — Front: camada de dados de lancamentos (vinculo, editar conta, parcelado) (hanzo)

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: TASK-181, TASK-187
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/stack.md ("Frontend (React)"), MyFinanceFrontEnd/src/features/lancamentos/types.ts, MyFinanceFrontEnd/src/features/lancamentos/api.ts, MyFinanceFrontEnd/src/features/lancamentos/query-keys.ts, MyFinanceFrontEnd/src/features/lancamentos/hooks/useEditarLancamento.ts, MyFinanceFrontEnd/src/features/lancamentos/hooks/useRemoverLancamento.ts, MyFinanceFrontEnd/src/features/lancamentos/hooks/useCriarLancamento.ts
ESCOPO: Em types.ts adicionar Avulso, OrigemVinculo, CompraParceladaId, ParcelaNumero, TotalParcelas ao lancamento e `contaId?` ao request de edicao; em api.ts adicionar `criarLancamentoParcelado` e `cancelarLancamentoParcelado`; hooks `useCriarLancamentoParcelado` e `useCancelarLancamentoParcelado` (invalidam fluxo de caixa, dashboard e limites); mapear 409 de editar/remover num erro tipado com `origemVinculo`; helper puro em lib/ que traduz origemVinculo em rotulo+rota de destino (ex.: CONTA_FIXA -> "Contas fixas", /contas-fixas).
CRITERIO DE ACEITE: 1) tipos sem any; 2) invalidacao de cache correta; 3) typecheck limpo.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/lancamentos/types.ts, MyFinanceFrontEnd/src/features/lancamentos/api.ts, MyFinanceFrontEnd/src/features/lancamentos/query-keys.ts, MyFinanceFrontEnd/src/features/lancamentos/hooks/useEditarLancamento.ts, MyFinanceFrontEnd/src/features/lancamentos/hooks/useRemoverLancamento.ts, MyFinanceFrontEnd/src/features/lancamentos/hooks/useCriarLancamentoParcelado.ts (novo), MyFinanceFrontEnd/src/features/lancamentos/hooks/useCancelarLancamentoParcelado.ts (novo), MyFinanceFrontEnd/src/features/lancamentos/lib/destinoDeEdicao.ts (novo)
NAO FAZER: nao renderizar UI; nao editar .claude/tasks.md.
RETORNO ESPERADO: tipos/hooks prontos + typecheck.
HISTORICO: 2026-10-13 (hanzo). Camada de dados do parcelado em conta banco: types/api/hooks useCriarLancamentoParcelado/useCancelarLancamentoParcelado, destinoDeEdicao (lib), ApiError.body em shared/api/client. Style rodada 1 PRECISA CORRIGIR (clean-code: duplicacao de campo de categoria, datas, logica pura em components, CLASSE_CAMPO, casts) -> rodada 2 APROVADO. Datas centralizadas em shared/lib/datas.ts; dica do destino de edicao em DestinoDeEdicao.dica.

---

## TASK-191 — Esqueleto: ativo vinculado a conta de investimento (item 8.5)

STATUS: CONCLUIDA
AGENT: killua
DEPENDENCIAS: vazio
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/regra-de-negocio.md (itens 8, 8.1 a 8.5 e 10), .claude/context/stack.md, .claude/context/clean-code.md, .claude/context/schema.dbml, MyFinances/MyFinances/Domain/Ativo.cs, MyFinances/MyFinances/Domain/Conta.cs, MyFinances/MyFinances/Services/AtivoService.cs, MyFinances/MyFinances/Services/IAtivoService.cs, MyFinances/MyFinances/Services/ContaService.cs, MyFinances/MyFinances/Services/IContaService.cs, MyFinances/MyFinances/Repositories/IAtivoRepository.cs, MyFinances/MyFinances/Controllers/AtivosController.cs, MyFinances/MyFinances/Controllers/ContasController.cs, MyFinances/MyFinances/DTOs/Conta/ContaResponse.cs, MyFinances/MyFinances/DTOs/Ativo/CriarAtivoRequest.cs, MyFinances/MyFinances/DTOs/Ativo/AtivoResponse.cs
ESCOPO: Devolver esqueleto e modelagem: `Ativo.ContaId` (Guid?) + `Conta`, `Instituicao` anulavel; `Domain/ContaInvestimentoSaldoCalculator` (`CalcularSaldo(Conta, IEnumerable<Ativo> ativosDaConta)` = soma valor_atual dos ativos ativos vinculados, senao saldo_manual; `PossuiAtivoAtivoVinculado`; `CalcularTotalInvestido(contasInvestimentoAtivas, ativosAtivos)` = saldos das contas + valor_atual dos ativos ativos SEM conta); metodos em IAtivoService (`CriarAtivo` com contaId obrigatorio sem instituicao; `VincularConta(ativoId, contaId)` nunca volta a null) e IContaService (saldo calculado, `DesativarConta` recusa, `AtualizarSaldoManual` recusa); excecoes (`ContaInvestimentoInvalidaParaAtivoException`, `ContaPossuiAtivoAtivoException` -> 409, `SaldoCalculadoNaoEditavelException` -> 422); IAtivoRepository (ListarAtivosPorConta, ExisteAtivoAtivoVinculado); DTOs (CriarAtivoRequest.ContaId sem Instituicao; AtivoResponse ContaId/ContaNome/SemConta; ContaResponse.Saldo calculado + SaldoCalculado bool; `PATCH /api/ativos/{id}/conta`); spec da migration (ativo.conta_id uuid null FK conta ON DELETE RESTRICT + indice; ativo.instituicao nullable); schema.dbml via skill alterar-context.
CRITERIO DE ACEITE: 1) esqueleto compila (NotImplementedException; FakeAtivoRepository ajustado so para compilar); 2) contrato cobre todas as frases do 8.5 (obrigatorio so em ativo novo, antigo fica sem conta, trocar nunca null, aporte nao mexe em conta); 3) lista de casos de teste.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Domain/Ativo.cs, MyFinances/MyFinances/Domain/ContaInvestimentoSaldoCalculator.cs (novo), MyFinances/MyFinances/Exceptions/ContaInvestimentoInvalidaParaAtivoException.cs (novo), MyFinances/MyFinances/Exceptions/ContaPossuiAtivoAtivoException.cs (novo), MyFinances/MyFinances/Exceptions/SaldoCalculadoNaoEditavelException.cs (novo), MyFinances/MyFinances/Services/IAtivoService.cs, MyFinances/MyFinances/Services/AtivoService.cs, MyFinances/MyFinances/Services/IContaService.cs, MyFinances/MyFinances/Services/ContaService.cs, MyFinances/MyFinances/Repositories/IAtivoRepository.cs, MyFinances/MyFinances/Repositories/AtivoRepository.cs, MyFinances/MyFinances/DTOs/Ativo/CriarAtivoRequest.cs, MyFinances/MyFinances/DTOs/Ativo/AtivoResponse.cs, MyFinances/MyFinances/DTOs/Conta/ContaResponse.cs, MyFinances/MyFinances.Tests/Helpers/FakeAtivoRepository.cs, .claude/context/schema.dbml
NAO FAZER: nao implementar logica real; nao criar migration; nao mexer em Controllers; nao editar .claude/tasks.md.
RETORNO ESPERADO: texto dos esqueletos + spec da migration + casos de teste.
HISTORICO: Esqueleto gravado (Ativo.ContaId/Conta, Instituicao nullable, ContaInvestimentoSaldoCalculator, 3 excecoes, repo, overload CriarAtivo por contaId + antigo Obsolete, VincularConta, ObterSaldo, ContaResponse.SaldoCalculado). Construtores antigos mantidos (transitorio). schema.dbml pendente (via alterar-context). Levi: AtivoConfiguration sem IsRequired em Instituicao, FK/indice, Include Conta, migration.

---

## TASK-192 — Testes RED: saldo calculado, totais e travas de conta de investimento (mike)

STATUS: CONCLUIDA
AGENT: mike
DEPENDENCIAS: TASK-191
FLUXO: Implementacao (rodada RED)
CONTEXTO A LER: .claude/context/regra-de-negocio.md (itens 8, 8.5 e 10), MyFinances/MyFinances/Domain/ContaInvestimentoSaldoCalculator.cs, MyFinances/MyFinances/Services/AtivoService.cs, MyFinances/MyFinances/Services/ContaService.cs, MyFinances/MyFinances.Tests/Services/AtivoServiceTests.cs, MyFinances/MyFinances.Tests/Services/ContaServiceTests.cs, MyFinances/MyFinances.Tests/Helpers/FakeAtivoRepository.cs
ESCOPO: Cobrir: calculador (com ativo ativo vinculado = soma de valor_atual; so inativos = ultimo saldo_manual; nenhum = saldo_manual (null -> 0); varios ativos; total = contas investimento + ativos ativos sem conta, sem somar vinculado duas vezes; conta inativa fora); service (criar ativo exige conta -> campo obrigatorio; conta inexistente/inativa/nao-investimento rejeitada; instituicao nao pedida; vincular ativo antigo; trocar de conta preserva aportes e rendimentos; nunca null); ContaService (desativar conta com ativo ativo vinculado -> ContaPossuiAtivoAtivoException; com ativos so inativos -> permitido; AtualizarSaldoManual com ativo ativo -> SaldoCalculadoNaoEditavelException; sem ativo -> permitido; aporte nao altera saldo da conta; AtualizarValorAtual altera saldo calculado).
CRITERIO DE ACEITE: 1) RED por NotImplementedException; 2) um teste por caso; 3) suite legada verde.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances.Tests/Domain/ContaInvestimentoSaldoCalculatorTests.cs (novo), MyFinances/MyFinances.Tests/Services/AtivoContaVinculoServiceTests.cs (novo), MyFinances/MyFinances.Tests/Services/ContaInvestimentoTravasServiceTests.cs (novo)
NAO FAZER: nao implementar; nao editar testes existentes (se testes legados de CriarAtivo quebrarem pela nova assinatura, reportar); nao editar .claude/tasks.md.
RETORNO ESPERADO: lista de testes + saida RED + testes legados afetados.
HISTORICO: mike RED: 48 testes (44 RED, 4 guardas do lado negativo).

---

## TASK-193 — GREEN: ativo vinculado a conta, saldo calculado e travas + migration (levi)

STATUS: CONCLUIDA
AGENT: levi
DEPENDENCIAS: TASK-192
FLUXO: Implementacao (rodada GREEN)
CONTEXTO A LER: .claude/context/regra-de-negocio.md (itens 8.5 e 10), .claude/context/clean-code.md, .claude/context/stack.md, MyFinances/MyFinances.Tests/Domain/ContaInvestimentoSaldoCalculatorTests.cs, MyFinances/MyFinances.Tests/Services/AtivoContaVinculoServiceTests.cs, MyFinances/MyFinances.Tests/Services/ContaInvestimentoTravasServiceTests.cs, MyFinances/MyFinances/Infrastructure/Configurations/AtivoConfiguration.cs, MyFinances/MyFinances/Repositories/AtivoRepository.cs
ESCOPO: Implementar calculador, AtivoService (criar com conta, vincular/trocar), ContaService (saldo calculado, recusas, total sem dupla contagem), repositorio, AtivoConfiguration (FK conta_id, instituicao nullable) e a migration EF aditiva.
CRITERIO DE ACEITE: 1) testes da TASK-192 GREEN; 2) migration aditiva e reversivel, ativos existentes ficam com conta_id null; 3) suite verde (ajustar testes legados de CriarAtivo para a nova assinatura, reportando).
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Domain/ContaInvestimentoSaldoCalculator.cs, MyFinances/MyFinances/Services/AtivoService.cs, MyFinances/MyFinances/Services/ContaService.cs, MyFinances/MyFinances/Repositories/AtivoRepository.cs, MyFinances/MyFinances/Repositories/ContaRepository.cs, MyFinances/MyFinances/Repositories/IContaRepository.cs, MyFinances/MyFinances/Infrastructure/Configurations/AtivoConfiguration.cs, MyFinances/MyFinances/Migrations/ (arquivos novos da migration + MyFinancesDbContextModelSnapshot.cs), MyFinances/MyFinances.Tests/Services/AtivoServiceTests.cs, MyFinances/MyFinances.Tests/Helpers/FakeAtivoRepository.cs
NAO FAZER: nao alterar testes da TASK-192; nao tocar controllers; nao editar .claude/tasks.md.
RETORNO ESPERADO: arquivos + `dotnet test` verde + nome da migration.
HISTORICO: levi: ContaInvestimentoSaldoCalculator, AtivoService por contaId, ContaService ObterSaldo + travas, AtivoConfiguration, 3 migrations (AddContaIdToAtivo, AddAssinaturaCartao - lacuna PR #72, AddLancamentoAntecipada). Commit 7e3e071.

---

## TASK-194 — Controllers/DTOs: ativo com conta, vincular conta, saldo calculado e total (levi)

STATUS: CONCLUIDA
AGENT: levi
DEPENDENCIAS: TASK-193
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/regra-de-negocio.md (itens 8.5 e 10), MyFinances/MyFinances/Controllers/AtivosController.cs, MyFinances/MyFinances/Controllers/ContasController.cs, MyFinances/MyFinances/DTOs/Ativo/CriarAtivoRequest.cs, MyFinances/MyFinances/DTOs/Ativo/AtivoResponse.cs, MyFinances/MyFinances/DTOs/Conta/ContaResponse.cs
ESCOPO: POST /api/ativos com contaId (sem instituicao); GET ativos/contas expondo ContaId, ContaNome, SemConta, Saldo calculado e SaldoCalculado; `PATCH /api/ativos/{id}/conta` (200/404/400); `PATCH /api/contas/{id}/desativar` -> 409 se ContaPossuiAtivoAtivoException; `PATCH /api/contas/{id}/saldo` -> 422 se SaldoCalculadoNaoEditavelException; `GET /api/contas/investimentos/total` retorna o total do item 8.5.
CRITERIO DE ACEITE: 1) cada status acima; 2) ContaResponse.Saldo de conta com ativo vinculado = soma valor_atual; 3) controllers sem logica.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Controllers/AtivosController.cs, MyFinances/MyFinances/Controllers/ContasController.cs, MyFinances/MyFinances/DTOs/Ativo/CriarAtivoRequest.cs, MyFinances/MyFinances/DTOs/Ativo/AtivoResponse.cs, MyFinances/MyFinances/DTOs/Ativo/VincularContaAtivoRequest.cs (novo), MyFinances/MyFinances/DTOs/Conta/ContaResponse.cs
NAO FAZER: nao alterar services; listar (sem alterar) impacto em mcp-server; nao editar .claude/tasks.md.
RETORNO ESPERADO: diff + build limpo + lista de impactos no mcp-server.
HISTORICO: levi: PATCH /api/ativos/{id}/conta, 409 ao desativar conta com ativo, 422 ao editar saldo calculado, ContaResponse com saldo calculado. Commit 7e3e071.

---

## TASK-195 — Mike GREEN + testes HTTP: ativo vinculado a conta

STATUS: CONCLUIDA
AGENT: mike
DEPENDENCIAS: TASK-194
FLUXO: Implementacao (rodada GREEN + HTTP)
CONTEXTO A LER: .claude/context/regra-de-negocio.md (itens 8.5 e 10), MyFinances/MyFinances.Tests/Controllers/AtivosControllerTests.cs, MyFinances/MyFinances.Tests/Controllers/ContasControllerTests.cs, MyFinances/MyFinances/Controllers/AtivosController.cs, MyFinances/MyFinances/Controllers/ContasController.cs
ESCOPO: Rodar testes da TASK-192 (GREEN) e escrever HTTP: criar ativo sem contaId -> 400; com conta valida -> 201 e conta mostra saldo = valor_atual; desativar conta com ativo ativo -> 409; PATCH saldo nessa conta -> 422; PATCH ativos/{id}/conta move e recalcula ambas as contas; total/investimentos nao soma ativo vinculado duas vezes; ativo antigo (conta_id null) aparece SemConta.
CRITERIO DE ACEITE: 1) TASK-192 GREEN; 2) HTTP GREEN; 3) suite verde.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances.Tests/Controllers/AtivoContaVinculoControllerTests.cs (novo)
NAO FAZER: nao alterar producao (reportar bug); nao editar .claude/tasks.md.
RETORNO ESPERADO: saida `dotnet test` + relatorio de bug se houver.
HISTORICO: mike migrou AtivosControllerTests/AtivoServiceTests para contaId e cobriu os endpoints novos por HTTP. Achou bug: PATCH com ContaId vazio -> 500; Kira adicionou catch CampoObrigatorioException -> 400. Overload [Obsolete] por instituicao e CriarAtivoRequest.Instituicao removidos. Suite 1048/1048. Commit 7e3e071.

---

## TASK-196 — Style review: ativo vinculado a conta de investimento (style)

STATUS: CONCLUIDA
AGENT: style
DEPENDENCIAS: TASK-195
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/regra-de-negocio.md (itens 8.5 e 10), .claude/context/clean-code.md, .claude/context/stack.md, MyFinances/MyFinances/Domain/ContaInvestimentoSaldoCalculator.cs, MyFinances/MyFinances/Services/AtivoService.cs, MyFinances/MyFinances/Services/ContaService.cs, MyFinances/MyFinances/Controllers/AtivosController.cs, MyFinances/MyFinances/Controllers/ContasController.cs
ESCOPO: Revisar: calculo unico de saldo (sem duplicar), nenhuma dupla contagem em total/patrimonio, migration aditiva, ativo antigo preservado, DTOs sem vazar entity.
CRITERIO DE ACEITE: veredito APROVADO ou tarefa(s) de correcao.
ARQUIVOS PERMITIDOS: nenhum (somente leitura)
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + achados.
HISTORICO: style 1a rodada: PRECISA CORRIGIR (construtores transitorios com null! em AtivoService/ContaService). Kira removeu inline (mecanico), 1048/1048. Ressalvas: N+1 em ListarContas (ObterSaldo por conta), dois overloads de ContaResponse.FromConta. Commit 7e3e071.

---

## TASK-197 — Front: camada de dados de investimentos e contas (conta do ativo, vincular, saldo calculado) (hanzo)

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: TASK-194
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/stack.md ("Frontend (React)"), MyFinanceFrontEnd/src/features/investimentos/types.ts, MyFinanceFrontEnd/src/features/investimentos/api.ts, MyFinanceFrontEnd/src/features/investimentos/query-keys.ts, MyFinanceFrontEnd/src/features/investimentos/hooks/useCriarAtivo.ts, MyFinanceFrontEnd/src/features/contas/types.ts, MyFinanceFrontEnd/src/features/contas/api.ts, MyFinanceFrontEnd/src/features/contas/hooks/useDesativarConta.ts, MyFinanceFrontEnd/src/features/contas/hooks/useAtualizarSaldoConta.ts, MyFinanceFrontEnd/src/features/contas/lib/calcularPatrimonioTotal.ts
ESCOPO: Tipos (AtivoResponse com contaId/contaNome/semConta; ContaResponse com saldoCalculado; CriarAtivoRequest com contaId e sem instituicao), api `vincularContaAtivo` e `buscarTotalInvestido`, hook `useVincularContaAtivo` (invalida ativos, contas e total), `useTotalInvestido`; hooks de desativar conta e atualizar saldo tratam 409/422 com mensagem tipada; `calcularPatrimonioTotal` passa a somar saldos Banco + totalInvestido do backend (sem somar contas investimento e ativos de novo).
CRITERIO DE ACEITE: 1) sem any; 2) invalidacao cruzada ativos<->contas; 3) typecheck limpo.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/investimentos/types.ts, MyFinanceFrontEnd/src/features/investimentos/api.ts, MyFinanceFrontEnd/src/features/investimentos/query-keys.ts, MyFinanceFrontEnd/src/features/investimentos/hooks/useCriarAtivo.ts, MyFinanceFrontEnd/src/features/investimentos/hooks/useVincularContaAtivo.ts (novo), MyFinanceFrontEnd/src/features/investimentos/hooks/useTotalInvestido.ts (novo), MyFinanceFrontEnd/src/features/contas/types.ts, MyFinanceFrontEnd/src/features/contas/api.ts, MyFinanceFrontEnd/src/features/contas/hooks/useDesativarConta.ts, MyFinanceFrontEnd/src/features/contas/hooks/useAtualizarSaldoConta.ts, MyFinanceFrontEnd/src/features/contas/lib/calcularPatrimonioTotal.ts
NAO FAZER: nao renderizar UI; nao editar .claude/tasks.md.
RETORNO ESPERADO: tipos/hooks prontos + typecheck.
HISTORICO: 2026-10-13 (hanzo). Tipos (AtivoResponse contaId/contaNome/semConta, CriarAtivoRequest com contaId, ContaResponse.saldoCalculado boolean), api vincularContaAtivo/buscarTotalInvestido, hooks useVincularContaAtivo/useTotalInvestido, erros tipados 409/422 (ContaOperacaoError), calcularPatrimonioTotal = Banco + totalInvestido. Fora da lista: ContasPage passa totalInvestido. Style rodada 1 PRECISA CORRIGIR (ALTA: saldoCalculado tipado como number e Atualizar saldo oferecido com saldo calculado; ALTA: total investido nao invalidado em 5 mutations; MEDIA: patrimonio caia em 0 com total carregando/erro) -> rodada 2 APROVADO.

---

## TASK-198 — Esqueleto: antecipacao de parcelas no cartao + marca "antecipada" (item 12)

STATUS: CONCLUIDA
AGENT: killua
DEPENDENCIAS: vazio
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/regra-de-negocio.md (item 12 inteiro, subsecoes Parcelamento, Estorno de compra parcelada e Antecipacao de parcelas; itens 9 e 14), .claude/context/stack.md, .claude/context/clean-code.md, .claude/context/schema.dbml, MyFinances/MyFinances/Services/FaturaCicloService.cs, MyFinances/MyFinances/Services/ComprasParceladasService.cs, MyFinances/MyFinances/Services/EstornoCompraParceladaService.cs, MyFinances/MyFinances/Domain/Lancamento.cs, MyFinances/MyFinances/Domain/Fatura.cs, MyFinances/MyFinances/DTOs/CompraResponse.cs, MyFinances/MyFinances/Controllers/CartaoComprasParceladasController.cs
ESCOPO: Decidir e devolver esqueleto da persistencia da marca: coluna `lancamento.antecipada boolean not null default false` (alternativa `fatura_original_id` descartada por YAGNI, sem desfazer na v1), registrada com tradeoff; `Domain/AntecipacaoParcelasSelector.Selecionar(parcelasDaCompra, faturaAbertaAtual, quantidade)` puro (elegiveis = parcelas em fatura POSTERIOR a aberta atual; maiores parcela_numero primeiro; quantidade > elegiveis -> rejeita inteiro); `IAntecipacaoParcelasService` (`ObterElegiveisAsync(contaId, compraParceladaId)`, `AnteciparAsync(contaId, compraParceladaId, quantidade, dataAntecipacao)`); efeito = mover o proprio Lancamento (fatura_id -> aberta atual, data -> data da antecipacao, antecipada=true; valor/parcela_numero/compra_parcelada_id/categoria intactos; nada criado nem removido); DTOs (AnteciparParcelasRequest, AnteciparParcelasResponse, ParcelaElegivelResponse) e `CompraResponse` + TotalParcelas + Antecipada; rotas `POST /api/contas/{contaId}/compras-parceladas/{compraParceladaId}/antecipacoes` e `GET .../antecipacoes/elegiveis`; spec da migration; schema.dbml via alterar-context; DI.
CRITERIO DE ACEITE: 1) esqueleto compila; 2) contrato cobre: sem pagamento/transferencia, sem desconto, repetivel, conta nao-Cartao e compra sem fatura rejeitadas; 3) lista de casos de teste (exemplo 10x com 3/10 na aberta: antecipar 2 move 10/10 e 9/10).
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Domain/Lancamento.cs, MyFinances/MyFinances/Domain/AntecipacaoParcelasSelector.cs (novo), MyFinances/MyFinances/Services/IAntecipacaoParcelasService.cs (novo), MyFinances/MyFinances/Services/AntecipacaoParcelasService.cs (novo), MyFinances/MyFinances/DTOs/AnteciparParcelasRequest.cs (novo), MyFinances/MyFinances/DTOs/AnteciparParcelasResponse.cs (novo), MyFinances/MyFinances/DTOs/ParcelaElegivelResponse.cs (novo), MyFinances/MyFinances/DTOs/CompraResponse.cs, MyFinances/MyFinances/Controllers/CartaoComprasParceladasController.cs (so assinaturas), MyFinances/MyFinances/Program.cs, .claude/context/schema.dbml
NAO FAZER: nao implementar logica; nao criar migration; nao editar .claude/tasks.md.
RETORNO ESPERADO: texto dos esqueletos + decisao da marca com tradeoff + spec da migration + casos de teste.
HISTORICO: Esqueleto gravado (Lancamento.Antecipada, AntecipacaoParcelasSelector, IAntecipacaoParcelasService, DTOs, CompraResponse.TotalParcelas/Antecipada, rotas no controller, DI). Marca = boolean (confirmado). schema.dbml pendente. Pergunta aberta: GET elegiveis sem data pode divergir do POST se o ciclo virou.

---

## TASK-199 — Testes RED: antecipacao de parcelas (mike)

STATUS: CONCLUIDA
AGENT: mike
DEPENDENCIAS: TASK-198
FLUXO: Implementacao (rodada RED)
CONTEXTO A LER: .claude/context/regra-de-negocio.md (item 12 subsecoes Parcelamento e Antecipacao de parcelas), MyFinances/MyFinances/Domain/AntecipacaoParcelasSelector.cs, MyFinances/MyFinances/Services/AntecipacaoParcelasService.cs, MyFinances/MyFinances.Tests/Services/EstornoCompraParceladaServiceTests.cs, MyFinances/MyFinances.Tests/Services/ComprasParceladasServiceIntegrationTests.cs
ESCOPO: Cobrir: elegiveis (so fatura posterior a aberta atual; parcela na aberta, fechada ou paga nao elegivel); ordem das ultimas para tras (10x, 3/10 na aberta, antecipar 2 -> 10/10 e 9/10; 4/10 a 8/10 ficam); quantidade > elegiveis rejeita inteiro sem mudar nada; quantidade < 1 rejeitada; efeito (fatura_id aberta atual, data = data da antecipacao, Antecipada=true; valor, parcela_numero, compra_parcelada_id, categoria intactos); soma das N parcelas continua = valor_total; nenhum lancamento criado/removido; total da fatura atual sobe e o das futuras cai; nao gera pagamento nem transferencia; repetivel enquanto houver elegivel; compra de banco (sem fatura), conta nao-Cartao e compra inexistente rejeitadas; gasto por categoria no mes da nova data.
CRITERIO DE ACEITE: 1) RED por NotImplementedException; 2) um teste por caso; 3) suite legada verde.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances.Tests/Domain/AntecipacaoParcelasSelectorTests.cs (novo), MyFinances/MyFinances.Tests/Services/AntecipacaoParcelasServiceTests.cs (novo)
NAO FAZER: nao implementar; nao editar testes existentes; nao editar .claude/tasks.md.
RETORNO ESPERADO: lista de testes + saida RED.
HISTORICO: mike RED: 24 testes (selector + servico em SQLite em memoria).

---

## TASK-200 — GREEN: antecipacao de parcelas + coluna antecipada + migration (levi)

STATUS: CONCLUIDA
AGENT: levi
DEPENDENCIAS: TASK-199, TASK-193
FLUXO: Implementacao (rodada GREEN)
CONTEXTO A LER: .claude/context/regra-de-negocio.md (item 12 subsecoes Parcelamento e Antecipacao de parcelas), .claude/context/clean-code.md, .claude/context/stack.md, MyFinances/MyFinances.Tests/Domain/AntecipacaoParcelasSelectorTests.cs, MyFinances/MyFinances.Tests/Services/AntecipacaoParcelasServiceTests.cs, MyFinances/MyFinances/Services/FaturaCicloService.cs, MyFinances/MyFinances/Infrastructure/Configurations/LancamentoConfiguration.cs, MyFinances/MyFinances/Repositories/LancamentoRepository.cs, MyFinances/MyFinances/Repositories/FaturaRepository.cs
ESCOPO: Implementar Selector e AntecipacaoParcelasService (transacao; fatura aberta atual via FaturaCicloService), coluna `antecipada` em LancamentoConfiguration, repositorio (parcelas da compra com Fatura incluida), CompraResponse.FromLancamento com TotalParcelas/Antecipada, e a migration EF aditiva (rodar depois da migration da TASK-193).
CRITERIO DE ACEITE: 1) testes da TASK-199 GREEN; 2) migration aditiva (default false para lancamentos existentes); 3) suite verde.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Domain/AntecipacaoParcelasSelector.cs, MyFinances/MyFinances/Services/AntecipacaoParcelasService.cs, MyFinances/MyFinances/Services/IAntecipacaoParcelasService.cs, MyFinances/MyFinances/Infrastructure/Configurations/LancamentoConfiguration.cs, MyFinances/MyFinances/Repositories/ILancamentoRepository.cs, MyFinances/MyFinances/Repositories/LancamentoRepository.cs, MyFinances/MyFinances/Repositories/IFaturaRepository.cs, MyFinances/MyFinances/Repositories/FaturaRepository.cs, MyFinances/MyFinances/DTOs/CompraResponse.cs, MyFinances/MyFinances/Migrations/ (arquivos novos + MyFinancesDbContextModelSnapshot.cs)
NAO FAZER: nao alterar testes da TASK-199; nao implementar controller; nao editar .claude/tasks.md.
RETORNO ESPERADO: arquivos + `dotnet test` verde + nome da migration.
HISTORICO: levi GREEN 24/24, commit a474748. Mapeamento antecipada pronto; migration AddLancamentoAntecipada pendente (apos AddContaIdToAtivo). Selector roda antes de resolver fatura destino (rejeicao sem efeito colateral). catch amplo no rollback.

---

## TASK-201 — Controller: antecipar parcelas e listar elegiveis (levi)

STATUS: CONCLUIDA
AGENT: levi
DEPENDENCIAS: TASK-200
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/regra-de-negocio.md (item 12 subsecao Antecipacao de parcelas), MyFinances/MyFinances/Controllers/CartaoComprasParceladasController.cs, MyFinances/MyFinances/Services/IAntecipacaoParcelasService.cs
ESCOPO: `GET /api/contas/{contaId}/compras-parceladas/{compraParceladaId}/antecipacoes/elegiveis` (200 lista com numero, valor, faturaOrigem) e `POST .../antecipacoes` com `{ quantidade }` (200 parcelas movidas; 400 `{ erro }` se quantidade > elegiveis ou invalida; 404 se compra nao existe).
CRITERIO DE ACEITE: 1) contratos acima; 2) controller sem logica; 3) FaturasController lista a compra com antecipada=true e totalParcelas.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Controllers/CartaoComprasParceladasController.cs
NAO FAZER: nao alterar service; nao editar .claude/tasks.md.
RETORNO ESPERADO: diff + build limpo.
HISTORICO: levi: GET elegiveis e POST antecipacoes (404 por mensagem, demais 400; dataAntecipacao opcional = hoje).

---

## TASK-202 — Mike GREEN + testes HTTP: antecipacao de parcelas

STATUS: CONCLUIDA
AGENT: mike
DEPENDENCIAS: TASK-201
FLUXO: Implementacao (rodada GREEN + HTTP)
CONTEXTO A LER: .claude/context/regra-de-negocio.md (item 12 subsecao Antecipacao de parcelas), MyFinances/MyFinances.Tests/Controllers/CartaoComprasParceladasControllerTests.cs, MyFinances/MyFinances.Tests/Controllers/FaturasControllerTests.cs, MyFinances/MyFinances/Controllers/CartaoComprasParceladasController.cs
ESCOPO: Rodar testes da TASK-199 (GREEN) e escrever HTTP: GET elegiveis; POST move parcelas e GET faturas mostra total da atual maior, total das futuras menor, parcelas com antecipada=true; quantidade acima do disponivel -> 400 sem alterar nada; repetir antecipacao; compra inexistente -> 404.
CRITERIO DE ACEITE: 1) TASK-199 GREEN; 2) HTTP GREEN; 3) suite verde.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances.Tests/Controllers/CartaoAntecipacaoControllerTests.cs (novo)
NAO FAZER: nao alterar producao (reportar bug); nao editar .claude/tasks.md.
RETORNO ESPERADO: saida `dotnet test` + relatorio de bug se houver.
HISTORICO: mike: 24 GREEN + 9 HTTP GREEN. NRE suspeito nao confirmado (EF fixup). Lacuna: nenhum endpoint lista parcelas da fatura com antecipada=true.

---

## TASK-203 — Style review: antecipacao de parcelas (style)

STATUS: CONCLUIDA
AGENT: style
DEPENDENCIAS: TASK-202
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/regra-de-negocio.md (item 12), .claude/context/clean-code.md, .claude/context/stack.md, MyFinances/MyFinances/Domain/AntecipacaoParcelasSelector.cs, MyFinances/MyFinances/Services/AntecipacaoParcelasService.cs, MyFinances/MyFinances/Controllers/CartaoComprasParceladasController.cs
ESCOPO: Revisar: antecipar nao paga nem cria transferencia, valor/parcela intactos, sem desconto, selecao das ultimas para tras, migration aditiva, estorno posterior coerente com a regra existente.
CRITERIO DE ACEITE: veredito APROVADO ou tarefa(s) de correcao.
ARQUIVOS PERMITIDOS: nenhum (somente leitura)
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + achados.
HISTORICO: style aprovou back e controller da antecipacao na 2a rodada. Commit 525d9a5.

---

## TASK-204 — Front: camada de dados de antecipacao de parcelas (hanzo)

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: TASK-201
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/stack.md ("Frontend (React)"), MyFinanceFrontEnd/src/features/cartao/types.ts, MyFinanceFrontEnd/src/features/cartao/api.ts, MyFinanceFrontEnd/src/features/cartao/query-keys.ts, MyFinanceFrontEnd/src/features/cartao/hooks/useFaturas.ts, MyFinanceFrontEnd/src/features/cartao/hooks/usePagarFatura.ts
ESCOPO: Tipos (compra com totalParcelas e antecipada; ParcelaElegivel; AnteciparRequest), api `listarParcelasElegiveis` e `anteciparParcelas`, hooks `useParcelasElegiveis(contaId, compraParceladaId)` e `useAnteciparParcelas` (invalida faturas, saldo do cartao e projecao do dashboard); helper puro em lib/ para o texto de pre-visualizacao (parcelas que vao para a fatura atual) usando so dados do backend.
CRITERIO DE ACEITE: 1) sem any; 2) invalidacao correta; 3) typecheck limpo.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/cartao/types.ts, MyFinanceFrontEnd/src/features/cartao/api.ts, MyFinanceFrontEnd/src/features/cartao/query-keys.ts, MyFinanceFrontEnd/src/features/cartao/hooks/useParcelasElegiveis.ts (novo), MyFinanceFrontEnd/src/features/cartao/hooks/useAnteciparParcelas.ts (novo), MyFinanceFrontEnd/src/features/cartao/lib/montarPreviaAntecipacao.ts (novo)
NAO FAZER: nao renderizar UI; nao recalcular fatura no front; nao editar .claude/tasks.md.
RETORNO ESPERADO: tipos/hooks prontos + typecheck.
HISTORICO: hanzo: tipos/api/hooks de antecipacao; previa corrigida para ultimas para tras. Commit 3bf2a23.

---

## TASK-205 — Esqueleto: edicao de conta a receber (item 13)

STATUS: CONCLUIDA
AGENT: killua
DEPENDENCIAS: vazio
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/regra-de-negocio.md (itens 9, 13 e 15), .claude/context/stack.md, .claude/context/clean-code.md, MyFinances/MyFinances/Services/ContaReceberService.cs, MyFinances/MyFinances/Services/IContaReceberService.cs, MyFinances/MyFinances/Domain/ContaReceber.cs, MyFinances/MyFinances/Domain/ContaReceberSaldoCalculator.cs, MyFinances/MyFinances/Controllers/ContasReceberController.cs, MyFinances/MyFinances/DTOs/ContaReceber/ContaReceberResponse.cs, MyFinances/MyFinances/Exceptions/PessoaObrigatoriaParaEmprestimoException.cs
ESCOPO: Devolver esqueleto de `IContaReceberService.EditarAsync(Guid id, string descricao, string? pessoa, DateOnly? dataPrevista, decimal? valorTotal) -> Task<ContaReceber>`, `EditarContaReceberRequest` (DTO), excecoes (`ValorTotalNaoEditavelException`, `ContaReceberRecorrenteNaoEditavelException`), rota `PUT /api/contas-receber/{id}` e mapeamento de erros (404/400/409); regras: editaveis em qualquer status descricao, pessoa (so EMPRESTIMO, obrigatoria nele; ignorada em RECEBIVEL) e data_prevista; valor_total so em RECEBIVEL Pendente (EMPRESTIMO nunca; valor igual ao atual e no-op); tipo nunca muda; ocorrencia de recebivel recorrente (recebivel_recorrente_id) e recusada ("edite o molde").
CRITERIO DE ACEITE: 1) esqueleto compila; 2) contrato cobre tipo imutavel, saldo_pendente continua = valor_total apos editar valor em Pendente, e efeito de data_prevista na projecao do item 9; 3) lista de casos de teste.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Services/IContaReceberService.cs, MyFinances/MyFinances/Services/ContaReceberService.cs, MyFinances/MyFinances/DTOs/ContaReceber/EditarContaReceberRequest.cs (novo), MyFinances/MyFinances/Exceptions/ValorTotalNaoEditavelException.cs (novo), MyFinances/MyFinances/Exceptions/ContaReceberRecorrenteNaoEditavelException.cs (novo)
NAO FAZER: nao implementar logica; nao tocar controller; nao editar .claude/tasks.md.
RETORNO ESPERADO: texto dos esqueletos + casos de teste.
HISTORICO: Esqueleto gravado (EditarAsync, EditarContaReceberRequest, ValorTotalNaoEditavelException, ContaReceberRecorrenteNaoEditavelException). Build ok.

---

## TASK-206 — Testes RED: edicao de conta a receber (mike)

STATUS: CONCLUIDA
AGENT: mike
DEPENDENCIAS: TASK-205
FLUXO: Implementacao (rodada RED)
CONTEXTO A LER: .claude/context/regra-de-negocio.md (itens 9, 13 e 15), MyFinances/MyFinances/Services/ContaReceberService.cs, MyFinances/MyFinances.Tests/Services/ContaReceberServiceTests.cs, MyFinances/MyFinances.Tests/Services/ContaReceberIntegrationTests.cs
ESCOPO: Cobrir: descricao/pessoa/data_prevista editaveis em PENDENTE, PARCIAL e RECEBIDO; valor_total editavel so em RECEBIVEL PENDENTE (saldo_pendente = novo valor); em PARCIAL/RECEBIDO com valor diferente -> ValorTotalNaoEditavelException; valor igual -> ok; EMPRESTIMO com valor diferente -> excecao; valor <= 0 rejeitado; descricao vazia rejeitada; EMPRESTIMO sem pessoa -> PessoaObrigatoriaParaEmprestimoException; pessoa em RECEBIVEL ignorada; tipo inalterado; ocorrencia recorrente -> ContaReceberRecorrenteNaoEditavelException; inexistente -> ContaReceberNaoEncontradaException; mudar data_prevista para outro mes move o saldo na projecao do mes.
CRITERIO DE ACEITE: 1) RED por NotImplementedException; 2) um teste por caso; 3) suite legada verde.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances.Tests/Services/ContaReceberEditarServiceTests.cs (novo)
NAO FAZER: nao implementar; nao editar testes existentes; nao editar .claude/tasks.md.
RETORNO ESPERADO: lista de testes + saida RED.
HISTORICO: mike RED: 28 testes (27 unit + 1 integracao projecao).

---

## TASK-207 — GREEN: ContaReceberService.EditarAsync (levi)

STATUS: CONCLUIDA
AGENT: levi
DEPENDENCIAS: TASK-206
FLUXO: Implementacao (rodada GREEN)
CONTEXTO A LER: .claude/context/regra-de-negocio.md (itens 13 e 15), .claude/context/clean-code.md, MyFinances/MyFinances.Tests/Services/ContaReceberEditarServiceTests.cs, MyFinances/MyFinances/Services/ContaReceberService.cs, MyFinances/MyFinances/Domain/ContaReceberSaldoCalculator.cs, MyFinances/MyFinances/Repositories/ContaReceberRepository.cs
ESCOPO: Implementar EditarAsync conforme o contrato da TASK-205, reutilizando ContaReceberSaldoCalculator para decidir o estado.
CRITERIO DE ACEITE: 1) testes da TASK-206 GREEN; 2) suite verde; 3) sem logica duplicada de estado.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Services/ContaReceberService.cs, MyFinances/MyFinances/Services/IContaReceberService.cs
NAO FAZER: nao alterar testes da TASK-206; nao tocar controller; nao editar .claude/tasks.md.
RETORNO ESPERADO: arquivos + `dotnet test` verde.
HISTORICO: levi GREEN 28/28, commit 16d8285. valor<=0 e descricao vazia recusados (suposicao do Kira).

---

## TASK-208 — Controller: PUT /api/contas-receber/{id} (levi)

STATUS: CONCLUIDA
AGENT: levi
DEPENDENCIAS: TASK-207
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/regra-de-negocio.md (item 13), MyFinances/MyFinances/Controllers/ContasReceberController.cs, MyFinances/MyFinances/DTOs/ContaReceber/EditarContaReceberRequest.cs, MyFinances/MyFinances/DTOs/ContaReceber/ContaReceberResponse.cs
ESCOPO: Implementar `[HttpPut("api/contas-receber/{id}")]`: 200 com ContaReceberResponse; 404 inexistente; 409 `{ erro }` para valor_total nao editavel ou ocorrencia recorrente; 400 para validacao.
CRITERIO DE ACEITE: 1) cada status acima; 2) controller sem logica.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Controllers/ContasReceberController.cs
NAO FAZER: nao alterar service; nao editar .claude/tasks.md.
RETORNO ESPERADO: diff + build limpo.
HISTORICO: levi: PUT /api/contas-receber/{id} (200/404/409/400). Commit 89f41ef.

---

## TASK-209 — Mike GREEN + testes HTTP: edicao de conta a receber

STATUS: CONCLUIDA
AGENT: mike
DEPENDENCIAS: TASK-208
FLUXO: Implementacao (rodada GREEN + HTTP)
CONTEXTO A LER: .claude/context/regra-de-negocio.md (item 13), MyFinances/MyFinances.Tests/Controllers/ContaReceberControllerTests.cs, MyFinances/MyFinances/Controllers/ContasReceberController.cs
ESCOPO: Rodar testes da TASK-206 (GREEN) e escrever HTTP: PUT em recebivel pendente altera valor; apos um recebimento, valor diferente -> 409; emprestimo valor diferente -> 409; descricao/pessoa/data editaveis em qualquer status; ocorrencia recorrente -> 409; 404.
CRITERIO DE ACEITE: 1) TASK-206 GREEN; 2) HTTP GREEN; 3) suite verde.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances.Tests/Controllers/ContaReceberEditarControllerTests.cs (novo)
NAO FAZER: nao alterar producao (reportar bug); nao editar .claude/tasks.md.
RETORNO ESPERADO: saida `dotnet test` + relatorio de bug se houver.
HISTORICO: mike: 28 unit GREEN + 8 HTTP GREEN. Commit 89f41ef.

---

## TASK-210 — Style review: edicao de conta a receber (style)

STATUS: CONCLUIDA
AGENT: style
DEPENDENCIAS: TASK-209
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/regra-de-negocio.md (itens 13 e 15), .claude/context/clean-code.md, .claude/context/stack.md, MyFinances/MyFinances/Services/ContaReceberService.cs, MyFinances/MyFinances/Controllers/ContasReceberController.cs
ESCOPO: Revisar: valor fixo apos primeiro recebimento, tipo imutavel, emprestimo intocavel, ocorrencia recorrente recusada, controller magro.
CRITERIO DE ACEITE: veredito APROVADO ou tarefa(s) de correcao.
ARQUIVOS PERMITIDOS: nenhum (somente leitura)
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + achados.
HISTORICO: style: APROVADO, sem correcao.

---

## TASK-211 — Front: camada de dados de edicao de conta a receber (hanzo)

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: TASK-208
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/stack.md ("Frontend (React)"), MyFinanceFrontEnd/src/features/contas-receber/types.ts, MyFinanceFrontEnd/src/features/contas-receber/query-keys.ts, MyFinanceFrontEnd/src/features/contas-receber/hooks/useExcluirContaReceber.ts, MyFinanceFrontEnd/src/features/contas-receber/lib/validarContaReceber.ts
ESCOPO: Criar `contas-receber/api.ts` se nao existir (conferir onde as chamadas vivem hoje e manter o padrao) com `editarContaReceber(id, req)`; tipo `EditarContaReceberRequest`; hook `useEditarContaReceber` (invalida lista, item e total-esperado-mes); helper puro `podeEditarValorTotal(conta)` (RECEBIVEL com status PENDENTE) so para habilitar o campo (backend continua validando).
CRITERIO DE ACEITE: 1) sem any; 2) 409 tratado como erro tipado; 3) typecheck limpo.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/contas-receber/api.ts, MyFinanceFrontEnd/src/features/contas-receber/types.ts, MyFinanceFrontEnd/src/features/contas-receber/hooks/useEditarContaReceber.ts (novo), MyFinanceFrontEnd/src/features/contas-receber/lib/podeEditarValorTotal.ts (novo)
NAO FAZER: nao renderizar UI; nao editar .claude/tasks.md.
RETORNO ESPERADO: hook pronto + typecheck.
HISTORICO: hanzo: editarContaReceber, useEditarContaReceber, podeEditarValorTotal. Commit 89f41ef.

---

## TASK-212 — Esqueleto: relatorio por categoria (item 19)

STATUS: CONCLUIDA
AGENT: killua
DEPENDENCIAS: vazio
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/regra-de-negocio.md (itens 3, 7, 12, 14 e 19), .claude/context/stack.md, .claude/context/clean-code.md, MyFinances/MyFinances/Domain/LimiteGastoCalculator.cs, MyFinances/MyFinances/Domain/ClassificacaoLancamentoService.cs, MyFinances/MyFinances/Services/LimiteGastoService.cs, MyFinances/MyFinances/Repositories/ILancamentoRepository.cs, MyFinances/MyFinances/Repositories/LancamentoRepository.cs, MyFinances/MyFinances/Domain/Categoria.cs, MyFinances/MyFinances/Controllers/LimitesGastoController.cs
ESCOPO: Devolver esqueleto de `Domain/GastoPorCategoriaCalculator` (puro: lancamentos do mes + categorias -> linhas de primeiro nivel somando subcategorias diretas, mais linha "Sem categoria"; so Tipo=Debit, Oculto=false, TransferenciaId=null; PENDENTE e PAGO; gasto>0; ordem desc; percentual 0-100 com 2 casas calculado no backend), `VariacaoMensalCalculator` (nulo se mes anterior zero), `IRelatorioCategoriaService/RelatorioCategoriaService` (`ObterResumoAsync(ano, mes)`, `ObterDetalheAsync(ano, mes, Guid? categoriaId)`), DTOs (RelatorioGastoPorCategoriaResponse{ano, mes, totalGasto, totalMesAnterior, variacaoPercentual?, linhas[]}; RelatorioCategoriaDetalheResponse{categoriaId?, nome, icone, total, percentualDoTotal, limite?{valorLimite, gastoRealizado}, lancamentos[]{id, descricao, data, contaId, contaNome, contaTipo, subcategoriaNome?, valor} em data crescente}), rotas `GET /api/relatorios/gasto-por-categoria?ano&mes` e `GET /api/relatorios/gasto-por-categoria/detalhe?ano&mes&categoriaId` (sem categoriaId = Sem categoria; subcategoria como id -> 400), metodo de repositorio `ListarDebitosParaRelatorioAsync(ano, mes)` e DI.
CRITERIO DE ACEITE: 1) esqueleto compila; 2) contrato prova que soma do detalhe = valor da linha e que transferencia/pagamento de fatura nunca entram (nota: LimiteGastoCalculator nao filtra transferencia); 3) lista de casos de teste.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Domain/GastoPorCategoriaCalculator.cs (novo), MyFinances/MyFinances/Domain/VariacaoMensalCalculator.cs (novo), MyFinances/MyFinances/Services/IRelatorioCategoriaService.cs (novo), MyFinances/MyFinances/Services/RelatorioCategoriaService.cs (novo), MyFinances/MyFinances/DTOs/Relatorio/RelatorioGastoPorCategoriaResponse.cs (novo), MyFinances/MyFinances/DTOs/Relatorio/RelatorioCategoriaDetalheResponse.cs (novo), MyFinances/MyFinances/Repositories/ILancamentoRepository.cs, MyFinances/MyFinances/Repositories/LancamentoRepository.cs, MyFinances/MyFinances/Controllers/RelatoriosController.cs (novo, so assinaturas), MyFinances/MyFinances/Program.cs
NAO FAZER: nao implementar logica; nao alterar LimiteGastoCalculator; nao editar .claude/tasks.md.
RETORNO ESPERADO: texto dos esqueletos + casos de teste.
HISTORICO: Esqueleto gravado (GastoPorCategoriaCalculator, VariacaoMensalCalculator, RelatorioCategoriaService, DTOs, RelatoriosController, ListarDebitosParaRelatorioAsync, DI). Arquivo extra autorizado pelo Kira: CategoriaNaoEhPrimeiroNivelException. So DEBIT (confirmado).

---

## TASK-213 — Testes RED: relatorio por categoria (mike)

STATUS: CONCLUIDA
AGENT: mike
DEPENDENCIAS: TASK-212
FLUXO: Implementacao (rodada RED)
CONTEXTO A LER: .claude/context/regra-de-negocio.md (itens 3, 12, 14 e 19), MyFinances/MyFinances/Domain/GastoPorCategoriaCalculator.cs, MyFinances/MyFinances/Services/RelatorioCategoriaService.cs, MyFinances/MyFinances.Tests/Domain/LimiteGastoCalculatorTests.cs, MyFinances/MyFinances.Tests/Services/LimiteGastoServiceTests.cs
ESCOPO: Cobrir: so DEBIT (CREDIT/estorno fora); oculto fora; transferencia, pagamento de fatura e emprestimo (perna unica) fora; PENDENTE e PAGO entram; compra de cartao entra pela propria data; parcela antecipada conta no mes da nova data; subcategorias somam na categoria de primeiro nivel; categoria sem gasto nao aparece; DEBIT sem categoria vira "Sem categoria"; ordem maior gasto primeiro; percentuais; limite mes (bordas do mes); variacao (mes anterior zero -> nula; (atual-anterior)/anterior); detalhe: lancamentos em data crescente, soma = valor da linha, conta de cartao identificada pelo contaTipo, subcategoria preenchida, barra limite x realizado quando ha limite, categoria passada que e subcategoria -> erro.
CRITERIO DE ACEITE: 1) RED por NotImplementedException; 2) um teste por caso; 3) suite legada verde.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances.Tests/Domain/GastoPorCategoriaCalculatorTests.cs (novo), MyFinances/MyFinances.Tests/Domain/VariacaoMensalCalculatorTests.cs (novo), MyFinances/MyFinances.Tests/Services/RelatorioCategoriaServiceTests.cs (novo)
NAO FAZER: nao implementar; nao editar testes existentes; nao editar .claude/tasks.md.
RETORNO ESPERADO: lista de testes + saida RED.
HISTORICO: mike RED: 57 testes.

---

## TASK-214 — GREEN: calculos e RelatorioCategoriaService (levi)

STATUS: CONCLUIDA
AGENT: levi
DEPENDENCIAS: TASK-213
FLUXO: Implementacao (rodada GREEN)
CONTEXTO A LER: .claude/context/regra-de-negocio.md (itens 14 e 19), .claude/context/clean-code.md, MyFinances/MyFinances.Tests/Domain/GastoPorCategoriaCalculatorTests.cs, MyFinances/MyFinances.Tests/Domain/VariacaoMensalCalculatorTests.cs, MyFinances/MyFinances.Tests/Services/RelatorioCategoriaServiceTests.cs, MyFinances/MyFinances/Repositories/LancamentoRepository.cs, MyFinances/MyFinances/Repositories/ICategoriaRepository.cs, MyFinances/MyFinances/Repositories/ILimiteGastoRepository.cs
ESCOPO: Implementar calculadores, ListarDebitosParaRelatorioAsync (Include Conta, Categoria; filtros Debit, !Oculto, TransferenciaId null, mes) e o service.
CRITERIO DE ACEITE: 1) testes da TASK-213 GREEN; 2) suite verde; 3) service sem DbContext direto.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Domain/GastoPorCategoriaCalculator.cs, MyFinances/MyFinances/Domain/VariacaoMensalCalculator.cs, MyFinances/MyFinances/Services/RelatorioCategoriaService.cs, MyFinances/MyFinances/Services/IRelatorioCategoriaService.cs, MyFinances/MyFinances/Repositories/ILancamentoRepository.cs, MyFinances/MyFinances/Repositories/LancamentoRepository.cs
NAO FAZER: nao alterar testes da TASK-213; nao implementar controller; nao editar .claude/tasks.md.
RETORNO ESPERADO: arquivos + `dotnet test` verde.
HISTORICO: levi GREEN 57/57, commit b0ab0fd. PercentualDoTotal sobre total do mes (pergunta aberta). Mensagem de ValorInvalidoException para mes 13 fica enganosa.

---

## TASK-215 — Controller: RelatoriosController (resumo e detalhe) (levi)

STATUS: CONCLUIDA
AGENT: levi
DEPENDENCIAS: TASK-214
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/regra-de-negocio.md (item 19), MyFinances/MyFinances/Controllers/RelatoriosController.cs, MyFinances/MyFinances/Controllers/LimitesGastoController.cs, MyFinances/MyFinances/Services/IRelatorioCategoriaService.cs
ESCOPO: Implementar `GET /api/relatorios/gasto-por-categoria?ano&mes` (default mes corrente quando omitidos) e `GET /api/relatorios/gasto-por-categoria/detalhe?ano&mes&categoriaId`: 200; 400 mes invalido ou categoria que e subcategoria; 404 categoria inexistente.
CRITERIO DE ACEITE: 1) contratos acima; 2) controller sem logica; 3) DI registrada.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Controllers/RelatoriosController.cs, MyFinances/MyFinances/Program.cs
NAO FAZER: nao alterar service; nao editar .claude/tasks.md.
RETORNO ESPERADO: diff + build limpo.
HISTORICO: levi: GET /api/relatorios/gasto-por-categoria e /detalhe (ano/mes default hoje). Commit a3aa0b6.

---

## TASK-216 — Mike GREEN + testes HTTP: relatorio por categoria

STATUS: CONCLUIDA
AGENT: mike
DEPENDENCIAS: TASK-215
FLUXO: Implementacao (rodada GREEN + HTTP)
CONTEXTO A LER: .claude/context/regra-de-negocio.md (item 19), MyFinances/MyFinances.Tests/Controllers/LimitesGastoControllerTests.cs, MyFinances/MyFinances/Controllers/RelatoriosController.cs
ESCOPO: Rodar testes da TASK-213 (GREEN) e escrever HTTP: massa com banco+cartao+transferencia+oculto; resumo com linhas, total e variacao; detalhe com soma = linha; Sem categoria; mes sem dados -> total 0 e linhas vazias; categoria inexistente -> 404; subcategoria -> 400.
CRITERIO DE ACEITE: 1) TASK-213 GREEN; 2) HTTP GREEN; 3) suite verde.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances.Tests/Controllers/RelatoriosControllerTests.cs (novo)
NAO FAZER: nao alterar producao (reportar bug); nao editar .claude/tasks.md.
RETORNO ESPERADO: saida `dotnet test` + relatorio de bug se houver.
HISTORICO: mike: 57 GREEN + 15 HTTP GREEN. Commit a3aa0b6.

---

## TASK-217 — Style review: relatorio por categoria (style)

STATUS: CONCLUIDA
AGENT: style
DEPENDENCIAS: TASK-216
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/regra-de-negocio.md (itens 3, 12, 14 e 19), .claude/context/clean-code.md, .claude/context/stack.md, MyFinances/MyFinances/Domain/GastoPorCategoriaCalculator.cs, MyFinances/MyFinances/Services/RelatorioCategoriaService.cs, MyFinances/MyFinances/Controllers/RelatoriosController.cs
ESCOPO: Revisar: competencia, exclusao de transferencia/oculto, agregacao por subcategoria igual a do item 14 (sem divergir), percentuais e arredondamento, detalhe = linha, sem logica no controller.
CRITERIO DE ACEITE: veredito APROVADO ou tarefa(s) de correcao.
ARQUIVOS PERMITIDOS: nenhum (somente leitura)
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + achados.
HISTORICO: style: APROVADO com ressalvas menores (mensagem mes 13, StatusElegiveis mutavel, Include Categoria desnecessario) — despachadas ao levi.

---

## TASK-218 — Front: camada de dados de relatorio por categoria (hanzo)

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: TASK-215
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/stack.md ("Frontend (React)"), MyFinanceFrontEnd/src/features/limite-gasto/api.ts, MyFinanceFrontEnd/src/features/limite-gasto/types.ts, MyFinanceFrontEnd/src/features/limite-gasto/query-keys.ts, MyFinanceFrontEnd/src/features/limite-gasto/hooks/useGastoVsLimiteTodasCategorias.ts, MyFinanceFrontEnd/src/shared/api/client.ts
ESCOPO: Criar feature `relatorios` (types.ts, api.ts, query-keys.ts) e hooks `useRelatorioGastoPorCategoria(ano, mes)` e `useRelatorioCategoriaDetalhe(ano, mes, categoriaId?)`; lib pura para navegacao de mes (reaproveitar o padrao de NavegadorMes sem importar de outra feature: se necessario mover para shared/lib).
CRITERIO DE ACEITE: 1) sem any; 2) front nao recalcula totais nem percentuais; 3) typecheck limpo.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/relatorios/types.ts (novo), MyFinanceFrontEnd/src/features/relatorios/api.ts (novo), MyFinanceFrontEnd/src/features/relatorios/query-keys.ts (novo), MyFinanceFrontEnd/src/features/relatorios/hooks/useRelatorioGastoPorCategoria.ts (novo), MyFinanceFrontEnd/src/features/relatorios/hooks/useRelatorioCategoriaDetalhe.ts (novo), MyFinanceFrontEnd/src/features/relatorios/lib/navegarMes.ts (novo)
NAO FAZER: nao renderizar UI; nao editar .claude/tasks.md.
RETORNO ESPERADO: tipos/hooks prontos + typecheck.
HISTORICO: hanzo: feature relatorios (types/api/query-keys/hooks/navegarMes). Commit 3bf2a23.

---

## TASK-219 — Front: camada de dados de de-para de categoria (hanzo)

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: vazio
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/stack.md ("Frontend (React)"), .claude/context/regra-de-negocio.md (item 7 De-para), MyFinances/MyFinances/Controllers/DeParaCategoriasController.cs, MyFinances/MyFinances/DTOs/DeParaCategoria/DeParaCategoriaResponse.cs, MyFinances/MyFinances/DTOs/DeParaCategoria/CriarDeParaCategoriaRequest.cs, MyFinances/MyFinances/DTOs/DeParaCategoria/EditarDeParaCategoriaRequest.cs, MyFinanceFrontEnd/src/features/categorias/api.ts, MyFinanceFrontEnd/src/features/categorias/query-keys.ts
ESCOPO: Criar feature `de-para` (types.ts, api.ts, query-keys.ts, hooks useDeParaCategorias, useCriarDePara, useEditarDePara, useRemoverDePara) para GET/POST/PUT/DELETE `/api/deparacategorias` (conferir a rota real no controller).
CRITERIO DE ACEITE: 1) sem any; 2) invalidacao da lista nas mutations; 3) typecheck limpo.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/de-para/types.ts (novo), MyFinanceFrontEnd/src/features/de-para/api.ts (novo), MyFinanceFrontEnd/src/features/de-para/query-keys.ts (novo), MyFinanceFrontEnd/src/features/de-para/hooks/useDeParaCategorias.ts (novo), MyFinanceFrontEnd/src/features/de-para/hooks/useCriarDePara.ts (novo), MyFinanceFrontEnd/src/features/de-para/hooks/useEditarDePara.ts (novo), MyFinanceFrontEnd/src/features/de-para/hooks/useRemoverDePara.ts (novo)
NAO FAZER: nao renderizar UI; nao inventar endpoint de "pendentes" ou "sugerido" (depende de import/Open Finance, v2); nao editar .claude/tasks.md.
RETORNO ESPERADO: hooks prontos + typecheck.
HISTORICO: hanzo: feature de-para (types, api, query-keys, 4 hooks). Rota real /api/deparacategorias. Typecheck ok. Achado: Location do Created no controller aponta /api/de-para-categorias (nao bate com a rota).

---

## TASK-220 — Primitivos shared/ui (1/2): Badge, Chip, IconTile, ProgressBar, PageHeader e tokens (hanzo)

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: vazio
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/identidade-visual.md, .claude/context/stack.md ("shared/ui"), .claude/context/clean-code.md, MyFinanceFrontEnd/src/index.css, MyFinanceFrontEnd/src/shared/ui/card.tsx, MyFinanceFrontEnd/src/shared/ui/button.tsx, MyFinanceFrontEnd/src/shared/lib/utils.ts, mockup desktop `.claude/context/mockups/Prototipo Desktop.dc.html` (badges Pago/Pendente/Manual/Sugerido/antecipada, chips Todos/Entradas/Saidas, icone-tile 34px, barras de limite)
ESCOPO: Criar em shared/ui: `badge.tsx` (variantes pago, pendente, atrasado, manual, sugerido, antecipada, parcela; raio 5px), `chip.tsx` (pilula selecionavel), `icon-tile.tsx` (quadrado arredondado 34px accent-deep + icone), `progress-bar.tsx` (variantes normal/estourado), `page-header.tsx` (titulo 19px + slot de acao); conferir tokens de index.css contra identidade-visual.md e ajustar so se divergirem.
CRITERIO DE ACEITE: 1) componentes sem fetch nem regra de dominio; 2) cores via tokens (nao hex solto), funcionam em tema claro e escuro; 3) typecheck/build limpos.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/shared/ui/badge.tsx (novo), MyFinanceFrontEnd/src/shared/ui/chip.tsx (novo), MyFinanceFrontEnd/src/shared/ui/icon-tile.tsx (novo), MyFinanceFrontEnd/src/shared/ui/progress-bar.tsx (novo), MyFinanceFrontEnd/src/shared/ui/page-header.tsx (novo), MyFinanceFrontEnd/src/index.css
NAO FAZER: nao migrar telas existentes; nao importar tipos de feature; nao editar .claude/tasks.md.
RETORNO ESPERADO: lista de componentes e props + build limpo.
HISTORICO: hanzo: badge, chip, icon-tile, progress-bar, page-header. Tokens de index.css ja batiam com identidade-visual. Build ok.

---

## TASK-221 — Primitivos shared/ui (2/2): Switch, SegmentedTabs, SelectField, Stepper, ConfirmDialog, BottomSheet (hanzo)

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: TASK-220
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/identidade-visual.md, .claude/context/stack.md ("shared/ui"), .claude/context/clean-code.md, MyFinanceFrontEnd/src/shared/ui/Modal.tsx, MyFinanceFrontEnd/src/shared/ui/input.tsx, mockup desktop `.claude/context/mockups/Prototipo Desktop.dc.html` (toggle Parcelar, tabs Despesas/Receitas, dropdown de tipo de conta, stepper de quantidade, dialogo "Cancelar Notebook Dell?") e mobile `.claude/context/mockups/Prototipo Mobile.dc.html` (menu de acoes em folha: `lmenu`, `sheet:*`)
ESCOPO: Criar em shared/ui: `switch.tsx`, `segmented-tabs.tsx`, `select-field.tsx` (campo + lista), `stepper.tsx` (- N +, min/max), `confirm-dialog.tsx` (icone, titulo, corpo, Voltar/Confirmar, variante destrutiva; base no Modal), `bottom-sheet.tsx` (folha inferior mobile, no desktop vira Modal centralizado).
CRITERIO DE ACEITE: 1) acessiveis (teclado, aria, fecha com Esc/clique fora); 2) sem fetch/regra de dominio; 3) typecheck/build limpos.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/shared/ui/switch.tsx (novo), MyFinanceFrontEnd/src/shared/ui/segmented-tabs.tsx (novo), MyFinanceFrontEnd/src/shared/ui/select-field.tsx (novo), MyFinanceFrontEnd/src/shared/ui/stepper.tsx (novo), MyFinanceFrontEnd/src/shared/ui/confirm-dialog.tsx (novo), MyFinanceFrontEnd/src/shared/ui/bottom-sheet.tsx (novo)
NAO FAZER: nao alterar Modal.tsx; nao migrar telas; nao editar .claude/tasks.md.
RETORNO ESPERADO: lista de componentes e props + build limpo.
HISTORICO: hanzo: switch, segmented-tabs, select-field, stepper, confirm-dialog (base BottomSheet center), bottom-sheet. Build ok.

---

## TASK-222 — Shell desktop: sidebar agrupada e area de conteudo (mockup s_dashboard, sidebar de todas as telas) (hanzo)

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: TASK-220
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/identidade-visual.md, .claude/context/stack.md, .claude/context/clean-code.md, mockup desktop `.claude/context/mockups/Prototipo Desktop.dc.html` (sidebar 232px, linhas 57-59: Inicio/Lancamentos/Cartao/Contas; grupo Organizacao: Categorias, De-para de categoria, Contas fixas; grupo Acompanhamento: Conciliacao, Contas a receber, Investimentos, Relatorio por categoria; rodape Sair), MyFinanceFrontEnd/src/app/AppShell.tsx, MyFinanceFrontEnd/src/app/AuthenticatedLayout.tsx, MyFinanceFrontEnd/src/app/components/ThemeToggle.tsx
ESCOPO: Extrair a navegacao para `app/navigation.ts` (grupos NAV_GROUPS, destinos primarios mobile, mantendo Limites de gasto em Organizacao e Recebiveis recorrentes em Acompanhamento para nao orfanar rotas) e reescrever a sidebar de AppShell no padrao do mockup (largura 232px, rotulos de grupo, item ativo accent-deep, marca "Financeiro", rodape Sair + ThemeToggle); liberar a largura do conteudo (hoje max-w-2xl) para as telas de grade do mockup.
CRITERIO DE ACEITE: 1) sidebar identica em estrutura ao mockup (grupos, ordem, estados ativo/hover); 2) itens para rotas ainda inexistentes (/depara, /conciliacao, /relatorio) existem no nav e levam ao redirect padrao ate as telas chegarem; 3) mobile (<md) nao regride (BottomTabBar continua funcionando) e build limpo.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/app/AppShell.tsx, MyFinanceFrontEnd/src/app/navigation.ts (novo)
NAO FAZER: nao mexer em BottomTabBar nem em routes.tsx; nao criar telas; nao incluir busca nem avatar (sem regra); nao editar .claude/tasks.md.
RETORNO ESPERADO: arquivos + captura/descricao do resultado em 1280px e 390px + build limpo.
HISTORICO: hanzo: navigation.ts + sidebar 232px agrupada. Commit d412c4f.

---

## TASK-223 — Shell mobile: bottom tab bar (5 abas) e tela "Mais" (mockup mobile s_mais) (hanzo)

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: TASK-222, TASK-221
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/identidade-visual.md, .claude/context/stack.md, mockup mobile `.claude/context/mockups/Prototipo Mobile.dc.html` (tela s_mais linhas 1018 em diante: cartao do usuario, grupos Organizacao e Acompanhamento, Sair; barra Inicio/Lancamentos/Cartao/Contas/Mais), MyFinanceFrontEnd/src/app/components/BottomTabBar.tsx, MyFinanceFrontEnd/src/app/navigation.ts, MyFinanceFrontEnd/src/app/routes.tsx, MyFinanceFrontEnd/src/features/auth/useAuth.ts
ESCOPO: Trocar a folha "Mais" por rota `/mais` com `MaisPage` (cartao com inicial/nome/email do usuario, listas agrupadas vindas de NAV_GROUPS, Sair + ThemeToggle) e ajustar BottomTabBar para 5 abas fixas, "Mais" ativa quando a rota atual e /mais ou qualquer destino secundario.
CRITERIO DE ACEITE: 1) tela /mais conforme o mockup mobile; 2) todos os destinos secundarios alcancaveis a partir de /mais; 3) desktop (>=md) sem mudanca e build limpo.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/app/components/BottomTabBar.tsx, MyFinanceFrontEnd/src/app/MaisPage.tsx (novo), MyFinanceFrontEnd/src/app/routes.tsx, MyFinanceFrontEnd/src/app/navigation.ts
NAO FAZER: nao registrar rotas de outras telas; nao tocar AppShell.tsx; nao editar .claude/tasks.md.
RETORNO ESPERADO: arquivos + descricao em 390px + build limpo.
HISTORICO: hanzo: /mais (MaisPage) + BottomTabBar 5 abas. Commit d412c4f. BrandTitle/UserFooter sem uso no AppShell (limpeza pendente).

---

## TASK-224 — Tela Login (mockup s_login desktop e mobile) (hanzo)

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: TASK-220
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/identidade-visual.md, .claude/context/clean-code.md, mockup desktop `.claude/context/mockups/Prototipo Desktop.dc.html` (s_login, linhas 19-54: painel de marca 50% #26215C + formulario 360px) e mobile `.claude/context/mockups/Prototipo Mobile.dc.html` (s_login, linhas 18-57), MyFinanceFrontEnd/src/features/auth/LoginPage.tsx, MyFinanceFrontEnd/src/features/auth/components/LoginBrandPanel.tsx, MyFinanceFrontEnd/src/features/auth/components/LoginForm.tsx, MyFinanceFrontEnd/src/features/auth/components/RegisterForm.tsx
ESCOPO: Alinhar LoginPage/LoginBrandPanel/LoginForm/RegisterForm ao mockup (desktop em duas colunas; mobile em coluna unica com marca no topo), usando primitivos de shared/ui.
CRITERIO DE ACEITE: 1) layout fiel nas duas larguras; 2) fluxo de login e registro inalterado; 3) elementos sem regra de negocio ("Esqueci minha senha", "Entrar com codigo de acesso") NAO renderizados.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/auth/LoginPage.tsx, MyFinanceFrontEnd/src/features/auth/components/LoginBrandPanel.tsx, MyFinanceFrontEnd/src/features/auth/components/LoginForm.tsx, MyFinanceFrontEnd/src/features/auth/components/RegisterForm.tsx
NAO FAZER: nao alterar AuthContext nem api de auth; nao editar .claude/tasks.md.
RETORNO ESPERADO: arquivos + build limpo.
HISTORICO: hanzo: login/registro alinhados ao mockup; Esqueci senha e codigo de acesso removidos (sem regra). Build ok.

---

## TASK-225 — Tela Dashboard (mockup s_dashboard desktop e mobile) (hanzo)

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: TASK-222, TASK-223, TASK-221
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/identidade-visual.md, .claude/context/clean-code.md, .claude/context/regra-de-negocio.md (itens 2, 9, 10, 12 e 14), mockup desktop `.claude/context/mockups/Prototipo Desktop.dc.html` (s_dashboard linhas 55-164) e mobile `.claude/context/mockups/Prototipo Mobile.dc.html` (s_dashboard linhas 58-167: saldo, acoes Novo lancamento/Transferir/Pagar conta, Resumo do mes), MyFinanceFrontEnd/src/features/dashboard/DashboardPage.tsx, MyFinanceFrontEnd/src/features/dashboard/components/AcoesRapidas.tsx, MyFinanceFrontEnd/src/features/dashboard/components/CardSaldoProjetado.tsx, MyFinanceFrontEnd/src/features/dashboard/components/UltimosLancamentos.tsx, MyFinanceFrontEnd/src/features/dashboard/components/GraficoEntradasSaidas.tsx, MyFinanceFrontEnd/src/features/dashboard/components/LimiteGastoIndicador.tsx, MyFinanceFrontEnd/src/features/dashboard/components/SeletorWidgets.tsx, MyFinanceFrontEnd/src/features/dashboard/hooks/useProjecaoMes.ts
ESCOPO: Desktop: saudacao + 3 cards (Saldo total, Entradas, Saidas) + grade 1.6fr/1fr com Ultimos lancamentos, Contas a pagar (clique leva a /conciliacao) e Contas (saldos, clique leva a /contas). Mobile: saldo, acoes rapidas (Novo lancamento, Transferir abre o fluxo de transferencia de /lancamentos, Pagar conta -> /conciliacao), Resumo do mes, Ultimos lancamentos. Usar so hooks existentes; widgets que o mockup nao mostra (GraficoEntradasSaidas, LimiteGastoIndicador, SeletorWidgets) ficam abaixo da grade do mockup, sem remover funcionalidade.
CRITERIO DE ACEITE: 1) valores vindos do backend (ProjecaoMes/lista de contas), nunca recalculados no front; 2) badges Pago/Pendente/Manual corretos; 3) responsivo e build limpo.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/dashboard/DashboardPage.tsx, MyFinanceFrontEnd/src/features/dashboard/components/AcoesRapidas.tsx, MyFinanceFrontEnd/src/features/dashboard/components/CardSaldoProjetado.tsx, MyFinanceFrontEnd/src/features/dashboard/components/UltimosLancamentos.tsx, MyFinanceFrontEnd/src/features/dashboard/components/GraficoEntradasSaidas.tsx, MyFinanceFrontEnd/src/features/dashboard/components/LimiteGastoIndicador.tsx, MyFinanceFrontEnd/src/features/dashboard/components/SeletorWidgets.tsx, MyFinanceFrontEnd/src/features/dashboard/components/ContasAPagarResumo.tsx (novo), MyFinanceFrontEnd/src/features/dashboard/components/ContasResumo.tsx (novo)
NAO FAZER: nao criar endpoint nem recalcular saldos; nao incluir "Buscar..." nem avatar; nao editar .claude/tasks.md.
RETORNO ESPERADO: arquivos + build limpo.
HISTORICO: hanzo: dashboard conforme mockup; widgets extras abaixo da grade. Commit d412c4f. Pendente: LancamentosPage ler ?novo=transferencia; contas a pagar so do primeiro cartao; saldo total so contas Banco.

---

## TASK-226 — Tela Contas (mockup s_contas + modal Nova conta, desktop e mobile) (hanzo)

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: TASK-197, TASK-222, TASK-221
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/identidade-visual.md, .claude/context/clean-code.md, .claude/context/regra-de-negocio.md (itens 8.5, 10 e 10.1), mockup desktop `.claude/context/mockups/Prototipo Desktop.dc.html` (s_contas linhas 165-248, m_contas linhas 249-288) e mobile `.claude/context/mockups/Prototipo Mobile.dc.html` (linhas 168-283), MyFinanceFrontEnd/src/features/contas/ContasPage.tsx, MyFinanceFrontEnd/src/features/contas/components/ContaCard.tsx, MyFinanceFrontEnd/src/features/contas/components/ContaItem.tsx, MyFinanceFrontEnd/src/features/contas/components/ContaBadgeOrigem.tsx, MyFinanceFrontEnd/src/features/contas/components/ContaIcone.tsx, MyFinanceFrontEnd/src/features/contas/components/FormNovaConta.tsx, MyFinanceFrontEnd/src/features/contas/components/PatrimonioTotalResumo.tsx
ESCOPO: Card de patrimonio total + grade 2 colunas de contas (mobile 1 coluna) no mockup; contas de investimento com ativo vinculado mostram saldo calculado e saldo nao editavel; desativar conta com ativo ativo mostra o erro 409 do backend; modal Nova conta (nome, tipo, saldo inicial, origem Manual).
CRITERIO DE ACEITE: 1) patrimonio = valor do hook da TASK-197 (sem soma dupla); 2) conta com SaldoCalculado exibe saldo calculado e bloqueia edicao; 3) responsivo e build limpo.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/contas/ContasPage.tsx, MyFinanceFrontEnd/src/features/contas/components/ContaCard.tsx, MyFinanceFrontEnd/src/features/contas/components/ContaItem.tsx, MyFinanceFrontEnd/src/features/contas/components/ContaBadgeOrigem.tsx, MyFinanceFrontEnd/src/features/contas/components/ContaIcone.tsx, MyFinanceFrontEnd/src/features/contas/components/FormNovaConta.tsx, MyFinanceFrontEnd/src/features/contas/components/PatrimonioTotalResumo.tsx
NAO FAZER: nao renderizar badge "OFX" nem "Conectar via OFX" (Open Finance e v2); nao editar .claude/tasks.md.
RETORNO ESPERADO: arquivos + build limpo.
HISTORICO: 2026-10-13 (hanzo). Tela Contas largura total, contas agrupadas por tipo (lib/agruparContas), useContas inclui cartoes (so exibicao), Atualizar saldo oculto quando saldoCalculado, 409 ao desativar conta com ativo exibido. Extra: edicao de conta (item 10.2, decisao do usuario 2026-10-13: nome/icone/cor/subtipo, tipo imutavel) - killua desenhou (Kira gravou regra e esqueleto), mike 23 testes RED, levi GREEN (1071/1071), style APROVADO; PUT /api/contas/{id}. Suposicoes pendentes de confirmacao: conta inativa nao edita (409), PUT substitui icone/cor/subtipo, Open Finance pode renomear, dias de fechamento/vencimento fora.

---

## TASK-227 — Tela Lancamentos (mockup s_lancamentos + modais Novo/Editar/Cancelar parcelado, desktop e mobile) (hanzo)

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: TASK-190, TASK-183, TASK-189, TASK-221, TASK-222
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/identidade-visual.md, .claude/context/clean-code.md, .claude/context/regra-de-negocio.md (itens 5, 14, 17 e 18), mockup desktop `.claude/context/mockups/Prototipo Desktop.dc.html` (s_lancamentos linhas 289-376, m_lancamentos 377-385, lConfirm 386-389, xe_lanc 499-507) e mobile `.claude/context/mockups/Prototipo Mobile.dc.html` (linhas 285-402: lista por dia, folha de acoes `sheet:lanc:*`, `lmenu`), MyFinanceFrontEnd/src/features/lancamentos/LancamentosPage.tsx, MyFinanceFrontEnd/src/features/lancamentos/components/FormLancamento.tsx, MyFinanceFrontEnd/src/features/lancamentos/components/LancamentoItem.tsx, MyFinanceFrontEnd/src/features/lancamentos/components/FiltroTipoLancamento.tsx, MyFinanceFrontEnd/src/features/lancamentos/components/NavegadorMes.tsx, MyFinanceFrontEnd/src/features/lancamentos/components/AvisoLimiteGasto.tsx, MyFinanceFrontEnd/src/features/lancamentos/components/TransferenciaFluxoCaixaItem.tsx, MyFinanceFrontEnd/src/features/lancamentos/components/FormTransferencia.tsx
ESCOPO: Tabela desktop (descricao, categoria, data, valor+badge, acoes) e lista por dia no mobile com folha de acoes; seletor de mes, chips Todos/Entradas/Saidas e totais; Novo lancamento com Despesa/Receita, toggle "Parcelar" (so despesa; N parcelas, Resumo, "Vale para a 1a parcela") chamando o endpoint de parcelado; Editar lancamento (tipo, valor, data, categoria, conta, status); Editar/Excluir DESABILITADOS quando `avulso=false`, com dica "edite em <destino>" (helper da TASK-190); lancamento parcelado mostra badge "3/10" e acao "Cancelar compra parcelada" com dialogo (N pendentes excluidas, pagas continuam); trocar tipo zera a categoria.
CRITERIO DE ACEITE: 1) regras de habilitacao vem de `avulso`/`origemVinculo` do backend; 2) badge Sugerido apenas se o backend enviar (nunca criado pelo front); 3) responsivo e build limpo.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/lancamentos/LancamentosPage.tsx, MyFinanceFrontEnd/src/features/lancamentos/components/FormLancamento.tsx, MyFinanceFrontEnd/src/features/lancamentos/components/LancamentoItem.tsx, MyFinanceFrontEnd/src/features/lancamentos/components/FiltroTipoLancamento.tsx, MyFinanceFrontEnd/src/features/lancamentos/components/NavegadorMes.tsx, MyFinanceFrontEnd/src/features/lancamentos/components/AvisoLimiteGasto.tsx, MyFinanceFrontEnd/src/features/lancamentos/components/TransferenciaFluxoCaixaItem.tsx, MyFinanceFrontEnd/src/features/lancamentos/components/FormTransferencia.tsx, MyFinanceFrontEnd/src/features/lancamentos/components/FormLancamentoParcelado.tsx (novo), MyFinanceFrontEnd/src/features/lancamentos/components/CancelarCompraParceladaDialog.tsx (novo)
NAO FAZER: nao calcular valor de parcela no front como fonte de verdade (resumo e so previa; backend define); nao permitir edicao de parcela isolada; nao editar .claude/tasks.md.
RETORNO ESPERADO: arquivos + build limpo.
HISTORICO: 2026-10-13 (hanzo). Tela Lancamentos largura total, tabela desktop/lista por dia mobile, toggle Parcelar, editar/excluir so avulso, cancelar compra parcelada, ?novo=transferencia|lancamento. Usuario aprovou o visual. Style rodada 1 PRECISA CORRIGIR (clean-code) -> rodada 2 APROVADO. Campo de categoria unificado em categorias/components/CategoriaCampoSelect.

---

## TASK-228 — Tela Cartao (mockup s_cartao + Nova compra + Antecipar parcelas, desktop e mobile) (hanzo)

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: TASK-204, TASK-203, TASK-221, TASK-222
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/identidade-visual.md, .claude/context/clean-code.md, .claude/context/regra-de-negocio.md (item 12), mockup desktop `.claude/context/mockups/Prototipo Desktop.dc.html` (s_cartao linhas 390-486, m_cartao Nova compra 487-492, antecipar `cAnt` 493-498) e mobile `.claude/context/mockups/Prototipo Mobile.dc.html` (linhas 403-495: `cmenu`, "Antecipar parcelas"), MyFinanceFrontEnd/src/features/cartao/ContaCartaoPage.tsx, MyFinanceFrontEnd/src/features/cartao/components/CartaoVisual.tsx, MyFinanceFrontEnd/src/features/cartao/components/FaturasSection.tsx, MyFinanceFrontEnd/src/features/cartao/components/FaturaItem.tsx, MyFinanceFrontEnd/src/features/cartao/components/LancarCompraForm.tsx, MyFinanceFrontEnd/src/features/cartao/components/PagarFaturaModal.tsx, MyFinanceFrontEnd/src/features/cartao/components/StatusFaturaBadge.tsx, MyFinanceFrontEnd/src/features/assinaturas-cartao/AssinaturasCartaoSection.tsx
ESCOPO: Coluna esquerda (CartaoVisual, fatura atual/status/vencimento, limite usado) e direita (abas de mes, Nova compra, lista de compras da fatura); parcela antecipada com badge "antecipada"; compra parcelada abre acao "Antecipar parcelas" (menu no mobile) com modal: Stepper de quantidade (max = elegiveis do backend), previa "Vao para a fatura atual", texto "Antecipar nao paga a fatura, so traz as parcelas para ela. Se o banco der desconto, lance um estorno na fatura."; Nova compra com parcelas opcional e Resumo; manter pagar fatura e a secao de assinaturas acessiveis.
CRITERIO DE ACEITE: 1) quantidade limitada ao numero de elegiveis do backend e erro 400 exibido; 2) apos antecipar, faturas atualizam (cache invalidado); 3) responsivo e build limpo.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/cartao/ContaCartaoPage.tsx, MyFinanceFrontEnd/src/features/cartao/components/CartaoVisual.tsx, MyFinanceFrontEnd/src/features/cartao/components/FaturasSection.tsx, MyFinanceFrontEnd/src/features/cartao/components/FaturaItem.tsx, MyFinanceFrontEnd/src/features/cartao/components/LancarCompraForm.tsx, MyFinanceFrontEnd/src/features/cartao/components/PagarFaturaModal.tsx, MyFinanceFrontEnd/src/features/cartao/components/StatusFaturaBadge.tsx, MyFinanceFrontEnd/src/features/cartao/components/AnteciparParcelasModal.tsx (novo), MyFinanceFrontEnd/src/features/assinaturas-cartao/AssinaturasCartaoSection.tsx
NAO FAZER: nao recalcular fatura nem decidir elegibilidade no front; nao oferecer "desfazer antecipacao" nem campo de desconto; nao editar .claude/tasks.md.
RETORNO ESPERADO: arquivos + build limpo.
HISTORICO: 2026-10-13 (hanzo). Tela Cartao: compras da fatura via GET novo, antecipar sem dataAntecipacao, layout lado a lado so em xl, modais no padrao FormModal (promovido para shared/ui). Kira inline: AppShell main min-w-0 e link Ver relatorio. Style rodada 1 PRECISA CORRIGIR (clean-code) -> rodada 2 APROVADO (skipToken nos hooks, previa de antecipacao em centavos). Pendente: bug pre-existente do FaturaCicloService fechando faturas futuras (aguarda ok do usuario).

---

## TASK-229 — Tela Categorias (mockup s_categorias + Nova/Editar categoria com limite, desktop e mobile) (hanzo)

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: TASK-221, TASK-222
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/identidade-visual.md, .claude/context/clean-code.md, .claude/context/regra-de-negocio.md (itens 7 e 14), mockup desktop `.claude/context/mockups/Prototipo Desktop.dc.html` (s_categorias linhas 508-531, xe_cat Editar categoria linha 507) e mobile `.claude/context/mockups/Prototipo Mobile.dc.html` (linhas 505-525, `sheet:cat:*`, `catlimrm`), MyFinanceFrontEnd/src/features/categorias/CategoriasPage.tsx, MyFinanceFrontEnd/src/features/categorias/FormCategoria.tsx, MyFinanceFrontEnd/src/features/categorias/components/CategoriaItem.tsx, MyFinanceFrontEnd/src/features/categorias/components/CampoLimiteGasto.tsx, MyFinanceFrontEnd/src/features/categorias/components/CategoriaSelect.tsx
ESCOPO: Abas Despesas/Receitas; linhas com icone, nome, "R$ gasto / limite" com barra (estourado em destaque) e acoes editar/arquivar (folha de acoes no mobile); modal Nova/Editar categoria com Despesa/Receita (tipo travado na edicao, item 7), Nome, Icone, Limite de gasto mensal (opcional, so despesa) e "Remover limite"; salvar categoria e limite no mesmo formulario (sequencia editar categoria -> definir/remover limite, com tratamento de falha parcial).
CRITERIO DE ACEITE: 1) edicao nao altera tipo; 2) "Remover limite" chama o endpoint existente; 3) responsivo e build limpo.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/categorias/CategoriasPage.tsx, MyFinanceFrontEnd/src/features/categorias/FormCategoria.tsx, MyFinanceFrontEnd/src/features/categorias/components/CategoriaItem.tsx, MyFinanceFrontEnd/src/features/categorias/components/CampoLimiteGasto.tsx, MyFinanceFrontEnd/src/features/categorias/components/CategoriaSelect.tsx
NAO FAZER: nao criar endpoint (backend do item 7 ja existe); nao hard-delete (arquivar e a unica exclusao); nao editar .claude/tasks.md.
RETORNO ESPERADO: arquivos + build limpo.
HISTORICO: hanzo: categorias conforme mockup, commit 85ef501. Campo Categoria-pai mantido; Remover limite age na hora.

---

## TASK-230 — Tela De-para de categoria (mockup s_depara desktop e mobile) (hanzo)

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: TASK-219, TASK-221, TASK-222, TASK-223
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/identidade-visual.md, .claude/context/clean-code.md, .claude/context/regra-de-negocio.md (item 7 De-para e "Escopo: v1 vs v2"), mockup desktop `.claude/context/mockups/Prototipo Desktop.dc.html` (s_depara linhas 532-601) e mobile `.claude/context/mockups/Prototipo Mobile.dc.html` (linhas 526-604: "vira"), MyFinanceFrontEnd/src/features/de-para/hooks/useDeParaCategorias.ts, MyFinanceFrontEnd/src/features/categorias/components/CategoriaSelect.tsx, MyFinanceFrontEnd/src/app/routes.tsx
ESCOPO: Criar `features/de-para/DeParaPage.tsx` e componentes: lista de vinculos (categoria importada -> categoria do app, badge Mapeado), criar vinculo ("Aplicar e lembrar para futuras"), editar e remover; registrar a rota `/depara`.
CRITERIO DE ACEITE: 1) CRUD funcional contra o endpoint existente; 2) abas/estados "Pendentes" e "Sugerido" NAO renderizados (dependem de import, v2); 3) responsivo (mobile com "vira") e build limpo.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/de-para/DeParaPage.tsx (novo), MyFinanceFrontEnd/src/features/de-para/components/DeParaItem.tsx (novo), MyFinanceFrontEnd/src/features/de-para/components/FormDePara.tsx (novo), MyFinanceFrontEnd/src/app/routes.tsx
NAO FAZER: nao criar endpoint de pendentes; nao implementar import; nao editar .claude/tasks.md.
RETORNO ESPERADO: arquivos + build limpo.
HISTORICO: hanzo: DeParaPage + rota /de-para. Commit d412c4f. Select inline do mockup trocado por editar/remover.

---

## TASK-231 — Tela Conciliacao / contas a pagar (mockup s_conciliacao, caminho v1 manual, desktop e mobile) (hanzo)

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: TASK-230, TASK-221, TASK-222
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/identidade-visual.md, .claude/context/clean-code.md, .claude/context/regra-de-negocio.md (itens 5, 9, 12, 13 e "Escopo: v1 vs v2"), mockup desktop `.claude/context/mockups/Prototipo Desktop.dc.html` (s_conciliacao linhas 602-676) e mobile `.claude/context/mockups/Prototipo Mobile.dc.html` (linhas 608-684), MyFinanceFrontEnd/src/features/lancamentos/hooks/useFluxoCaixaTodasContas.ts, MyFinanceFrontEnd/src/features/lancamentos/hooks/useMarcarComoPago.ts, MyFinanceFrontEnd/src/features/contas-receber/hooks/useContasReceber.ts, MyFinanceFrontEnd/src/app/routes.tsx
ESCOPO: Criar `features/conciliacao/ConciliacaoPage.tsx`: abas A pagar / A receber / Conciliado; "A pagar" lista lancamentos DEBIT PENDENTE (fluxo de caixa de todas as contas) com situacao Atrasado / Vence em N dias e botao Pagar (marcar como pago); a linha do Cartao de credito leva a /cartao (nao reimplementa pagamento de fatura); "A receber" lista contas a receber nao recebidas; "Conciliado" lista PAGOS do mes; resumo Em aberto / Atrasado; registrar a rota `/conciliacao` (serializar apos TASK-230 por causa de routes.tsx).
CRITERIO DE ACEITE: 1) Pagar usa o endpoint de marcar como pago existente; 2) linha "Sugerido / Sugestao de conciliacao / Revisar" NAO renderizada (conciliacao com extrato e Open Finance, v2); 3) responsivo e build limpo.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/conciliacao/ConciliacaoPage.tsx (novo), MyFinanceFrontEnd/src/features/conciliacao/components/ItemContaAPagar.tsx (novo), MyFinanceFrontEnd/src/features/conciliacao/lib/situacaoDoVencimento.ts (novo), MyFinanceFrontEnd/src/app/routes.tsx
NAO FAZER: nao criar endpoint; nao criar status SUGERIDO; nao editar .claude/tasks.md.
RETORNO ESPERADO: arquivos + build limpo.
HISTORICO: hanzo: ConciliacaoPage + rota /conciliacao. Commit d412c4f. Linha do cartao soma COMPETENCIA_CARTAO pendente (nao conferido com backend).

---

## TASK-232 — Tela Contas fixas (mockup s_fixa + Nova/Editar/Apagar, desktop e mobile) (hanzo)

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: TASK-177, TASK-176, TASK-221, TASK-222
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/identidade-visual.md, .claude/context/clean-code.md, .claude/context/regra-de-negocio.md (item 6), mockup desktop `.claude/context/mockups/Prototipo Desktop.dc.html` (s_fixa linhas 677-753, xe_fixa Editar conta fixa linha 507, dialogo `xDlg`) e mobile `.claude/context/mockups/Prototipo Mobile.dc.html` (linhas 687-757), MyFinanceFrontEnd/src/features/contas-fixas/ListaContasFixas.tsx, MyFinanceFrontEnd/src/features/contas-fixas/FormContaFixa.tsx, MyFinanceFrontEnd/src/features/contas-fixas/components/ContaFixaItem.tsx
ESCOPO: Cabecalho com Total fixo mensal; tabela Descricao / Recorrencia ("Mensal - dia 5", "Anual - marco") / Valor / Ativa (switch) / editar / apagar; modal Nova e Editar conta fixa (nome, valor, dia, categoria, conta de pagamento, Mensal/Anual, aviso "Mudar valor, dia ou categoria atualiza os lancamentos pendentes ja gerados. Os pagos nao mudam."); apagar com ConfirmDialog destrutivo explicando: pendentes sao excluidos, pagos permanecem como lancamentos avulsos, sem desfazer.
CRITERIO DE ACEITE: 1) apagar usa useApagarContaFixa; 2) texto do dialogo bate com o item 6; 3) responsivo e build limpo.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/contas-fixas/ListaContasFixas.tsx, MyFinanceFrontEnd/src/features/contas-fixas/FormContaFixa.tsx, MyFinanceFrontEnd/src/features/contas-fixas/components/ContaFixaItem.tsx
NAO FAZER: nao permitir parcelamento em conta fixa; nao editar .claude/tasks.md.
RETORNO ESPERADO: arquivos + build limpo.
HISTORICO: hanzo: contas fixas conforme mockup; total fixo mensal somado no front (sem endpoint); anual sem mes (DTO nao traz). Commit 3bf2a23.

---

## TASK-233 — Tela Contas a receber (mockup s_receber + Registrar recebimento + Editar, desktop e mobile) (hanzo)

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: TASK-211, TASK-210, TASK-221, TASK-222
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/identidade-visual.md, .claude/context/clean-code.md, .claude/context/regra-de-negocio.md (itens 9 e 13), mockup desktop `.claude/context/mockups/Prototipo Desktop.dc.html` (s_receber linhas 978-1065, xe_rec Editar conta a receber linha 507) e mobile `.claude/context/mockups/Prototipo Mobile.dc.html` (linhas 942-1017, `sheet:rec:*`), MyFinanceFrontEnd/src/features/contas-receber/ListaContasReceber.tsx, MyFinanceFrontEnd/src/features/contas-receber/FormRegistrarContaReceber.tsx, MyFinanceFrontEnd/src/features/contas-receber/FormRegistrarRecebimento.tsx, MyFinanceFrontEnd/src/features/contas-receber/components/ContaReceberItem.tsx
ESCOPO: Cartoes A receber / Ja recebido / Atrasado, abas Todos/Emprestimos/Recebiveis, linhas com vencimento, progresso "R$ 300 de R$ 900", valor total, status e acoes Receber / Editar / Excluir (folha de acoes no mobile); modal Registrar recebimento (Parcial/Total, valor, data, conta de destino); novo modal Editar conta a receber (descricao, pessoa so em emprestimo, data prevista, valor total desabilitado com ajuda quando nao editavel, via `podeEditarValorTotal`).
CRITERIO DE ACEITE: 1) valores e saldos vem do backend; 2) 409 do PUT exibido como mensagem; 3) responsivo e build limpo.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/contas-receber/ListaContasReceber.tsx, MyFinanceFrontEnd/src/features/contas-receber/FormRegistrarContaReceber.tsx, MyFinanceFrontEnd/src/features/contas-receber/FormRegistrarRecebimento.tsx, MyFinanceFrontEnd/src/features/contas-receber/FormEditarContaReceber.tsx (novo), MyFinanceFrontEnd/src/features/contas-receber/components/ContaReceberItem.tsx
NAO FAZER: nao editar ocorrencia de recebivel recorrente (edita-se o molde); nao editar .claude/tasks.md.
RETORNO ESPERADO: arquivos + build limpo.
HISTORICO: hanzo: contas a receber conforme mockup + modal Editar; resumo somado no front; import de conciliacao/lib. Commit 3bf2a23.

---

## TASK-234 — Tela Investimentos (mockup s_investimentos + Novo ativo com conta, desktop e mobile) (hanzo)

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: TASK-197, TASK-196, TASK-221, TASK-222
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/identidade-visual.md, .claude/context/clean-code.md, .claude/context/regra-de-negocio.md (itens 8, 8.1, 8.2, 8.4 e 8.5), mockup desktop `.claude/context/mockups/Prototipo Desktop.dc.html` (s_investimentos linhas 831-931, m_investimentos 932-977) e mobile `.claude/context/mockups/Prototipo Mobile.dc.html` (linhas 793-940), MyFinanceFrontEnd/src/features/investimentos/ListaAtivosPage.tsx, MyFinanceFrontEnd/src/features/investimentos/components/ResumoAtivosCards.tsx, MyFinanceFrontEnd/src/features/investimentos/components/GraficoConsolidadoAtivos.tsx, MyFinanceFrontEnd/src/features/investimentos/components/AtivoCard.tsx, MyFinanceFrontEnd/src/features/investimentos/components/AtivoItem.tsx, MyFinanceFrontEnd/src/features/investimentos/components/FiltroTipoAtivo.tsx, MyFinanceFrontEnd/src/features/investimentos/components/ModalNovoAtivo.tsx, MyFinanceFrontEnd/src/features/investimentos/components/FormRegistrarAporte.tsx
ESCOPO: Total investido (item 8.5, hook `useTotalInvestido`), tiles Renda fixa / Renda variavel com % da carteira, bloco Carteira (donut + legenda), tabela Ativo / Tipo / Conta / Valor / Evolucao (mobile: lista com filtro Todos/Renda fixa/Variavel); coluna Conta com "Sem conta" + acao "Vincular conta" para ativo antigo; Novo ativo com nome, tipo, quantidade + preco unitario (item 8.1), data da compra, seletor de conta de investimento e atalho "Nova conta de investimento"; manter aporte, dividendo, valor atual e desativar acessiveis.
CRITERIO DE ACEITE: 1) conta obrigatoria no cadastro; 2) "Vincular conta" troca/define a conta e nunca volta para null; 3) responsivo e build limpo.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/investimentos/ListaAtivosPage.tsx, MyFinanceFrontEnd/src/features/investimentos/components/ResumoAtivosCards.tsx, MyFinanceFrontEnd/src/features/investimentos/components/GraficoConsolidadoAtivos.tsx, MyFinanceFrontEnd/src/features/investimentos/components/AtivoCard.tsx, MyFinanceFrontEnd/src/features/investimentos/components/AtivoItem.tsx, MyFinanceFrontEnd/src/features/investimentos/components/FiltroTipoAtivo.tsx, MyFinanceFrontEnd/src/features/investimentos/components/ModalNovoAtivo.tsx, MyFinanceFrontEnd/src/features/investimentos/components/FormRegistrarAporte.tsx, MyFinanceFrontEnd/src/features/investimentos/components/VincularContaAtivoModal.tsx (novo)
NAO FAZER: nao renderizar "+4,2% no mes" (sem historico mensal na v1); nao usar campo "Valor investido" direto no cadastro (a regra e quantidade x preco unitario); nao editar .claude/tasks.md.
RETORNO ESPERADO: arquivos + build limpo.
HISTORICO: 2026-10-13 (hanzo). Tela Investimentos: total investido do back, tiles RF/RV com % da carteira, donut, tabela Ativo/Tipo/Conta/Valor/Evolucao, Vincular conta (nunca null), Novo ativo com conta obrigatoria e quantidade x preco. Desvios: sem sparkline e '+4,2%' (sem historico), filtros tambem no desktop, grafico de rendimentos mantido. Style rodada 1 PRECISA CORRIGIR (invalidacao do total) -> rodada 2 APROVADO.

---

## TASK-235 — Tela Relatorio por categoria (mockup s_relatorio, desktop e mobile) (hanzo)

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: TASK-231, TASK-218, TASK-217, TASK-221, TASK-222
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/identidade-visual.md, .claude/context/clean-code.md, .claude/context/regra-de-negocio.md (item 19), mockup desktop `.claude/context/mockups/Prototipo Desktop.dc.html` (s_relatorio linhas 754-827) e mobile `.claude/context/mockups/Prototipo Mobile.dc.html` (linhas 759-791), MyFinanceFrontEnd/src/features/relatorios/hooks/useRelatorioGastoPorCategoria.ts, MyFinanceFrontEnd/src/features/relatorios/lib/navegarMes.ts, MyFinanceFrontEnd/src/app/routes.tsx
ESCOPO: Criar `features/relatorios/RelatorioCategoriaPage.tsx`: seletor de mes, cartao Total gasto com variacao "+8% vs mes anterior" (oculta quando o backend manda variacao nula), lista de categorias com valor, % do total e barra, ordenada como vem do backend (inclui "Sem categoria"), clique leva a `/relatorio/:categoriaId`; registrar a rota `/relatorio` (serializar apos TASK-231 por causa de routes.tsx).
CRITERIO DE ACEITE: 1) nenhum total/percentual calculado no front; 2) mes sem dados mostra estado vazio; 3) responsivo e build limpo.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/relatorios/RelatorioCategoriaPage.tsx (novo), MyFinanceFrontEnd/src/features/relatorios/components/LinhaCategoriaRelatorio.tsx (novo), MyFinanceFrontEnd/src/features/relatorios/components/ResumoTotalGasto.tsx (novo), MyFinanceFrontEnd/src/app/routes.tsx
NAO FAZER: nao criar endpoint; nao editar .claude/tasks.md.
RETORNO ESPERADO: arquivos + build limpo.
HISTORICO: hanzo: RelatorioCategoriaPage + rota /relatorio. Donut do mockup omitido (sem paleta). Commit 470b13b.

---

## TASK-236 — Tela Detalhe da categoria no relatorio (mockup s_relcat, desktop e mobile) (hanzo)

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS: TASK-235
FLUXO: Implementacao
CONTEXTO A LER: .claude/context/identidade-visual.md, .claude/context/clean-code.md, .claude/context/regra-de-negocio.md (itens 14, 18 e 19), mockup desktop `.claude/context/mockups/Prototipo Desktop.dc.html` (s_relcat linhas 828-830) e mobile `.claude/context/mockups/Prototipo Mobile.dc.html` (linhas 792), MyFinanceFrontEnd/src/features/relatorios/hooks/useRelatorioCategoriaDetalhe.ts, MyFinanceFrontEnd/src/features/relatorios/RelatorioCategoriaPage.tsx, MyFinanceFrontEnd/src/app/routes.tsx
ESCOPO: Criar `RelatorioCategoriaDetalhePage.tsx` (rota `/relatorio/:categoriaId`, e `/relatorio/sem-categoria`): cabecalho "Alimentacao - Julho 2026", Total na categoria, "16% do total gasto", barra "Limite de gasto R$ 620 de R$ 900" (so se houver limite), tabela Descricao / Data / Conta / Valor (cartao identificado como tal, subcategoria quando houver) em data crescente, voltar para /relatorio.
CRITERIO DE ACEITE: 1) somente leitura (sem editar/excluir; edicao e pela tela de origem, item 18); 2) soma exibida = total do backend; 3) responsivo e build limpo.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/relatorios/RelatorioCategoriaDetalhePage.tsx (novo), MyFinanceFrontEnd/src/features/relatorios/components/TabelaLancamentosCategoria.tsx (novo), MyFinanceFrontEnd/src/app/routes.tsx
NAO FAZER: nao calcular percentuais; nao editar .claude/tasks.md.
RETORNO ESPERADO: arquivos + build limpo.
HISTORICO: hanzo: RelatorioCategoriaDetalhePage + rotas /relatorio/sem-categoria e /relatorio/:id. Largura da barra de limite calculada no front. Commit 470b13b.


## TASK-237 — Front: consolidar duplicacoes residuais apontadas pelo style (hanzo)

STATUS: CONCLUIDA
AGENT: hanzo
DEPENDENCIAS:
FLUXO: Melhoria
CONTEXTO A LER: .claude/context/clean-code.md, .claude/context/stack.md
ESCOPO: Eliminar as copias residuais: LIMIAR_PERTO_DO_LIMITE (dashboard/LimiteGastoIndicador e lancamentos/lib/limiarAlertaLimite passam a importar de limite-gasto/lib/estadoLimite), CategoriaSelect/achatarCategorias e AssinaturasCartaoSection passam a usar categorias/lib (montarOpcoesCategoria/construirMapaCategorias), dataDeHoje de investimentos/lib e o shim cartao/lib/formatarData apontam para shared/lib/datas, useContasParaSelecao promovido para shared/hooks.
CRITERIO DE ACEITE: 1) cada helper existe em um unico lugar; 2) comportamento visual identico; 3) npx tsc -b limpo.
ARQUIVOS PERMITIDOS: MyFinanceFrontEnd/src/features/{dashboard,lancamentos,limite-gasto,categorias,assinaturas-cartao,investimentos,cartao,contas-receber,contas-fixas}/**, MyFinanceFrontEnd/src/shared/{lib,hooks}/**
NAO FAZER: nao mudar regra nem visual; nao editar .claude/tasks.md; nao commitar.
RETORNO ESPERADO: arquivos tocados + tsc.
HISTORICO: 2026-10-14 (hanzo). LIMIAR via limite-gasto/lib/estadoLimite (dashboard e AvisoLimiteGasto; limiarAlertaLimite.ts removido), CategoriaSelect sobre montarOpcoesCategoria (campo subcategoria?), AssinaturasCartaoSection com construirMapaCategorias, datas so em shared/lib/datas (shim cartao/lib/formatarData e investimentos/lib/dataDeHoje removidos; dataDeHoje passa a usar data local), useContasParaSelecao em shared/hooks. Style rodada 1 PRECISA CORRIGIR (shared dependia de contas-receber via contasReceberKeys) -> chave movida para contasKeys -> reconferido pelo Kira. tsc limpo.

## TASK-238 — Back: FaturaCicloService fecha faturas futuras ao lancar compra parcelada (diagnostico)

STATUS: CONCLUIDA
AGENT: killua
DEPENDENCIAS:
FLUXO: Correcao
CONTEXTO A LER: .claude/context/regra-de-negocio.md (item 12), MyFinances/MyFinances/Services/FaturaCicloService.cs
ESCOPO: ResolverFaturaParaLancamentoAsync (~linhas 27-36) fecha a fatura ABERTA e cria a seguinte quando a data do lancamento >= DataFechamento; parcelas futuras de compra parcelada fecham todas as faturas intermediarias (so a ultima fica ABERTA), quebrando "fatura atual" e elegiveis de antecipacao. Regra critica: killua -> mike RED -> levi GREEN -> style.
CRITERIO DE ACEITE: 1) lancar parcela futura nao fecha a fatura aberta atual; 2) faturas futuras nascem em estado coerente com o item 12; 3) suite verde.
ARQUIVOS PERMITIDOS: definidos pelo killua no esqueleto.
NAO FAZER: nao alterar regra sem confirmar com o usuario; nao editar .claude/tasks.md; nao commitar.
RETORNO ESPERADO: diagnostico + esqueleto/contrato para TDD.
HISTORICO: 2026-10-14. Diagnostico killua (bug + 2 extras: duplicata de ciclo no dia do fechamento e compra de hoje caindo na ultima aberta). Decisoes do usuario: status FUTURA (FUTURA->ABERTA->FECHADA->PAGA, uma ABERTA), fechamento automatico pelo calendario (preguicoso), dia de fechamento inclusivo, retroativa em FECHADA entra, em PAGA encerrada rejeita, PAGA nao encerrada reabre, assinatura propaga ABERTA+FUTURA, FUTURA pagavel, dashboard so ABERTA/FECHADA, migration de dados. Kira gravou regra (item 12 "Ciclo e status da fatura") e esqueleto; mike RED 45 testes; levi GREEN (FaturaCicloPolitica, resolver por ciclo, reconciliacao, migration ReclassificarStatusFaturaPorCalendario); mike ajustou 2 testes desatualizados; hanzo front (badge FUTURA, selecionarFaturaAtual, dashboard). Style rodada 1 PRECISA CORRIGIR (ALTA: parcela em fatura PAGA posterior elegivel a antecipacao; baixas: campo morto, nomes "aberta", ciclo opcional no controller) -> mike RED 5 testes -> levi -> style rodada 2 APROVADO. Suite 1157/1157. Migration ensaiada (rollback, idempotente) e aplicada no banco de preview.

## TASK-239 — Back: revalidar fatura ao editar dia_referencia da assinatura de cartao

STATUS: CONCLUIDA
AGENT: killua
DEPENDENCIAS: TASK-238
FLUXO: Correcao
CONTEXTO A LER: .claude/context/regra-de-negocio.md (item 12 "Ciclo e status da fatura" e assinatura de cartao), MyFinances/MyFinances/Services/AssinaturaCartaoService.cs
ESCOPO: AssinaturaCartaoService.EditarAsync recalcula a data da compra vinculada quando dia_referencia muda, mas nao move a compra de fatura quando a nova data cruza o fechamento; a regra diz "a fatura e revalidada". Lacuna pre-existente apontada pelo style na TASK-238. Regra critica: killua -> mike RED -> levi -> style.
CRITERIO DE ACEITE: 1) nova data fora do ciclo move a compra para a fatura do ciclo correto (ABERTA/FUTURA); 2) FECHADA/PAGA nunca alteradas; 3) suite verde.
ARQUIVOS PERMITIDOS: definidos pelo killua.
NAO FAZER: nao editar .claude/tasks.md; nao commitar.
RETORNO ESPERADO: desenho + esqueleto para TDD.
HISTORICO: 2026-10-14. Decisao do usuario: destino FECHADA/PAGA nao move (novo dia vale dali pra frente). killua desenho; mike 15 testes; levi FaturaCicloPolitica.PodeReceberCompraMovida, FaturaCicloService.ResolverFaturaParaCompraMovidaAsync, AssinaturaCartaoService.MoverCompraParaNovoDiaAsync; mike corrigiu cenario DestinoFechada (FECHADA com fechamento futuro nao existe pela regra). Style APROVADO. Suite 1199/1199.

## TASK-240 — Back: indice unico (conta_id, data_fechamento) em fatura

STATUS: CONCLUIDA
AGENT: killua
DEPENDENCIAS: TASK-238
FLUXO: Melhoria
CONTEXTO A LER: .claude/context/regra-de-negocio.md (item 12 "Ciclo e status da fatura"), configuracao EF da entidade Fatura
ESCOPO: Garantir no banco que nao existam duas faturas do mesmo ciclo (corrida entre lancamentos simultaneos), com migration de indice unico e tratamento de conflito no FaturaCicloService.
CRITERIO DE ACEITE: 1) indice unico criado; 2) criacao concorrente reaproveita a fatura existente; 3) suite verde.
ARQUIVOS PERMITIDOS: definidos pelo killua.
NAO FAZER: nao editar .claude/tasks.md; nao commitar.
RETORNO ESPERADO: desenho + esqueleto.
HISTORICO: 2026-10-14. killua desenho; mike testes de conflito (SQLite + decorator de corrida); levi indice IX_fatura_conta_data_fechamento, migration AddIndiceUnicoFaturaContaDataFechamento (guard aborta se houver duplicata), FaturaRepository.Descartar, CriarFaturaAsync recupera a vencedora. Style rodada 1 PRECISA CORRIGIR (teste/comentario prometiam recuperacao dentro de transacao no Postgres, que aborta com 25P02) -> comentario LIMITACAO + teste renomeado -> reconferido. Divida aceita: savepoint/excecao de dominio (docs/modulo-cartao-credito.md).

## TASK-241 — Back: criterio unico de ocorrencia nao confirmada da conta fixa (item 6)

STATUS: CONCLUIDA
AGENT: levi
DEPENDENCIAS:
FLUXO: Correcao
CONTEXTO A LER: .claude/context/regra-de-negocio.md (item 6, Apagar conta fixa e periodicidade/desativar)
ESCOPO: Ao apagar/desativar/limpar periodicidade, ocorrencia nao confirmada (banco Status != Pago, inclusive Sugerido; cartao fatura != Paga) sai junto; confirmada fica desvinculada.
CRITERIO DE ACEITE: 1) Sugerido tratado como nao confirmado nos 3 caminhos; 2) predicado unico; 3) suite verde.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Domain/OcorrenciaContaFixaPolitica.cs, Services/RecorrenciaGeradorService.cs, Services/IRecorrenciaGeradorService.cs
NAO FAZER: mudar o criterio do cartao (FECHADA continua apagavel).
RETORNO ESPERADO: politica + testes.
HISTORICO: 2026-10-14. Decisoes do usuario: SUGERIDO = nao confirmado; FECHADA no cartao continua apagavel. killua achou que apagar pendentes ja estava implementado e que restava so o Sugerido; mike 13 testes; levi OcorrenciaContaFixaPolitica substituindo os privados do RecorrenciaGeradorService. Style APROVADO. Regra item 6 atualizada; percentual do relatorio (item 19) e validacao da edicao de conta a receber (item 13) confirmados na mesma rodada.
