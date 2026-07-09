# Tasks — Modulo Categorias (v1)

Gerado por killua a partir de `regra-de-negocio.md` item 7 e `schema.dbml`
(tabelas `categoria` e `de_para_categoria`, ja modeladas no schema, sem
nenhuma linha de codigo ainda). Decisoes em aberto foram confirmadas pelo
usuario em 2026-07-08 e ja estao registradas em `regra-de-negocio.md` item 7.

Escopo desta lista: entidades, CRUD de categoria e CRUD de de-para. NAO inclui
UI (hanzo) — fica pra depois. NAO inclui o consumo do de-para pelo modulo de
lancamento/sync (integracao de outro modulo).

Decisoes que fecham o escopo (ja no regra-de-negocio.md item 7):
- Hierarquia: no maximo 1 nivel (subcategoria nao pode ter filha).
- Subcategoria herda `tipo` do pai obrigatoriamente.
- Arquivar categoria-pai CASCATEIA para as subcategorias.
- Categoria arquivada nao pode ser usada em lancamento novo (regra pro futuro
  modulo de lancamento consumir; aqui so listamos arquivada=false por padrao).
- Nome duplicado permitido, sem constraint.
- De-para e 1:1: `categoria_pierre` tem UNIQUE constraint.

Formato: STATUS (PENDENTE | CONCLUIDA | BLOQUEADA).

---

