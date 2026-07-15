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

STATUS: CONCLUIDA (16/16 testes GREEN, build limpo. Kira corrigiu inline 5 chamadas de Adicionar sem await — warning CS4014, inconsistente com o padrao ja usado em PagamentoFaturaService/CompraCartaoService)
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

STATUS: PENDENTE
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

STATUS: PENDENTE
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

STATUS: PENDENTE
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

STATUS: PENDENTE
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

STATUS: PENDENTE
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

STATUS: PENDENTE
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

STATUS: PENDENTE
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

STATUS: PENDENTE
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

STATUS: PENDENTE
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

- **`ContaReceberSaldoCalculator` como calculadora estatica separada do Service**, espelhando EXATAMENTE o padrao ja em producao de `FaturaSaldoCalculator` (usado por `PagamentoFaturaService`). Saldo nunca e armazenado como calculo, so o resultado final (`Status`) e persistido em `ContaReceber.Status`, igual `Fatura.Status`.
- **Perna unica do emprestimo via `ContaDestinoId` nullable**, reaproveitando a tabela `transferencia` e a regra de exclusao de gasto/receita do item 3 (`lancamento.transferencia_id != null`) sem nenhuma logica nova de exclusao. Tradeoff aceito: a coluna perde a garantia de not-null no banco; compensado por validacao de Service em cada fluxo que cria `Transferencia` (ver risco de regressao na TASK-002).
- **`ContaReceberService` usa parametros primitivos, nao Request DTOs**, seguindo a mesma convencao ja estabelecida em `AtivoService` (`RegistrarCompra(Guid contaId, string ticker, ...)`) — DTOs ficam so na borda do Controller.
- **Recebimento usa FK direta `Lancamento.ContaReceberId` (1:N)**, sem tabela de juncao, exatamente como o schema.dbml ja modela e no MESMO padrao ja usado por `fatura_id`/`conta_fixa_id` em `Lancamento`.
- **Overpayment e REJEITADO** (confirmado pelo usuario): `RegistrarRecebimento` com valor maior que o `saldo_pendente` atual lanca `ValorRecebimentoExcedeSaldoPendenteException` (422), sem criar lancamento nem alterar estado. `saldo_pendente` nunca fica negativo — `RECEBIDO` e sempre `saldo_pendente == 0` (ver regra-de-negocio.md item 13, "Estados").
- **TASK-010 (total a receber esperado no mes) implementada isolada agora** (confirmado pelo usuario), no mesmo escopo estrito que Investimentos usou pra "total investido" — nao espera um modulo de Dashboard/Projecao completo existir primeiro.

## Duvida em aberto para o usuario

1. **Projecao do mes / dashboard completo nao existe no codebase.** Nenhum
   endpoint de `saldo_projetado` (item 9) foi encontrado — nem para
   `total_recebido_no_mes`, `total_pago_no_mes` nem `total_a_pagar_no_mes`.
   Este modulo entrega so a fatia de contas a receber (TASK-010/011,
   confirmado). Um modulo "Dashboard/Projecao" completo, unificando as pecas
   de todos os modulos (lancamento, fatura, conta fixa, contas a receber),
   continua fora de escopo aqui — decisao de quando vira modulo proprio fica
   para o usuario, sem bloquear esta entrega.
2. **Validacao de existencia de `CategoriaId`** (quando informado, tanto no
   registro quanto no recebimento): a regra nao exige validacao explicita, e
   o restante do codebase (`Lancamento.CategoriaId`) tambem nao valida em
   Service — so tem FK de banco com `SetNull`. Mantive o MESMO padrao (nao
   validar em Service) por consistencia; nao e bloqueante, so registrado para
   nao virar decisao silenciosa.
