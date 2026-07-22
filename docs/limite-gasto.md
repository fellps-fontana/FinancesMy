# Módulo: Limite de Gasto por Categoria

## Visão geral

Alerta de orçamento mensal por categoria — nunca bloqueia nada. O usuário
define um `valor_limite` para uma categoria de tipo DESPESA (`LimiteGasto`,
1:1 com `Categoria`); o sistema compara o gasto realizado no mês contra esse
limite e sinaliza visualmente quando estoura, em três superfícies (dashboard,
tela de lançamento, relatório por categoria). A tabela `limite_gasto` já
existia no `schema.dbml` desde antes deste módulo, órfã no código — este
trabalho cobriu regra de negócio, backend e frontend do zero.

## Regras de negócio implementadas

Item 14 da `regra-de-negocio.md`, escrito neste módulo (não existia antes):

- **Só categoria DESPESA**: `Definir` rejeita categoria RECEITA ou arquivada
  com `CategoriaInvalidaParaLimiteGastoException` (422). Orçamento é conceito
  de gasto, não de entrada.
- **Gasto realizado no mês**: soma de `Lancamento` com `Tipo == Debit` e
  `Oculto == false`, filtrado por mês calendário (`Data.Year`/`Data.Month`,
  não range de 30 dias) — regime de **competência** (conta ao ser lançado,
  independente de status PENDENTE/PAGO), mesma filosofia do item 12 (compra
  de cartão conta na hora).
- **Hierarquia (1 nível só)**: se uma categoria-pai tem limite cadastrado, o
  gasto realizado dela soma também os lançamentos de suas subcategorias
  diretas — decisão do usuário que **inverteu** a suposição inicial do
  arquiteto (que era "independentes"). Não desce em subcategoria-de-
  subcategoria.
- **Estourar = só alerta visual**: `gasto_realizado > valor_limite` nunca
  bloqueia a escrita de um lançamento ou compra de cartão — o módulo somente
  calcula e informa (`LimiteGastoStatus.Estourado`).
- **Upsert em `Definir`**: chamar duas vezes para a mesma categoria atualiza
  o `ValorLimite` existente, nunca duplica (índice único em `categoria_id`
  reforça isso também no banco).
- **Período**: só MENSAL implementado nesta versão (campo pronto para
  extensão futura).
- Classificado como regra **não-crítica** (comparável a
  `CalcularTotalAReceberEsperadoNoMes`, não a `ContaReceberSaldoCalculator`):
  erro aqui produz um número errado numa tela, nunca corrompe estado nem
  bloqueia operação — por isso o módulo seguiu o fluxo simples (arquitetar →
  codar → testar → style), sem ciclo RED/GREEN formal.

## Modelo de dados e endpoints

`LimiteGasto` (entidade nova, tabela `limite_gasto` já existia no schema):
`Id`, `CategoriaId` (FK, índice único, cascade), `ValorLimite`, `Periodo`
(enum `PeriodoLimiteGasto`, só `Mensal`). `ILancamentoRepository` ganhou
`ListarPorCategoriasEPeriodo(IEnumerable<Guid>, ano, mes)` — aceita lista (não
um id só) por causa da agregação de hierarquia.

Endpoints (`LimitesGastoController`, rota `api/limites-gasto`):
- `POST /api/limites-gasto` — upsert (200 atualiza / 201 cria)
- `DELETE /api/limites-gasto/{categoriaId}` — 204
- `GET /api/limites-gasto` — lista todos os limites cadastrados
- `GET /api/limites-gasto/gasto-vs-limite?ano=&mes=` — todas as categorias
- `GET /api/limites-gasto/gasto-vs-limite/{categoriaId}?ano=&mes=` — uma só

Frontend em `MyFinanceFrontEnd/src/features/limite-gasto/` (camada de dados +
página de comparativo) e componentes espalhados por onde cada superfície
pediu: `features/dashboard/components/LimiteGastoIndicador.tsx`,
`features/lancamentos/components/AvisoLimiteGasto.tsx` (+
`lib/limiarAlertaLimite.ts`), `features/categorias/components/
CampoLimiteGasto.tsx`. Rota nova `/limites-gasto`.

## Lacunas conhecidas