### TASK-001 — Entidade Categoria + migration
STATUS: CONCLUIDA (build ok, migration InitialCreateCategoria gerada; nao aplicada localmente por falta de Postgres rodando, estrutura SQL conferida)
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: nenhuma
CONTEXTO A LER: regra-de-negocio.md item 7 (Categorias, incluindo decisoes de 2026-07-08); schema.dbml tabela `categoria`; stack.md secao Convencoes (uuid, idioma do dominio, DTOs)
ESCOPO: criar `Models/TipoCategoria.cs` (enum Despesa|Receita + conversion ToStorageValue/FromStorageValue, mesmo padrao de `TipoConta`), `Models/Categoria.cs` (Id, Nome, Tipo, ParentId, Parent, Subcategorias, Arquivada default false), configurar `Categoria` em `MyFinancesDbContext` (DbSet, conversion do Tipo, self-reference com `DeleteBehavior.Restrict`), gerar migration `InitialCreateCategoria`.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Models/TipoCategoria.cs (novo), MyFinances/MyFinances/Models/Categoria.cs (novo), MyFinances/MyFinances/Data/MyFinancesDbContext.cs (estender), MyFinances/MyFinances/Migrations/*InitialCreateCategoria* (novo)
NAO FAZER: nao implementar service/repository/controller aqui; nao usar `AppDbContext`; nao adicionar constraint de nome unico (decisao: nome duplicado e permitido).
RETORNO ESPERADO: model + migration aplicavel; enum com conversion validada contra os valores DESPESA/RECEITA do schema.

---

### TASK-002 — Revisao TASK-001
STATUS: CONCLUIDA (APROVADO apos 1 rodada de correcao — migration regenerada pelo EF removeu sem querer o default de banco Conta.Ativa=true, e Categoria.Arquivada nunca ganhou default=false por ser o default do CLR; corrigido com HasDefaultValue explicito + migration corretiva 20260708223212)
AGENT: style
FLUXO: Implementacao
DEPENDENCIAS: TASK-001
CONTEXTO A LER: regra-de-negocio.md item 7
ESCOPO: confirmar `Arquivada` default false, FK self-reference com `Restrict` (nunca cascade delete no banco — a cascata de arquivamento e logica de aplicacao, TASK-003, nao FK cascade), conversion do enum compativel com o schema, nenhum caminho de hard-delete introduzido.
ARQUIVOS PERMITIDOS: os do TASK-001 (leitura + veredito)
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito (APROVADO | PRECISA CORRIGIR) + tarefa de correcao se reprovado.

---

### TASK-003 — Repository + Service + Controller de Categoria (CRUD)
STATUS: PENDENTE
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-001
CONTEXTO A LER: regra-de-negocio.md item 7 (Categorias, TODAS as decisoes de 2026-07-08 — hierarquia 1 nivel, tipo herdado, cascata no arquivar); clean-code.md secao "Organizacao (.NET)"
ESCOPO: `ICategoriaRepository`/`CategoriaRepository` (Adicionar, ObterPorId com Subcategorias incluidas, Listar com filtro tipo/arquivada/parentId, Salvar); `ICategoriaService`/`CategoriaService` com:
- `Criar`: valida `parentId` (se informado) existe, nao esta arquivado, e NAO TEM parent proprio (hierarquia maxima de 1 nivel — bloquear criar subcategoria de subcategoria); se `parentId` informado, `Tipo` da nova categoria DEVE ser igual ao `Tipo` do pai (senao rejeitar).
- `Listar`: filtra `arquivada=false` por padrao; parametro explicito derruba o filtro.
- `Editar`: nome + parentId (mesmas validacoes de hierarquia/tipo do Criar); bloqueia parentId==id.
- `Arquivar`: seta `Arquivada=true` na categoria E em TODAS as subcategorias dela (cascata, decisao confirmada); nunca remove linha.
`CategoriasController` com POST/GET/PUT/PATCH `arquivar` em `/api/categorias`.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Repositories/ICategoriaRepository.cs (novo), MyFinances/MyFinances/Repositories/CategoriaRepository.cs (novo), MyFinances/MyFinances/Services/ICategoriaService.cs (novo), MyFinances/MyFinances/Services/CategoriaService.cs (novo), MyFinances/MyFinances/Controllers/CategoriasController.cs (novo), MyFinances/MyFinances/DTOs/Categoria/CriarCategoriaRequest.cs (novo), MyFinances/MyFinances/DTOs/Categoria/EditarCategoriaRequest.cs (novo), MyFinances/MyFinances/DTOs/Categoria/CategoriaResponse.cs (novo), MyFinances/MyFinances/Exceptions/CategoriaNaoEncontradaException.cs (novo)
NAO FAZER: nao implementar hard-delete em nenhuma rota; nao permitir subcategoria de subcategoria (hierarquia maxima 1 nivel); nao permitir tipo diferente do pai; nao esquecer a cascata de arquivamento pras subcategorias; nao adicionar constraint de nome unico.
RETORNO ESPERADO: contrato dos 4 endpoints (rota, verbo, body, retorno) + service testavel isoladamente.

---

### TASK-004 — Revisao TASK-003
STATUS: PENDENTE
AGENT: style
FLUXO: Implementacao
DEPENDENCIAS: TASK-003
CONTEXTO A LER: regra-de-negocio.md item 7
ESCOPO: confirmar que Arquivar nunca remove a linha e cascateia pras subcategorias; que Criar/Editar bloqueiam hierarquia > 1 nivel, tipo diferente do pai, e auto-referencia (parentId==id); que nenhuma regra nao-decidida foi silenciosamente assumida.
ARQUIVOS PERMITIDOS: os do TASK-003
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + tarefa de correcao se reprovado.

---

### TASK-005 — Testes TASK-003
STATUS: PENDENTE
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-004 (aprovado)
CONTEXTO A LER: regra-de-negocio.md item 7
ESCOPO: cobrir criar categoria raiz e subcategoria (mesmo tipo do pai); rejeicao de subcategoria com tipo diferente do pai; rejeicao de criar subcategoria de subcategoria (hierarquia > 1 nivel); listar com filtros tipo/arquivada/parentId; editar nome e parentId; rejeicao de parentId inexistente; rejeicao de parentId==id; arquivar seta Arquivada=true na categoria E em todas as subcategorias, sem remover nenhuma linha.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances.Tests/CategoriaServiceTests.cs (novo)
NAO FAZER: nao alterar service para o teste passar sem reportar bug.
RETORNO ESPERADO: testes passando; relatorio estruturado se falhar por bug de codigo.

---

### TASK-006 — Entidade DeParaCategoria + migration
STATUS: PENDENTE
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-001
CONTEXTO A LER: regra-de-negocio.md item 7 paragrafo "De-para" (incluindo decisao de 2026-07-08: vinculo 1:1); schema.dbml tabela `de_para_categoria`
ESCOPO: criar `Models/DeParaCategoria.cs` (Id, CategoriaPierre, CategoriaId, Categoria), configurar em `MyFinancesDbContext` (DbSet, FK para Categoria com `DeleteBehavior.Restrict`, INDICE UNICO em `CategoriaPierre`), gerar migration `InitialCreateDeParaCategoria`.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Models/DeParaCategoria.cs (novo), MyFinances/MyFinances/Data/MyFinancesDbContext.cs (estender), MyFinances/MyFinances/Migrations/*InitialCreateDeParaCategoria* (novo)
NAO FAZER: nao implementar service/controller aqui; nao deixar `CategoriaPierre` sem indice unico (decisao confirmada: vinculo 1:1).
RETORNO ESPERADO: model + migration aplicavel, com constraint UNIQUE em `CategoriaPierre` visivel na migration gerada.

---

### TASK-007 — Revisao TASK-006
STATUS: PENDENTE
AGENT: style
FLUXO: Implementacao
DEPENDENCIAS: TASK-006
CONTEXTO A LER: regra-de-negocio.md item 7 paragrafo "De-para"
ESCOPO: confirmar FK correta pra Categoria, `Restrict` no delete, e que a migration realmente cria indice/constraint UNIQUE em `CategoriaPierre` (nao so no modelo em memoria).
ARQUIVOS PERMITIDOS: os do TASK-006
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + tarefa de correcao se reprovado.

---

### TASK-008 — Repository + Service + Controller de DeParaCategoria (CRUD)
STATUS: PENDENTE
AGENT: levi
FLUXO: Implementacao
DEPENDENCIAS: TASK-001, TASK-003 (reaproveita ICategoriaRepository pra validar CategoriaId), TASK-006
CONTEXTO A LER: regra-de-negocio.md item 7 paragrafo "De-para"
ESCOPO: `IDeParaCategoriaRepository`/`DeParaCategoriaRepository` (Adicionar, ObterPorId, ObterPorCategoriaPierre, Listar, Remover, Salvar); `IDeParaCategoriaService`/`DeParaCategoriaService` com Criar (valida CategoriaId existente via ICategoriaRepository; rejeita se ja existe vinculo com o mesmo CategoriaPierre — retornar erro de conflito, nao deixar estourar so na constraint do banco), Listar, Editar (troca CategoriaId), Excluir (HARD DELETE — sem FK dependente na tabela, decisao registrada no desenho do killua); `DeParaCategoriasController` com POST/GET/PUT/DELETE em `/api/de-para-categorias`.
ARQUIVOS PERMITIDOS: MyFinances/MyFinances/Repositories/IDeParaCategoriaRepository.cs (novo), MyFinances/MyFinances/Repositories/DeParaCategoriaRepository.cs (novo), MyFinances/MyFinances/Services/IDeParaCategoriaService.cs (novo), MyFinances/MyFinances/Services/DeParaCategoriaService.cs (novo), MyFinances/MyFinances/Controllers/DeParaCategoriasController.cs (novo), MyFinances/MyFinances/DTOs/DeParaCategoria/CriarDeParaCategoriaRequest.cs (novo), MyFinances/MyFinances/DTOs/DeParaCategoria/EditarDeParaCategoriaRequest.cs (novo), MyFinances/MyFinances/DTOs/DeParaCategoria/DeParaCategoriaResponse.cs (novo), MyFinances/MyFinances/Exceptions/DeParaCategoriaNaoEncontradoException.cs (novo)
NAO FAZER: nao permitir CategoriaPierre duplicado (decisao: vinculo 1:1, rejeitar no service ANTES de bater na constraint do banco); nao consumir este de-para no fluxo de import/sync de lancamento (fora de escopo deste modulo).
RETORNO ESPERADO: contrato dos 4 endpoints + service testavel isoladamente.

---

### TASK-009 — Revisao TASK-008
STATUS: PENDENTE
AGENT: style
FLUXO: Implementacao
DEPENDENCIAS: TASK-008
CONTEXTO A LER: regra-de-negocio.md item 7 paragrafo "De-para"
ESCOPO: confirmar validacao de CategoriaId existente no Criar/Editar, rejeicao de CategoriaPierre duplicado tratada no service (nao so na constraint do banco, pra dar erro legivel), hard-delete restrito a esta tabela (nao vaza pra Categoria).
ARQUIVOS PERMITIDOS: os do TASK-008
NAO FAZER: nao editar codigo.
RETORNO ESPERADO: veredito + tarefa de correcao se reprovado.

---

### TASK-010 — Testes TASK-008
STATUS: PENDENTE
AGENT: mike
FLUXO: Implementacao
DEPENDENCIAS: TASK-009 (aprovado)
CONTEXTO A LER: regra-de-negocio.md item 7 paragrafo "De-para"
ESCOPO: cobrir criar vinculo; rejeicao de CategoriaId inexistente; rejeicao de CategoriaPierre duplicado; listar filtrando por CategoriaPierre; editar trocando CategoriaId; excluir remove a linha fisicamente (hard delete).
ARQUIVOS PERMITIDOS: MyFinances/MyFinances.Tests/DeParaCategoriaServiceTests.cs (novo)
NAO FAZER: nao alterar service para o teste passar sem reportar bug.
RETORNO ESPERADO: testes passando; relatorio se bug de codigo.

---

## Decisoes do usuario (confirmadas em 2026-07-08)

Ja registradas em `regra-de-negocio.md` item 7 — replicadas aqui como
referencia rapida da task queue:

1. Hierarquia maxima de 1 nivel.
2. Subcategoria herda `tipo` do pai obrigatoriamente.
3. Arquivar categoria-pai cascateia pras subcategorias.
4. Categoria arquivada nao pode ser usada em lancamento novo (contrato pro
   futuro modulo de lancamento consumir; nao implementado aqui).
5. Nome de categoria duplicado e permitido.
6. De-para e 1:1 — `categoria_pierre` com UNIQUE constraint.

Fora de escopo v1, nao tarefado: UI (hanzo), consumo do de-para pelo sync,
enforcement de "categoria arquivada nao pode em lancamento novo" (isso e do
modulo de lancamento, so o contrato fica documentado aqui).
