# Módulo: Conta Fixa

## Visão geral

Modela despesas recorrentes de valor conhecido (aluguel, assinatura, etc).
Uma `ContaFixa` é um molde (`descricao`, `valor`, `dia_vencimento`,
`periodicidade`, `categoria_id?`, `ativa`) vinculado a uma conta de origem.
Ela mesma não é lançamento — ao ser criada ou reativada, gera
automaticamente `Lancamento`s PENDENTE (DEBIT) para a ocorrência corrente e
a próxima (mês seguinte se `MENSAL`, mesmo mês do ano seguinte se `ANUAL`),
de forma idempotente. Não há sync/job mensal na v1 (item 11 é v2): os
únicos dois gatilhos de geração são criar e reativar.

## Regras de negócio implementadas

Item 6 da `regra-de-negocio.md`, revisado com as decisões desta entrega:

- **Horizonte de geração**: ocorrência corrente + próxima (2 ocorrências), a
  cada criação ou reativação. Decisão confirmada com o usuário — a regra
  original só sugeria isso como pendência em aberto.
- **Periodicidade (`MENSAL` padrão ou `ANUAL`)**: `ContaFixaLancamentoFactory.
  ProximaOcorrencia` soma 1 mês (`MENSAL`) ou 1 ano (`ANUAL`) à data de
  referência; o dia é reajustado pelo clamp já existente a partir do
  `DiaVencimento` da ContaFixa, não pela data intermediária. `[REVISAR]`
  ainda em aberto (não decidido nesta entrega): editar a periodicidade de
  `MENSAL` para `ANUAL` (ou vice-versa) não regenera nem limpa os
  `Lancamento`s PENDENTE já gerados sob a periodicidade antiga.
- **Idempotência obrigatória**: antes de gerar o par ano/mês, verifica se já
  existe `Lancamento` com aquele `conta_fixa_id` + mês/ano. Rodar a geração
  duas vezes para a mesma ContaFixa/mês é no-op na segunda vez
  (`IContaFixaRepository.ExisteLancamentoGerado`).
- **Clamp de dia de vencimento**: usa `DateTime.DaysInMonth` — dia 31 num mês
  de 30 dias (ou fevereiro) cai no último dia do mês, mesmo padrão já usado
  por `FaturaCicloService.CriarDataValida` no ciclo do cartão.
- **Tipo sempre DEBIT**: conta fixa é sempre despesa recorrente, nunca
  recebimento (mesma família do item 5, "contas a pagar").
- **Editar propaga só para `Status=Pendente`**: mudar valor/dia_vencimento/
  categoria atualiza os `Lancamento` já gerados que ainda não foram pagos;
  os `Status=Pago` nunca são tocados (fato histórico). `Periodicidade` é a
  exceção: só é atualizada no registro `ContaFixa` (ver `[REVISAR]` acima),
  nunca propagada aos lançamentos já gerados.
- **Categoria vinculada é opcional e mantém o vínculo mesmo se arquivada**:
  `categoria_id` continua resolvível mesmo depois de a categoria (ou
  subcategoria) ser arquivada (item 7 — arquivar não deleta o vínculo).
- **Desativar exclui só `Status=Pendente`**: hard delete dos lançamentos
  pendentes vinculados; `Pago` permanece intocado. Reativar gera os 2 meses
  do zero, respeitando a idempotência.

## Modelo de dados e endpoints

`ContaFixa` (tabela: `id`, `conta_id`, `categoria_id?`, `descricao`,
`valor`, `dia_vencimento`, `periodicidade` (default `MENSAL`, migration
aditiva — registros existentes viram `MENSAL` automaticamente), `ativa`).
`Lancamento` ganhou navegação `ContaFixa`/FK `ContaFixaId`
(`OnDelete=SetNull`) — a coluna já existia no schema, só faltava o
relacionamento no EF.

Regra crítica isolada em `ContaFixaLancamentoFactory.CriarLancamentoPendente`
(função estática pura — clamp de data + shape do lançamento, sem persistir),
consumida por `ContaFixaService.GerarLancamentosPendentes` (idempotência +
persistência), mesmo padrão de calculador estático já usado por
`ClassificacaoLancamentoService`/`ContaReceberSaldoCalculator`.

