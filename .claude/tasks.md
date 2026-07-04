# Tasks — Modulo de Investimentos (v1)

Escopo confirmado: investimento como CONTA MANUAL (tipo INVESTIMENTO, origem
MANUAL, saldo via `saldo_manual`). Sem ativos, ticker, preco medio, cotacao ou
rentabilidade — isso e v2 e esta fora daqui (ver regra-de-negocio.md, secao
"Escopo: v1 vs v2").

Codebase e greenfield: nao ha EF Core, DbContext, entidades nem controllers
ainda. As primeiras tasks criam essa base.

---

## TASK-001 — Setup EF Core + Npgsql + DbContext

STATUS: CONCLUIDA
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: nenhuma
CONTEXTO A LER: stack.md secao "Stack" e "Convencoes"
ESCOPO: adicionar pacotes EF Core + Npgsql ao csproj, criar `MyFinancesDbContext` vazio e configurar a connection string via appsettings/variavel de ambiente.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/MyFinances.csproj`, `MyFinances/MyFinances/Program.cs`, `MyFinances/MyFinances/appsettings.json`, `MyFinances/MyFinances/appsettings.Development.json`, `MyFinances/MyFinances/Data/MyFinancesDbContext.cs` (novo)
NAO FAZER: nao versionar credencial real de banco em `appsettings.json` (so em `appsettings.Development.json`, local); nao criar nenhum DbSet ainda (fica pra TASK-002); nao remover o endpoint `weatherforecast` (isso e limpeza, nao faz parte desta task).
RETORNO ESPERADO: projeto compila, `DbContext` registrado via DI, connection string configuravel.

---

## TASK-002 — Entidade Conta + migration inicial

STATUS: CONCLUIDA
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-001
CONTEXTO A LER: schema.dbml tabela `conta`; regra-de-negocio.md secao 1 (origem OPEN_FINANCE/MANUAL) e secao 10 (saldo de conta)
ESCOPO: criar a entidade `Conta` refletindo TODOS os campos da tabela `conta` do schema.dbml (nao so os de investimento) e gerar a migration inicial.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Models/Conta.cs` (novo), `MyFinances/MyFinances/Data/MyFinancesDbContext.cs`, `MyFinances/MyFinances/Migrations/**` (gerado pelo EF)
NAO FAZER: nao criar entidade Lancamento, Transferencia, Fatura ou qualquer outra tabela do schema — so `conta`, que e a unica necessaria para este modulo. Nao implementar logica de servico aqui, so o modelo de dados.
RETORNO ESPERADO: migration aplicavel, tabela `conta` criada no Postgres com os campos e tipos do schema.dbml.

---

## TASK-003 — Repository + Service de Conta (investimento)

STATUS: CONCLUIDA (aprovada pelo style apos 1 rodada de correcao)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-002
CONTEXTO A LER: regra-de-negocio.md secao 8 (cofrinho/investimentos/acoes) e secao 10 (saldo de conta manual)
ESCOPO: criar `ContaRepository` (acesso a dados via EF Core) e `ContaService` com metodos criar, listar (filtro por `tipo=INVESTIMENTO`), atualizar `saldo_manual` e desativar (`ativa=false`), validando que `tipo=INVESTIMENTO` sempre exige `origem=MANUAL`.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Repositories/ContaRepository.cs` (novo), `MyFinances/MyFinances/Services/ContaService.cs` (novo)
NAO FAZER: nao implementar logica de conta BANCO/CARTAO (sync Pierre, saldo calculado por lancamento) — so o caminho MANUAL/INVESTIMENTO. Nao expor entity diretamente para fora da camada (isso e responsabilidade do controller/DTO na TASK-004). Nao implementar hard-delete — desativar e sempre soft (`ativa=false`), igual ao padrao de soft-delete ja usado no restante do dominio (item 4).
RETORNO ESPERADO: `ContaService` testavel isoladamente (sem depender do controller), com metodos nomeados por intencao (nao if solto).

---

## TASK-004 — Controller REST de Conta (investimento)

STATUS: CONCLUIDA (aprovada pelo style apos 1 rodada de correcao)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-003
CONTEXTO A LER: clean-code.md secao "Organizacao (.NET)" (DTOs, camadas); regra-de-negocio.md secao 8
ESCOPO: criar `ContasController` com endpoints `POST /contas`, `GET /contas?tipo=INVESTIMENTO`, `PATCH /contas/{id}/saldo`, `PATCH /contas/{id}/desativar`, usando DTOs de entrada/saida (nunca a entity).
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Controllers/ContasController.cs` (novo), `MyFinances/MyFinances/Dtos/Conta/*.cs` (novo)
NAO FAZER: nao expor `dia_fechamento`/`dia_vencimento` no DTO (campos exclusivos de CARTAO, fora de escopo). Nao colocar regra de negocio no controller — so orquestra a chamada ao Service.
RETORNO ESPERADO: contrato de API documentado (rota, verbo, body de entrada, shape de retorno) para os 4 endpoints.

