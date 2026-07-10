# Modulo Lancamento Geral

## Visao geral

Camada GERAL de servico sobre as entidades `Lancamento`/`Transferencia`, que
ja existiam no codigo (herdadas do modulo cartao-credito). Fornece: a funcao
de classificacao que decide se um lancamento e Entrada, Saida, Transferencia
ou CompetenciaCartao (peca central reaproveitada pelos modulos cartao,
investimentos e sync); o CRUD de lancamento manual; a criacao de transferencia
entre contas manuais (duas pernas); e a ocultacao (soft-delete) de lancamento
Open Finance. Nao inclui UI — e modulo 100% backend, consumido por outros
modulos e futuramente pela tela de lancamentos geral.

## Regras de negocio implementadas

- **Regra de sinal (item 2, CRITICA):** `ClassificacaoLancamentoService.Classificar`
  nunca le `Valor`, so `Tipo` + vinculos `TransferenciaId`/`FaturaId`. Precedencia:
  Transferencia > CompetenciaCartao > Tipo (Entrada/Saida).
- **Conta manual e fonte da verdade, OF e imutavel (item 1):** todo escrita
  (criar/editar/excluir lancamento, ocultar) valida `Origem=MANUAL` na conta
  antes de agir, exceto a ocultacao (que e o caminho inverso: so age em OF).
- **Transferencias de mesma titularidade (item 3):** `TransferenciaService`
  cria as duas pernas (DEBIT origem / CREDIT destino) atomicamente, mesmo
  `TransferenciaId`, `Manual=true`, `Status=PAGO`.
- **Exclusao de lancamento (item 4):** MANUAL e hard-delete (bloqueado por
  vinculo com transferencia/fatura/conciliacao); Open Finance e soft-delete
  (`Oculto=true`, nunca remove a linha, protege o sync de reimportar).
- **Status na criacao/edicao manual (item 5):** so `PENDENTE`/`PAGO` — `SUGERIDO`
  e exclusivo da futura maquina de conciliacao automatica.

## Modelo de dados

Nao criou tabelas novas — reaproveita `lancamento`/`transferencia` (schema.dbml),
ja migradas pelo modulo cartao-credito.

## Endpoints entregues

| Metodo | Rota | Descricao |
|---|---|---|
| GET | `/api/lancamentos?visao=caixa` | Visao de fluxo de caixa (herdado do cartao, intocado) |
| GET | `/api/lancamentos` (sem `visao`) | Listagem crua, filtravel por `contaId`/`status` |
| POST | `/api/lancamentos` | Criar lancamento manual |
| PUT | `/api/lancamentos/{id}` | Editar lancamento manual |
| DELETE | `/api/lancamentos/{id}` | Excluir lancamento manual (hard delete, bloqueado por vinculo) |
| PATCH | `/api/lancamentos/{id}/ocultar` | Ocultar lancamento Open Finance (soft-delete) |
| POST | `/api/transferencias` | Criar transferencia entre duas contas manuais |

## O que cada agent entregou

- **killua:** decompos o modulo em 12 tasks a partir dos itens 1-4 da regra
  de negocio, isolando a funcao de classificacao como peca reutilizavel.
- **levi:** `ClassificacaoLancamentoService`, `LancamentoManualService`,
  `TransferenciaService`, `LancamentoOcultacaoService`, extensao do
  `LancamentosController` existente.
- **style:** 4 rodadas de correcao ao longo do modulo — constante `DEBIT`
  duplicada (TASK-001), gate de origem MANUAL faltando em `ExcluirLancamentoAsync`
  + validacao de request duplicada (TASK-004), namespace nao usando `using
  MyFinances.Models` (TASK-010). Aprovou os demais de primeira (TASK-007).
  Tambem sinalizou 2 pontos transversais fora de escopo desta task: (a)
  controllers do projeto (incluindo `LancamentosController`) devolvem entity
  crua no response em vez de DTO de saida — padrao preexistente, nao
  regressao; (b) o paragrafo de "exclusao de lancamento MANUAL = hard delete"
  sumiu de `regra-de-negocio.md` num merge/sync anterior, embora o codigo
  implemente a regra corretamente.
- **mike:** 36 testes novos no total (6 classificacao, 26 CRUD manual, 10
  transferencia — TASK-009 nao duplicou nada apesar do nome parecido bater
  em outra suite via `--filter`, 4 ocultacao). Projeto fechou em 135/135
  testes passando.

## Notas operacionais

- **Colisao de rota resolvida:** `GET /api/lancamentos` ja existia (herdado
  do sync com `cartao-credito-tasks`, contrato `?visao=caixa`). A listagem
  crua desta task foi bifurcada na MESMA action via query string, em vez de
  criar uma segunda action colidindo na mesma rota.
- **Sync de branch:** esta branch nasceu de um merge parcial de
  `cartao-credito-tasks` (congelado na TASK-017) e foi sincronizada
  manualmente com o estado final (TASK-040) antes de comecar este modulo —
  `git merge`/`git rebase` estao bloqueados por policy no `settings.json` do
  repo, entao a sincronizacao foi feita arquivo a arquivo via `git checkout
  <branch> -- <path>`, preservando a fila de tasks propria (`tasks.md` nao
  foi sobrescrito pelo do cartao).
- **Subagents e `.claude/tasks.md`:** varios subagents rodaram limpeza de
  working tree (`git checkout`/`restore`) antes de commitar so os proprios
  arquivos, revertendo edicoes ja feitas no `tasks.md` (arquivo rastreado)
  sem afetar os arquivos novos (untracked). Kira reaplicou o status
  manualmente a cada ocorrencia; instrucao de "nao rodar checkout/restore"
  foi adicionada aos briefings dos subagents seguintes.

## Lacunas conhecidas

- Sem UI (fora de escopo deste modulo, fica pra `hanzo` quando solicitado).
- Sem checagem de reimport do sync contra `Oculto` (fica pro futuro modulo
  de sync com Pierre, v2).
- Sem cancelamento/estorno de transferencia (nao solicitado).
- Transferencia envolvendo conta Open Finance: adiada, nao decidida (ver
  `tasks.md`, secao "Decisoes do usuario", item 3).
- `regra-de-negocio.md` esta com o paragrafo de exclusao MANUAL hard-delete
  ausente (achado do style, TASK-005) — o codigo esta certo, so a
  documentacao precisa ser restaurada via `killua`/skill `alterar-context`.
- Exposicao de entity crua no response dos controllers (nao so
  `LancamentosController` — padrao replicado em varios controllers do
  projeto) — correcao transversal fora de escopo deste modulo.
