# Módulo: Categorias (+ De-Para)

## Visão geral

Categorização de lançamentos, com dois conceitos (item 7 da
`regra-de-negocio.md`):

- **Categoria** — tabela mestre própria do usuário (não vem do Pierre),
  `tipo` DESPESA | RECEITA, com até 1 nível de subcategoria via
  auto-relacionamento (`parent_id`).
- **De-Para Categoria** — vínculo entre a string de categoria que vem do
  Open Finance/Pierre (ou de uma descrição importada) e uma categoria do
  usuário, para aplicar automaticamente a categoria certa em lançamentos
  importados.

## Regras de negócio implementadas

Item 7 da `regra-de-negocio.md`:

- Categoria tem `tipo` fixo (DESPESA ou RECEITA), definido na criação.
- Hierarquia de no máximo 1 nível: uma subcategoria não pode ter
  subcategoria própria (`CategoriaService.ValidarParent` bloqueia vincular
  a um parent que já tem `ParentId` preenchido).
- Subcategoria deve ter o **mesmo tipo** da categoria pai — criar ou editar
  com tipo divergente é rejeitado (400).
- Não é permitido vincular a uma categoria arquivada (nem como parent, nem
  editando o vínculo).
- Uma categoria não pode ser parent de si mesma.
- **Arquivar é o único "remove"** — nunca há delete físico. Arquivar uma
  categoria-pai arquiva em cascata todas as suas subcategorias.
- Editar uma categoria com subcategorias existentes não permite mudar o
  `parent_id` dela mesma (regra de proteção da hierarquia — ver
  `CategoriaService.Editar`, bloqueia se `categoria.Subcategorias.Any()`).
- **De-Para:** se existe vínculo cadastrado para a `categoria_pierre`, o
  import aplica a categoria do usuário automaticamente; se não existe, o
  lançamento fica com `categoria_id = null` e é candidato a entrar numa aba
  de "vínculo pendente" (a aba em si é lacuna de frontend, ver abaixo). Essa
  regra é o contrato pronto para quando a integração Pierre (item 11) ou o
  import de fatura Nubank entrarem — nenhum dos dois está em v1 (ver
  `regra-de-negocio.md` "Escopo: v1 vs v2").

## Modelo de dados e endpoints

`categoria`: `id`, `nome`, `tipo`, `parent_id` (self-FK, null = raiz),
`arquivada`. `de_para_categoria`: `id`, `categoria_pierre` (string livre),
`categoria_id` (FK -> categoria).

Endpoints (`CategoriasController`):
- `POST /api/categorias`, `PUT /api/categorias/{id}`
- `PATCH /api/categorias/{id}/arquivar`
- `GET /api/categorias?tipo=&arquivada=&parentId=`

Endpoints (`DeParaCategoriasController`):
- `POST /api/de-para-categorias`, `PUT /api/de-para-categorias/{id}`,
  `DELETE /api/de-para-categorias/{id}`
- `GET /api/de-para-categorias?categoriaPierre=`

## Lacunas conhecidas

- **Sem tela própria de CRUD de categorias no frontend.**
  `MyFinanceFrontEnd/src/features/categorias/` tem só um `.gitkeep` e um
  componente (`CampoLimiteGasto.tsx`) reaproveitado pelo módulo de Limite
  de Gasto — não há listagem, criação, edição ou arquivamento de categoria
  pela UI. Mesma lacuna já registrada em Conta Fixa e Contas a Receber
  ("`features/categorias/` ainda é placeholder"), e a causa raiz de todas
  elas: sem tela de categoria, os formulários de outros módulos que
  referenciam `categoriaId` não têm de onde selecionar.
- **Sem tela de De-Para** — endpoints existem, mas não há UI para vincular
  `categoria_pierre` a uma categoria. Baixo impacto agora porque nenhum
  fluxo de import (Pierre ou Nubank) está em v1; vira bloqueio assim que um
  dos dois entrar.
- **Sem seed de categorias padrão** — nenhuma migration insere categorias
  default; o usuário começa com a tabela vazia e precisa criar tudo via API
  direta (Swagger/curl) até que exista uma tela.
- **Histórico de execução não rastreável:** módulo predata o padrão
  `demands.md`/`tasks.md`. Não há task list nem PR numerado localizável
  para o Categorias/De-Para — mesma situação do módulo Usuário/Auth.

## O que foi entregue

Sem registro de agent-por-agent (módulo anterior ao tracking atual). O que
existe hoje em `main`: CRUD completo de categoria com hierarquia de 1
nível e arquivamento em cascata, CRUD completo de De-Para, e as validações
de regra do item 7 — cobertura de backend completa; a lacuna real é 100%
de frontend (nenhuma tela).
