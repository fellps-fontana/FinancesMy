# Stack e convencoes

## Stack

- **Backend:** .NET 10, API REST. (ambiente de dev tem apenas SDK .NET 10 instalado; EF Core 8.0.4 e Npgsql 8.0.3 confirmados compativeis com o TFM net10.0)
- **ORM:** Entity Framework Core + Npgsql.
- **Banco:** PostgreSQL.
- **Frontend:** React (TypeScript) + Vite.
- **Fonte externa:** API REST do Pierre (Open Finance) via Bearer token.
- **Cotacao (v1, sob demanda):** Brapi ou similar para acoes BR, consultada via proxy no backend (sem persistencia, sem polling — ver regra-de-negocio.md item 8).
- **Grafico (frontend):** Recharts, para o historico de cotacao do ativo (item 8).

## Integracao Pierre

- Autenticacao: header `Authorization: Bearer sk-...`.
- Base: `https://www.pierre.finance/tools/api`.
- Endpoints de leitura usados: get-transactions, get-accounts, get-balance,
  get-bills, manual-update.
- Sync via polling agendado (BackgroundService), nao tempo real.
- Dedup pela chave `pierre_txn_id`.

## Pontos de atencao do dado Pierre (ver regra-de-negocio.md)

- Sinal do `valor` nao confiavel em cartao -> usar `tipo` (DEBIT/CREDIT).
- Transacoes de mesma titularidade vem duplicadas -> excluir do calculo.
- Campo `manual_transaction` distingue origem manual de OF.
- Conciliacao por valor + janela de 1 dia.

## Convencoes

- Migrations versionadas pelo EF.
- IDs como uuid.
- Datas em UTC no banco; conversao na borda (UI).
- Idioma do dominio em portugues (nomes de tabela/campo conforme schema.dbml);
  codigo de infraestrutura pode usar ingles.
- DTOs: pasta e namespace em maiusculo — `DTOs/` e `MyFinances.DTOs`. Nunca
  `Dtos`/`MyFinances.Dtos` (divergencia ja corrigida em 3 branches em 2026-07).

## Organizacao de pastas (Backend)

Registrado apos dois incidentes de convencao nao documentada virarem bug: o
casing de `DTOs/` teve que ser corrigido em 3 branches antes de virar nota
acima, e `Domain/`/`Models/` coexistiram por acidente de merge sem que
ninguem percebesse a duplicata ate a unificacao do DbContext (2026-07). A
lista abaixo e o proposito e a regra de nomenclatura de CADA pasta de
primeiro nivel do backend — nao e arbitraria, e o registro que faltava.