- **Dashboard, tela de lançamento e tela de categoria ainda não existem como
  páginas** no frontend (`features/dashboard/`, `features/lancamentos/` e
  `features/categorias/` tinham só `.gitkeep` antes deste módulo, embora boa
  parte do backend de lançamento manual já exista). Os três componentes
  (`LimiteGastoIndicador`, `AvisoLimiteGasto`, `CampoLimiteGasto`) foram
  entregues standalone, prontos para embutir quando essas páginas forem
  arquitetadas — não fazem parte desta entrega.
- **Já existe outra tela "Relatório por categoria"** em
  `features/cartao/RelatorioCategoriaPage.tsx`, escopada só a compras de
  cartão (item 12) e com um gap pré-existente (chama endpoint que não existe
  no backend). O comparativo deste módulo (`ComparativoLimiteGastoPage`,
  rota `/limites-gasto`) é deliberadamente uma tela **separada** — soma todo
  `Debit` da categoria, não só cartão — para não empilhar decisão de
  unificação em cima de um gap que já existia antes.
- Threshold de "perto do limite" (80%, usado no dashboard e no aviso de
  lançamento) é decisão de UX isolada em cada componente consumidor, não
  contrato de API — `percentualUtilizado`/`estourado` vêm crus do backend.
- Check-then-act sem transação/lock em `Definir`/`Remover` (fragilidade
  teórica de concorrência) — padrão já existente em outros services do
  projeto, não regressão deste módulo.

## O que cada agent entregou

- **killua**: modelou a entidade 1:1 com categoria, classificou a regra como
  não-crítica (justificado contra o critério já usado no projeto:
  `ContaReceberSaldoCalculator` vs `CalcularTotalAReceberEsperadoNoMes`), e
  quebrou o módulo em 12 tasks. A suposição inicial sobre hierarquia
  ("subcategorias independentes") foi corrigida pelo usuário antes da
  implementação começar (a soma acontece no pai).
- **levi**: implementou entidade/migration, repository, service e controller.
  Ao corrigir um build quebrado por conta própria (bug pré-existente de
  `TASK-002`, `TransferenciaResponse.ContaDestinoId` não-nulável quando o
  domínio já era `Guid?`), usou `!.Value` — compilava mas quebraria em
  runtime para EMPRÉSTIMO (item 13); Kira corrigiu tornando o DTO nulável.
  Também não atribuiu `Categoria` ao criar um `LimiteGasto` novo (só no
  update), deixando `CategoriaNome` vazio na resposta 201 — corrigido na
  revisão.
- **mike**: 29 testes de calculator/service (cobertura explícita de
  hierarquia via `Moq.Verify` na lista de ids repassada ao repository) + 6
  testes HTTP end-to-end. Nenhum bug de produção encontrado nas duas rodadas.
- **hanzo**: camada de dados (types/api/hooks) e as 4 superfícies de UI, em
  paralelo (arquivos disjuntos). Confirmou explicitamente que
  `RelatorioCategoriaPage.tsx` (módulo de cartão) não foi tocado. Escolheu
  não compartilhar a constante de threshold "perto do limite" (80%) entre
  `dashboard` e `lancamentos` — cada consumidor decide seu próprio corte
  visual, para não acoplar features por um número puramente estético.
- **style**: 2 rodadas no backend. Rodada 1 reprovou por
  `LimitesGastoController` fazer `Listar()` (full scan) duas vezes só para
  decidir 200 vs 201 e para achar `CategoriaNome` — `Definir` e
  `ObterGastoVsLimite` passaram a devolver tupla com o dado já resolvido.
  Rodada 2 aprovou, confirmando 359/359 testes e nenhum problema de camada
  novo. Frontend revisado inline por Kira (sem gate formal de `style`, por
  ser consumo puro de dado já calculado no backend, sem lógica de domínio
  nova no cliente).

## Notas operacionais

Padrão recorrente neste módulo: **decisões de arquitetura que o killua marca
como suposição precisam ser confirmadas antes de virar código, não depois**.
Duas das seis suposições do desenho inicial (hierarquia soma-no-pai vs
independente; regime de competência vs caixa) foram trazidas ao usuário antes
da implementação começar, e uma delas (hierarquia) foi invertida — se tivesse
sido codada como suposição, teria exigido retrabalho em repository, service e
testes.
