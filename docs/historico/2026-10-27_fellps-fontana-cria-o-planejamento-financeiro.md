# fellps-fontana/Criação-planejamento-financeiro
- Periodo: 2026-10-14 -> 2026-10-27
- Sessoes: 2026-10-14; 2026-10-22; 2026-10-23 (TASK-265 a 270 feitas com o Argon/Gemini, fora do Claude); 2026-10-26; 2026-10-27 07:41
- Fluxo: Implementacao

## Demanda
Substituir o limite de gasto avulso por categoria pelo planejamento mensal
(item 20 da regra): renda prevista, fixas, investimento, sobra e sublimites
percentuais por categoria (item 14), com tela unificada `/planejamento`,
"Copiar de outro mes", tools do MCP e cobertura de teste da previa do front.

## Tasks
Tasks 242 a 271 estao em `.claude/tasks.md` (formato antigo); a 272 no
arquivo `.kira/` da branch.

### TASK-242 — Modelagem e esqueleto do planejamento mensal
- Agent: killua
- Status final: CONCLUIDA
- Voltou da revisao: nao
- Observacao: renumerada de 171 por colisao; itens 20/14/19 mesclados na regra antes de comecar.

### TASK-243 — Migration plano_mensal e plano_mensal_sublimite
- Agent: levi
- Status final: CONCLUIDA
- Voltou da revisao: nao (revisao inline, estrutural)
- Observacao: AddPlanoMensal, nao aplicada em banco.

### TASK-244/245 — PlanejamentoMensalCalculator (RED -> GREEN)
- Agent: mike / levi
- Status final: CONCLUIDA
- Voltou da revisao: nao; 4 testes RED tinham dado inconsistente, corrigidos pelo mike sem mudar esperados
- Observacao: 79 testes.

### TASK-246/247 — PlanoMensalValidator (RED -> GREEN)
- Agent: mike / levi
- Status final: CONCLUIDA
- Voltou da revisao: nao
- Observacao: 30 testes, um por posicao da ordem de checagem.

### TASK-248/249 — PlanoMensalInicialFactory (RED -> GREEN)
- Agent: mike / levi
- Status final: CONCLUIDA
- Voltou da revisao: nao
- Observacao: 21 testes.

### TASK-250/251 — PlanejamentoMensalService e repositorio (RED -> GREEN)
- Agent: mike / levi
- Status final: CONCLUIDA
- Voltou da revisao: 1x style (ano > 9999 virava 500 via DateOnly)
- Observacao: usuario decidiu que salvar mes sem plano cria com os valores enviados; 41 testes.

### TASK-252/253 — Controller, DTOs e testes HTTP
- Agent: levi / mike
- Status final: CONCLUIDA
- Voltou da revisao: 1x style (Required implicito, nome de campo, catches duplicados); 1x gon (GET criava plano em qualquer mes futuro -> horizonte de 12 meses; teto de renda)
- Observacao: testes HTTP acharam 2 bugs de producao (arquivada 404 em vez de 422; 500 ao adicionar sublimite em plano existente).

### TASK-254/255 — Relatorio por categoria le o sublimite
- Agent: levi / mike
- Status final: CONCLUIDA
- Voltou da revisao: style achou bug anterior (relatorio descartava gasto de categoria arquivada)
- Observacao: 38 testes do relatorio.

### TASK-256 a 260, 262 — Front: dados, tela, dashboard, relatorio, cadastro, limpeza
- Agent: hanzo
- Status final: CONCLUIDA
- Voltou da revisao: 257 e 259 1x style cada (barra vazia com R$ 0, flash apos salvar, estouro recalculado no cliente); 260 1x
- Observacao: 13 mutacoes passaram a invalidar o plano do mes.

### TASK-261 — MCP: tools do plano mensal
- Agent: levi
- Status final: CONCLUIDA
- Voltou da revisao: nao
- Observacao: obter/salvar no lugar das 4 tools de limite.

### TASK-263 — Remover modulo LimiteGasto e dropar limite_gasto
- Agent: levi
- Status final: CONCLUIDA
- Voltou da revisao: nao
- Observacao: migration DropLimiteGasto, nao aplicada.

### TASK-264 — Documentacao
- Agent: kira
- Status final: CONCLUIDA
- Voltou da revisao: nao
- Observacao: substituida no encerramento por `docs/backend/planejamento-mensal.md`.

### TASK-265/266 — GastosPorCategoria no calculator e no response
- Agent: mike / levi
- Status final: CONCLUIDA
- Voltou da revisao: nao
- Observacao: 8 testes RED -> GREEN.