Endpoints (`ContaFixaController`):
- `POST /api/contas-fixas`, `PUT /api/contas-fixas/{id}`
- `POST /api/contas-fixas/{id}/desativar`, `POST /api/contas-fixas/{id}/reativar`
- `GET /api/contas-fixas?ativa=`, `GET /api/contas-fixas/{id}`

Frontend em `MyFinanceFrontEnd/src/features/contas-fixas/`: listagem
(mockup 09) com badge ativa/inativa, badge de periodicidade (Mensal/Anual)
e nome de categoria resolvido (inclusive se a categoria estiver arquivada —
`useCategorias` é chamado duas vezes, `arquivada=false` e `arquivada=true`,
mesclando o resultado); formulário de criar/editar com toggle de
periodicidade e `CategoriaSelect` (tipo Despesa, já que conta fixa é sempre
DEBIT) — conta de origem só no modo criar, pois `EditarContaFixaRequest`
não aceita `contaId`; ação inline de desativar (com confirmação, por ser
destrutiva) / reativar (clique direto, com aviso de que gera lançamentos
novos).

## Lacunas conhecidas

- `[REVISAR]` (item 6): editar a periodicidade de uma ContaFixa não
  regenera/limpa os `Lancamento`s PENDENTE já gerados sob a periodicidade
  antiga — deliberadamente não implementado, aguarda decisão do usuário.
- Em modo edição, o form reenvia `categoriaId`/`periodicidade` já existentes
  da ContaFixa para não apagá-los por omissão (o backend faz substituição
  total, não merge).
- Sem teste de virada de ano (dezembro→janeiro) na geração — `style` marcou
  como lacuna de cobertura não-bloqueante (a lógica depende de
  `DateOnly.AddMonths` do framework, não de código customizado).
- `useContasParaSelecao` (busca contas de origem combinando banco+investimento)
  segue morando em `features/contas-receber/hooks/` e foi reaproveitado via
  import cross-feature — candidato a promover para `shared/hooks/` numa task
  futura que toque em qualquer um dos dois módulos.
- Form de criar/editar não foi integrado em `ListaContasFixas.tsx` ainda
  (decisão deliberada, mesmo padrão que Contas a Receber deixou em aberto).

## O que cada agent entregou

### Entrega de periodicidade + categoria (TASK-107 a 114)

- **killua**: modelou o esqueleto de `PeriodicidadeContaFixa` (enum
  Mensal/Anual + `ToStorageValue`/`FromStorageValue`, mesmo padrão de
  `TipoConta`), o campo `ContaFixa.Periodicidade` e a assinatura de
  `ProximaOcorrencia`, além do plano de migration — sem lógica real, corpo
  `NotImplementedException`.
- **mike**: TDD RED→GREEN de `ProximaOcorrencia`/`GerarLancamentosPendentes`
  (12 testes novos: clamp de dia 31 em fevereiro tanto mensal quanto anual,
  virada de ano/mês, idempotência) mais 4 testes HTTP end-to-end cobrindo o
  achado do style (criar/editar com periodicidade inválida → 400, com ANUAL
  → 201/200 e persistência).
- **levi**: implementou `ProximaOcorrencia` + migration aditiva. Precisou de
  2 rodadas de correção do `style`: (1) `Periodicidade` ficou implementado
  no Domain/Factory/EF mas nunca exposto nos DTOs/Controller — impossível
  criar ContaFixa `ANUAL` via API; (2) a correção da rodada 1 colocou a
  validação de string→enum dentro do Controller (`ConverterPeriodicidade`),
  violando a separação de camadas do `stack.md` — movida para dentro do
  Service, mesmo padrão de `ContaService.ConverterTipoConta`.
