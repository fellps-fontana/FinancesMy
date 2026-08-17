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
- **Ícone (metadado de exibição):** `Categoria.Icone` (string nullable) —
  nome de um ícone de catálogo fixo (não upload/URL livre), sem nenhuma
  lógica de domínio associada. Campo puramente opcional; categorias sem
  ícone continuam válidas.

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

- **Sem tela de De-Para** — endpoints existem, mas não há UI para vincular
  `categoria_pierre` a uma categoria. Baixo impacto agora porque nenhum
  fluxo de import (Pierre ou Nubank) está em v1; vira bloqueio assim que um
  dos dois entrar.
- **Sem seed de categorias padrão** — nenhuma migration insere categorias
  default; o usuário começa com a tabela vazia e precisa criar tudo via API
  direta (Swagger/curl) até que exista uma tela.
- **Histórico de execução do CRUD/backend original não rastreável:** o
  módulo predata o padrão `demands.md`/`tasks.md` — não há task list nem PR
  numerado localizável para o CRUD inicial de Categorias/De-Para (mesma
  situação do módulo Usuário/Auth). A partir do Bloco I (TASK-137 a 140,
  ver abaixo) o histórico já é rastreável.
- **`CategoriaService.Editar` faz replace total do `Icone`** — não há
  merge parcial; um PUT que não enviar `icone` zera o campo. Hoje inofensivo
  porque `FormCategoria.tsx` é o único consumidor e sempre reenvia o valor
  atual, mas vira armadilha silenciosa se outro form/consumidor de API
  aparecer sem saber disso (achado do `style` na TASK-140, não bloqueante).
- **Catálogo de ícones (`shared/lib/catalogoIcones.ts`) foi criado
  antecipando reuso pela Conta** — a TASK-129 (ícone em Conta), que
  motivou originalmente esse catálogo, ainda não foi executada. Se ela
  nunca vier a reusar o catálogo, considerar descer o arquivo para
  `features/categorias/lib/`.

## O que foi entregue

CRUD completo de categoria com hierarquia de 1 nível e arquivamento em
cascata, CRUD completo de De-Para, tela própria de gestão de categorias no
frontend (`features/categorias/`: `CategoriasPage.tsx`, `FormCategoria.tsx`,
`CategoriaItem.tsx`, `CategoriaSelect.tsx`) e as validações de regra do
item 7 — sem registro de agent-por-agent anterior ao Bloco I (módulo
predata o tracking atual).

**Bloco I — TASK-137 a 140 (rastreado, `tasks.md`):**
- `levi` (TASK-137): campo `Categoria.Icone` propagado ponta a ponta
  (Domain, EF Configuration, migration aditiva, DTOs, Service, Controller).
  44 testes de `CategoriaService` GREEN.
- `hanzo` (TASK-139): diagnosticou e corrigiu o "botão Editar duplicado" —
  causa real era 2 pontos de entrada simultâneos de edição (item da lista
  continuava clicável com o form já aberto acima), não duplicação no DOM.
- `hanzo` (TASK-138): seletor/exibição de ícone em `FormCategoria`,
  `CategoriaItem` e `CategoriaSelect`; criou o catálogo fixo de ícones em
  `shared/lib/catalogoIcones.ts` ao descobrir que a TASK-129 (que
  supostamente já teria feito isso para Conta) nunca foi executada.
- `style` (TASK-140): **APROVADO**, sem rodada de correção. Dois achados
  não-bloqueantes registrados acima (replace total no Editar, catálogo
  antecipado em `shared/lib`).