### TASK-267 a 270 — Front: previa live, tela unificada, barra R$ 0, copiar de outro mes
- Agent: hanzo
- Status final: CONCLUIDA
- Voltou da revisao: nao registrado (todas "style rodada 1 APROVADO")
- Observacao: a cobertura da TASK-272 achou 4 bugs em `rascunho.ts` deste bloco.

### TASK-271 — MCP: copiar plano, entrada R$ <-> %, gastosPorCategoria
- Agent: levi
- Status final: CONCLUIDA
- Voltou da revisao: 1x style (indice de categorias sobrescrevia subcategoria; conversao R$ da sub sobre a base errada)
- Observacao: estava EM ANDAMENTO sem codigo; concluida em 2026-10-26.

### TASK-272 — vitest e cobertura de rascunho.ts
- Agent: hanzo
- Status final: CONCLUIDA
- Voltou da revisao: 1x style (nomes, comentarios, assinatura de removerSublimite)
- Observacao: 78 testes; 4 bugs achados e corrigidos.

## Entrega por agent
- killua: modelagem e esqueleto do plano mensal, esqueleto de GastosPorCategoria, textos da regra (horizonte, teto de renda, arquivada intocavel no rascunho).
- levi: dominio puro (calculator, validator, factory), repositorio com corrida resolvida pelo indice unico, service, controller, remocao do LimiteGasto, 2 migrations, tools do MCP.
- hanzo: feature `planejamento-mensal` do front, tela unificada com edicao R$ <-> %, copiar de outro mes, remocao do limite-gasto, vitest.
- mike: 79 + 30 + 21 + 41 + 38 + 8 testes no back (RED -> GREEN); ajuste dos 38 do relatorio.
- style: Required implicito em record, teto de ano, estouro recalculado no cliente, barra vazia com R$ 0, indice de categorias do MCP.
- gon: abuso do GET criando plano em qualquer mes futuro (horizonte + 12) e teto da renda; vitest sem vulnerabilidade nova.

## Decisoes
- Salvar mes sem plano cria com os valores enviados — decisao do usuario (item 20).
- Horizonte de mes corrente + 12 e teto da renda — achado do gon, regra atualizada.
- Sublimite de categoria arquivada e intocavel no rascunho ate "Remover"; "Copiar de outro mes" preserva as arquivadas do destino — decisao do usuario em 2026-10-26 (itens 14 e 20).
- Teto da renda no front validado pelo texto (16 digitos inteiros) — o double nao representa o valor exato.
- Relatorio expoe `Estourado` pronto do back — front nao recalcula regra.

## Pendencias / debito tecnico
- "Remover" num pai arquivado leva junto os sublimites das subcategorias; regra omissa, aguardando o usuario.
- Migrations AddPlanoMensal e DropLimiteGasto nao aplicadas em banco persistente.
- mcp-server sem teste automatizado; `alterarRendaPrevista` sem teste no front.
- Previa do front e back podem divergir se a formula mudar de um lado so.
- 23 vulnerabilidades do npm audit pre-existentes; nenhum controller com `[Authorize]` (pre-existente).
- Contador global `.kira/task-counter` da worktree principal nao foi gravado (permissao negada); deve valer 272.
- Docs antigas fora do formato novo (`categorias.md`, `projecao-do-mes.md`, ...) e `docs/frontend/navegacao.md` ainda nao existem no formato da Secao 9.1.

## Documentacao atualizada
- docs/backend/planejamento-mensal.md — novo, consolida `docs/planejamento-mensal.md` (removido).
- docs/backend/relatorio-categoria.md — novo, consolida `docs/relatorio-categoria.md` (removido).
- docs/projecao-do-mes.md — navegacao por categoria vai para `/planejamento?categoriaId=`.
- docs/modulo-cartao-credito.md — referencia ao sublimite aponta para o doc novo.
- docs/investimentos.md — LimiteGastoService -> PlanejamentoMensalService.

## Commits
```
8ca90b3 test(front): vitest e suite de rascunho.ts do planejamento mensal
5e8e546 feat: planejamento mensal substitui limite de gasto (TASK-242 a 271)
```
(+ o commit de documentacao desta branch)

## Arquivos alterados
Base `6a91f96`, antes do commit de documentacao:
```
139 files changed, 13345 insertions(+), 3144 deletions(-)
```
Principais: `MyFinances/MyFinances/{Domain,Services,Repositories,Controllers,DTOs,Exceptions,Migrations}` (planejamento mensal entra, LimiteGasto sai),
`MyFinances/MyFinances.Tests` (+5 suites, -3 do limite), `MyFinanceFrontEnd/src/features/planejamento-mensal` (nova),
`MyFinanceFrontEnd/src/features/limite-gasto` (reduzida), `mcp-server/src/tools/planejamentoMensal.ts`, `.claude/context/*`, `.claude/tasks.md`.
Lista completa: `git diff --stat 6a91f96..HEAD`.