- **hanzo**: `FormContaFixa` ganhou toggle Mensal/Anual + `CategoriaSelect`;
  `ListaContasFixas`/`ContaFixaItem` reconstruídos conforme mockup 09.
  Identificou, fora do escopo permitido dele, que `ContaFixaResponse.
  Periodicidade` expunha o enum de domínio cru (serializava em PascalCase,
  `"Mensal"`/`"Anual"`) em vez do padrão canônico em caixa alta já usado por
  `LancamentoResponseDto.Tipo`/`ContaReceberResponse.Tipo` — Kira corrigiu
  isso direto no backend (mudança mecânica de contrato, tratada como
  tarefa mínima) e simplificou o front removendo a conversão manual que
  existia só por causa dessa inconsistência. Precisou de 1 rodada de
  correção do `style`: `useCategorias("Despesa")` sem `arquivada` só trazia
  categorias ativas por default no backend, então uma ContaFixa vinculada a
  categoria já arquivada perdia o badge de nome silenciosamente — corrigido
  buscando `arquivada=false` e `arquivada=true` e mesclando os resultados.
- **style**: 3 rodadas de correção nesta entrega (2 backend, 1 front,
  detalhadas acima), aprovação final do bloco confirmando que nenhum
  achado ficou residual (inclusive checou que subcategoria arquivada com
  pai ativo já era coberta antes da correção, e que a correção fechou
  exatamente o buraco que faltava: a categoria-pai arquivada sumindo
  inteira da consulta). o calculador estático de geração e o
  contrato de service em tupla `(bool, T?, string?)` (consistente com o
  módulo de Lançamento Geral que este consome). Identificou 3 lacunas de
  regra que não assumiu sozinho (tipo do lançamento, propagação de edição,
  comportamento ao desativar) — todas resolvidas com o usuário antes da
  implementação. Achado de kickoff: `tasks.md` do módulo de Lançamento Geral
  (DEMANDA-001) estava marcado como pendente, mas o código já estava
  mergeado em `main` havia dias — corrigido antes de prosseguir.
- **levi**: implementou entidade/migration/repository/service/controller.
  Em duas ocasiões distintas alterou `TransferenciaResponse.ContaDestinoId`
  fora do escopo permitido, revertendo o design nullable estabelecido pelo
  módulo de Contas a Receber (o campo é `null` no fluxo de EMPRÉSTIMO) — Kira
  reverteu as duas vezes após confirmar que o build passava sem a mudança.
- **mike**: TDD completo da regra crítica (15 testes RED→GREEN) mais 8+7
  testes de integração HTTP. Uma rodada precisou de correção própria (mock de
  método inexistente em `ILancamentoRepository` e uso incorreto da API do
  Moq `It.Any` em vez de `It.IsAny`) — corrigido sem tocar em código de
  produção, mantendo o RED válido.
- **style**: aprovou a regra crítica de primeira (clamp de data, idempotência
  e propagação corretos, confirmado rodando a suite). Na revisão geral do
  backend, achou 3 problemas reais: falta de validação de `DiaVencimento`/
  `Valor` causando `ArgumentOutOfRangeException` não tratada (500 genérico
  via API pública), string mágica (`erro?.Contains("nao encontrada")`)
  decidindo status HTTP, e uso de `!` sem checar `sucesso`. Provou o bug de
  validação rodando o cenário isolado antes de reportar. Aprovou na segunda
  rodada após a correção.

## Notas operacionais

- **`TransferenciaResponse.cs` é um ponto de atrito recorrente entre
  módulos.** Já teve edição fora de escopo revertida em pelo menos 3
  ocasiões ao longo de Contas a Receber e Conta Fixa — o campo
  `ContaDestinoId` precisa ser `Guid?` (nullable) porque o fluxo de
  EMPRÉSTIMO do item 13 o deixa `null`, mas isso não é óbvio pra quem só olha
  o módulo de Lançamento Geral (onde o campo é sempre preenchido). Qualquer
  task futura que toque nesse DTO deve ler o item 13 da regra-de-negocio.md
  antes de "corrigir" a nullability.
- Processo zumbi (`MyFinances.exe`) travando o build do worktree durante a
  execução das tasks — resolvido matando o processo pelo PID; não afetou
  outros worktrees porque o lock era num `bin/` específico deste worktree.