- **Domain/**: entidades de dominio (classes que mapeiam pra tabela) e enums
  de dominio (ex: `StatusFatura`, `TipoLancamento`). Namespace
  `MyFinances.Domain`. Nome de classe: singular, PascalCase, batendo com a
  tabela em `schema.dbml` (a tabela e snake_case, a classe e PascalCase — ver
  Convencoes acima). `Models/` esta BANIDA: se reaparecer, e o mesmo bug
  corrigido nesta tarefa (Domain/Models coexistindo sem ninguem perceber a
  duplicata).
- **DTOs/**: contratos de entrada/saida da API (Request/Response); nunca
  expor a entity direto. Namespace `MyFinances.DTOs` (maiusculo, ja
  documentado acima). Organizar em subpasta por entidade quando houver mais
  de 2-3 DTOs da mesma entidade (padrao ja em uso: `DTOs/Conta/`,
  `DTOs/Ativo/`, `DTOs/Categoria/`).
- **Infrastructure/Configurations/**: uma classe `IEntityTypeConfiguration<T>`
  por entidade que precisa de mapeamento explicito (`ToTable`,
  `HasColumnName`, `HasConversion`, indice, default value). Nome:
  `{Entidade}Configuration.cs`. Regra: qualquer coisa alem do mapeamento
  automatico do EF ganha uma Configuration — nunca configurar inline em
  `DbContext.OnModelCreating` (foi exatamente o inline que causou Conta,
  Ativo e Categoria ficarem em PascalCase sem ninguem perceber).
- **Infrastructure/Filters/**: filtros globais de pipeline (ex:
  `GlobalExceptionFilter`). Nunca logica de dominio.
- **Repositories/**: acesso a dado por entidade — interface
  `I{Entidade}Repository` + implementacao `{Entidade}Repository`, injetando
  `MyFinancesDbContext`. UM SO DbContext no projeto: nunca criar um segundo;
  se surgir necessidade de outro "modulo" de dado, adiciona `DbSet` no mesmo
  `MyFinancesDbContext`.
- **Services/**: regra de negocio — interface `I{Entidade}Service` +
  implementacao `{Entidade}Service`. Nunca acessa `DbContext` direto, so via
  Repository (ver clean-code.md, Organizacao .NET).
- **Controllers/**: entrada HTTP. So orquestra Service + DTO, sem logica de
  negocio.
- **Exceptions/**: excecoes de dominio especificas (ex:
  `ContaNaoEncontradaException`), nunca excecao generica.
- **Migrations/**: gerado pelo EF, nunca editado a mao (excecao documentada
  caso a caso, ex: correcao de FK). UM SO historico de migration, pra UM SO
  DbContext.
- **Data/**: o proprio DbContext — so pode existir UM,
  `MyFinancesDbContext` — e seeders de dado de dev (ex: `DevUserSeeder`).

## Frontend (React)

- Vite como build. TypeScript estrito.
- Estado de servidor via camada dedicada (React Query ou hook proprio), nao
  fetch solto no componente.
- Componentes pequenos e por feature, espelhando os modulos do app.
- Identidade visual: ver `context/identidade-visual.md` quando existir.

### Estrutura de pastas (src/)

```
src/
  app/                    <- shell da aplicacao: App.tsx, main.tsx, routes.tsx,
                             ProtectedRoute.tsx, paginas de nivel raiz (Home.tsx)
  features/<modulo>/      <- um por dominio (auth, cartao, categorias, contas,
                             dashboard, investimentos, lancamentos)
  shared/                 <- reuso entre features, sem regra de negocio de
                             nenhum modulo especifico
```

Feature so com `.gitkeep` (ex: `categorias/`, `contas/`, `dashboard/`,
`lancamentos/` em 2026-07) e placeholder de modulo nao implementado, nao e
drift.

#### `features/<modulo>/`

| Arquivo/pasta      | Proposito                                             | Quando um arquivo entra aqui |
|---------------------|--------------------------------------------------------|-------------------------------|
| raiz da feature      | componente de pagina, alvo direto de uma `<Route>` em `app/routes.tsx` (ex: `ContaCartaoPage.tsx`, `ListaContasInvestimento.tsx`) | so o(s) componente(s) roteado(s). Nenhum outro componente de apresentacao vive na raiz — vai para `components/`, mesmo que so tenha um consumidor. |
| `api.ts`             | chamadas HTTP cruas da feature                          | uma funcao por endpoint, so monta request e retorna a Promise. Nao decide cache, retry, invalidacao ou quando chamar — isso e do hook que envolve. |
| `types.ts`           | tipos TypeScript da feature (request/response, DTOs de tela) | tipo usado por mais de um arquivo da feature (api, hooks, componentes) |
| `query-keys.ts`      | chaves do React Query centralizadas (ex: `investimentosKeys`) | feature tem mais de uma query/mutation que precisa invalidar cache uma da outra. Existe pra nao espalhar string magica de chave entre hooks. Feature simples sem cache cruzado (ex: `auth/`) pode nao ter. |
| `components/`        | componentes de apresentacao especificos da feature (renderizam JSX, recebem props, sem fetch direto) | qualquer componente que NAO e o componente roteado da raiz. Ex: `ContaInvestimentoCard`, `FaturaItem`, formularios, modais, itens de lista. |
| `hooks/`             | hooks de dados/estado da feature — um hook por operacao (ex: `useCriarContaInvestimento`, `useDesativarConta`, `useContasInvestimento`) | arquivo usa `useState`/`useQuery`/`useMutation`/`useEffect` ou qualquer API com ciclo de vida do React. Encapsula a chamada de `api.ts` + decisao de cache/invalidacao via `query-keys.ts`. |
| `lib/`               | funcoes puras de calculo/validacao/formatacao (ex: `calcularValorAtivo`, `validarSaldo`, `formatarMoeda`) | funcao sem estado, sem hook, sem fetch — recebe input, devolve output, testavel isolada sem render nem mock de rede. Se a funcao chama `useState` ou faz fetch, NAO e `lib/`, e `hooks/`. |

**Criterio objetivo lib/ vs hooks/ vs components/:**
1. Renderiza JSX? -> `components/` (ou raiz, se for o componente roteado).
2. Nao renderiza, mas usa `useState`/`useQuery`/`useMutation`/`useEffect`/
   qualquer hook do React? -> `hooks/`.
3. Nao renderiza e nao tem ciclo de vida React (funcao pura: mesmo input,
   mesmo output, sem efeito colateral)? -> `lib/`.

Nao existe categoria hibrida. Um arquivo que mistura fetch com JSX viola a
separacao de `clean-code.md` ("Organizacao (React)") e deve ser quebrado.

#### `shared/`

| Pasta        | Proposito                                              | Regra de entrada |
|--------------|----------------------------------------------------------|-------------------|
| `api/`       | client HTTP generico e sessao (`client.ts`, `session.ts`) | infraestrutura de rede que nenhuma feature especifica dona — usado por todas via `api.ts` de cada feature |
| `hooks/`     | hooks reutilizaveis entre 2+ features                    | mesmo hook seria duplicado em mais de uma feature se ficasse local |
| `lib/`       | utils genericos sem regra de negocio de dominio (ex: `utils.ts`) | funcao pura de proposito geral (ex: merge de classes), nao calculo de regra de negocio de um modulo |
| `types/`     | tipos compartilhados entre features (ex: `user.ts`)       | tipo usado por 2+ features (ex: usuario autenticado) |
| `ui/`        | design system puro: `button`, `card`, `input`, `label`, `alert` | SO estilo/comportamento generico de UI. Sem fetch, sem regra de negocio, sem import de tipo de feature especifica. Se o componente calcula algo de dominio ou busca dado, ele NAO pertence aqui — pertence a `features/<modulo>/components/`. |

### Excecoes conhecidas (nao copiar como padrao)

- `features/auth/` nao tem `lib/` nem `query-keys.ts` — feature simples, sem
  cache cruzado entre queries. Tem `AuthContext.tsx` na raiz (Context API,
  nao hook) porque estado de sessao e global à app, nao uma query pontual.
  Padrao valido so pra esse caso; nao aplicar Context API a outra feature sem
  justificativa equivalente.