---

## TASK-005 — Testes: CRUD de conta investimento e regra de saldo manual

STATUS: PENDENTE
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-004
CONTEXTO A LER: regra-de-negocio.md secao 10 (saldo de conta manual); secao 8 (cada conta e separada, sem classificacao por nome)
ESCOPO: escrever e rodar testes cobrindo: saldo da conta INVESTIMENTO e sempre `saldo_manual` (nunca calculado), criacao de multiplas contas de investimento distintas (cofrinho/XP/carteira) sem colisao, validacao que rejeita `tipo=INVESTIMENTO` com `origem=OPEN_FINANCE`, e que "desativar" so seta `ativa=false` (nao apaga linha).
ARQUIVOS PERMITIDOS: pasta de testes definida em stack.md (criar se nao existir, ex: `MyFinances/MyFinances.Tests/`)
NAO FAZER: nao alterar `ContaService`/`ContasController` para fazer o teste passar sem reportar — bug de codigo volta pro levi.
RETORNO ESPERADO: testes passando; se falhar por bug de codigo (nao de teste), relatorio estruturado apontando arquivo e linha.

---

## TASK-006 — Calculo do total investido

STATUS: PENDENTE
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-003
CONTEXTO A LER: regra-de-negocio.md secao "Escopo: v1 vs v2" (racional "ver o total investido no patrimonio"); secao 10
ESCOPO: criar funcao nomeada e testavel no `ContaService` que soma `saldo_manual` de todas as contas ativas com `tipo=INVESTIMENTO`, e expor via `GET /contas/investimentos/total` no controller.
ARQUIVOS PERMITIDOS: `MyFinances/MyFinances/Services/ContaService.cs`, `MyFinances/MyFinances/Controllers/ContasController.cs`
NAO FAZER: NAO tentar calcular "patrimonio total" do app (isso somaria tambem contas BANCO/CARTAO via Open Finance, cuja logica de saldo calculado por lancamento ainda nao existe — ver duvida em aberto no fim do arquivo). Escopo aqui e so o total das contas de investimento.
RETORNO ESPERADO: endpoint retornando `{ totalInvestido: decimal }`, funcao de calculo isolada e nomeada (ex: `CalcularTotalInvestido`).

---

## TASK-007 — Teste do calculo de total investido

STATUS: PENDENTE
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-006
CONTEXTO A LER: regra-de-negocio.md secao 10 e secao "Escopo: v1 vs v2"
ESCOPO: testar que o total soma somente contas `tipo=INVESTIMENTO` E `ativa=true`, ignora contas desativadas e contas de outros tipos, e retorna zero sem contas cadastradas.
ARQUIVOS PERMITIDOS: pasta de testes (mesma da TASK-005)
NAO FAZER: nao testar patrimonio total do app — fora de escopo aqui.
RETORNO ESPERADO: testes passando cobrindo os 3 casos citados; relatorio de bug se falhar por codigo.

