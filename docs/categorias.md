# Modulo Categorias (v1)

Gerencia as categorias do usuario (DESPESA|RECEITA, hierarquia de 1 nivel) e o
vinculo de-para entre a string de categoria vinda do Pierre e a categoria
propria do usuario. Nao inclui UI nem o consumo do de-para pelo fluxo de
lancamento/sync — isso fica pra outro modulo.

## Regras de negocio implementadas

Ver `context/regra-de-negocio.md` item 7 (secao "Categorias" e decisoes
datadas de 2026-07-08). Resumo:

- Categoria tem `tipo` (DESPESA|RECEITA) e hierarquia de no MAXIMO 1 nivel
  (subcategoria nao pode ter filha propria).
- Subcategoria herda o `tipo` do pai obrigatoriamente.
- Categoria e SEMPRE soft-delete (`Arquivada=true`), nunca hard-delete.
  Arquivar uma categoria-pai cascateia pras subcategorias.
- Categoria arquivada nao pode ser usada em lancamento novo — isso e
  CONTRATO pro futuro modulo de lancamento consumir, nao enforced aqui.
- Nome de categoria duplicado e permitido.
- De-para (`categoria_pierre -> categoria_id`) e vinculo 1:1, com UNIQUE
  constraint no banco. Excluir de-para e HARD DELETE (sem FK dependente
  nessa tabela).

## Modelo de dados e endpoints

**Categoria** (`Categorias`): Id, Nome, Tipo, ParentId (self-reference,
`DeleteBehavior.Restrict`), Arquivada (default false).

**DeParaCategoria** (`DeParaCategorias`): Id, CategoriaPierre (UNIQUE),
CategoriaId (FK Restrict).

| Verbo  | Rota                              |
|--------|------------------------------------|
| POST   | `/api/categorias`                  |
| GET    | `/api/categorias?tipo=&arquivada=&parentId=` |
| PUT    | `/api/categorias/{id}`             |
| PATCH  | `/api/categorias/{id}/arquivar`    |
| POST   | `/api/de-para-categorias`          |
| GET    | `/api/de-para-categorias?categoriaPierre=` |
| PUT    | `/api/de-para-categorias/{id}`     |
| DELETE | `/api/de-para-categorias/{id}`     |

## O que cada agent entregou

- **killua**: modelou as duas entidades a partir do `schema.dbml` ja
  existente, quebrou em 10 tasks, e levantou 6 perguntas de regra que nao
  estavam decididas (hierarquia, tipo herdado, cascata, de-para 1:1, nome
  duplicado, uso pos-arquivar) — todas confirmadas pelo usuario antes da
  execucao e registradas em `regra-de-negocio.md`.
- **levi**: implementou as 2 entidades + migrations e os 2 CRUDs completos
  (Categoria, DeParaCategoria), em 5 rodadas de codigo + 3 correcoes.
- **style**: 4 ciclos de revisao, 3 achados reais corrigidos —
  (1) migration da TASK-001 removeu sem querer o default de banco
  `Conta.Ativa=true` (regressao em modulo alheio) e nao aplicou o default
  `Categoria.Arquivada=false` exigido pelo schema; (2) `Editar` de categoria
  nao validava se a propria categoria ja tinha subcategorias, permitindo
  criar hierarquia de 2 niveis por edicao; (3) tabela/DbSet de
  `DeParaCategoria` nomeados em ingles (`DeParaCategories`), quebrando a
  convencao em portugues do resto do dominio. TASK-009 (CRUD de-para)
  aprovada de primeira, com uma observacao nao bloqueante registrada
  (race condition rara de duplicata concorrente cai em 500 cru).
- **mike**: 37 testes novos (25 em `CategoriaServiceTests`, 12 em
  `DeParaCategoriaServiceTests`), 141/141 testes passando na solution
  inteira ao final. Nenhum bug de codigo encontrado nos testes.

## Lacunas conhecidas / pendencias

- UI (hanzo) nao entra neste modulo — telas de categoria e de-para ficam
  pra outra task.
- O modulo de lancamento (quando implementado/estendido) precisa: (a)
  rejeitar `categoria_id` de categoria arquivada na criacao de lancamento
  novo; (b) consumir a tabela de-para no fluxo de import/sync — nenhum dos
  dois foi implementado aqui, so o contrato ficou documentado em
  `regra-de-negocio.md` item 7.
- Race condition rara: duas requisicoes simultaneas criando o mesmo
  `CategoriaPierre` podem ambas passar na validacao do service e uma delas
  estourar a constraint UNIQUE do banco como erro 500 cru (nao capturado
  no controller). Registrado pelo `style` na TASK-009, nao bloqueante,
  nao corrigido nesta v1.