---

## TASK-008 — Camada de dados no front (hook de contas de investimento)

STATUS: PENDENTE
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-004, TASK-006
CONTEXTO A LER: stack.md secao "Frontend (React)"; clean-code.md secao "Organizacao (React)"
ESCOPO: criar hook/data layer (`useContasInvestimento`) que consome `GET /contas?tipo=INVESTIMENTO` e `GET /contas/investimentos/total`, sem fetch solto em componente.
ARQUIVOS PERMITIDOS: `frontend/src/features/investimentos/api/*.ts` (novo — caminho a confirmar conforme estrutura real do front, ainda inexistente)
NAO FAZER: nao colocar chamada de fetch dentro de componente de UI (isso e TASK-009/010).
RETORNO ESPERADO: hook tipado (sem `any`) retornando lista de contas e total investido, com estado de loading/erro.

---

## TASK-009 — UI: lista de contas de investimento + total

STATUS: PENDENTE
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-008
CONTEXTO A LER: identidade-visual.md (se existir); regra-de-negocio.md secao 8
ESCOPO: tela listando as contas de investimento (nome, saldo atual) com o total investido em destaque.
ARQUIVOS PERMITIDOS: `frontend/src/features/investimentos/ListaContasInvestimento.tsx` (novo, caminho a confirmar)
NAO FAZER: nao exibir campos de CARTAO (dia_fechamento/vencimento) nem qualquer dado de ativo/ticker (v2).
RETORNO ESPERADO: componente de apresentacao consumindo o hook da TASK-008, sem logica de calculo embutida.

---

## TASK-010 — UI: criar/editar/desativar conta de investimento

STATUS: PENDENTE
AGENT: hanzo
FLUXO: Implementacao
DEPENDENCIAS: TASK-008
CONTEXTO A LER: regra-de-negocio.md secao 8 e secao 10
ESCOPO: formulario para criar conta de investimento (nome + saldo inicial), editar `saldo_manual` e acao de desativar.
ARQUIVOS PERMITIDOS: `frontend/src/features/investimentos/FormContaInvestimento.tsx` (novo, caminho a confirmar)
NAO FAZER: nao permitir escolher `origem` no form (sempre MANUAL, implicito); nao expor campo de tipo de ativo/ticker (v2).
RETORNO ESPERADO: componente de formulario chamando o hook da TASK-008 para POST/PATCH.

---

## Decisoes de modelagem (Killua)

- **Migration cria a tabela `conta` inteira**, nao so os campos de investimento. A tabela e compartilhada por BANCO/CARTAO/INVESTIMENTO no schema.dbml — criar uma versao parcial agora e recriar depois pra outros modulos seria retrabalho e schema fragmentado. Decisao que afeta outros modulos futuros (reaproveitam esta tabela, nao remigram).
- **`tipo=INVESTIMENTO` implica `origem=MANUAL` sempre** — a regra de negocio so descreve investimento como manual (cofrinho, XP, carteira); nao ha mencao a investimento via Open Finance. Virou validacao explicita na Service (TASK-003).
- **Total investido != patrimonio total do app.** A regra diz "ver o total investido no patrimonio", mas patrimonio completo somaria tambem contas BANCO/CARTAO com saldo calculado por lancamento (item 10) — e `lancamento` nao existe ainda no codebase (outro modulo). Escopo mantido estrito: so total de contas de investimento.

## Duvida em aberto para o usuario

Um "patrimonio total" de verdade (somando Open Finance + manual) depende de
modulos que ainda nao existem (conta corrente, cartao, lancamento, sync
Pierre). Vira modulo separado, ou o endpoint de total (TASK-006) ja deveria
nascer com um contrato generico (ex: `/patrimonio`) pensado para acomodar
outros tipos de conta depois? Nao assumido — mantido escopo estrito de
investimento.
