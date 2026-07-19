# MyFinances

Aplicativo de finanças pessoais. Centraliza contas, lançamentos, cartão de
crédito e investimentos num único painel, com o usuário como fonte da
verdade dos dados (v1 opera 100% em modo manual — a integração Open Finance
via API do Pierre está prevista no schema, mas fica para a v2).

## O que o sistema faz

- **Contas manuais**: conta corrente, cofrinho, cartão de crédito e conta de
  investimento, cada uma com seu saldo (definido pelo usuário ou calculado,
  dependendo do tipo).
- **Lançamentos e categorias**: receitas e despesas categorizadas, com
  suporte a categorias/subcategorias e conta fixa recorrente.
- **Cartão de crédito**: modelo estilo Organizze — separa a compra
  (competência) do pagamento da fatura (caixa), evitando dupla contagem no
  fluxo de caixa. Fatura por ciclo, pagamento parcial/antecipado.
- **Investimentos**: conta com carteira de ativos (ticker, quantidade, preço
  médio calculado, preço atual informado pelo usuário), compra/venda, e
  gráfico de cotação histórica consultado sob demanda (Brapi).
- **Dashboard**: projeção do mês (recebido − pago − a pagar).

Regras de negócio completas em `.claude/context/regra-de-negocio.md`.

## Stack

| Camada     | Tecnologia |
|------------|------------|
| Backend    | .NET 10 (ASP.NET Core Web API) |
| ORM        | Entity Framework Core + Npgsql |
| Banco      | PostgreSQL |
| Auth       | JWT Bearer |
| Frontend   | React 19 + TypeScript + Vite |
| Estilo     | Tailwind CSS + Radix UI (shadcn) |
| Dados (front) | TanStack React Query |
| Gráficos   | Recharts |

## Estrutura do repositório

```
MyFinances/           <- solução .NET (API)
  MyFinances/          <- projeto da API (Controllers, Services, Repositories, Domain...)
  MyFinances.Tests/     <- testes
MyFinanceFrontEnd/    <- SPA React/Vite
docs/                  <- documentação viva por módulo (cartão, investimentos, ...)
.claude/context/       <- regra de negócio, stack e convenções (fonte da verdade)
```

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) (com npm)
- PostgreSQL rodando localmente (ou acessível via connection string)

## Como rodar

### 1. Banco de dados

Crie um banco PostgreSQL local. As connection strings padrão esperam:

- **Development**: `Host=localhost;Port=5432;Database=myfinances_dev;Username=postgres;Password=postgres`
  (`MyFinances/MyFinances/appsettings.Development.json`)
- **Produção/base**: `Host=localhost;Port=5432;Database=myfinances;Username=postgres;Password=postgres`
  (`MyFinances/MyFinances/appsettings.json`)

Ajuste usuário/senha/host conforme seu ambiente. Não é necessário criar as
tabelas manualmente — o passo seguinte aplica as migrations.

### 2. Backend (API)

```bash
cd MyFinances/MyFinances
dotnet restore
dotnet ef database update   # aplica as migrations no banco
dotnet run
```

A API sobe por padrão em `http://localhost:5146` (ver
`Properties/launchSettings.json` para a porta exata). Em ambiente de
desenvolvimento, um usuário de teste é semeado automaticamente
(`DevUserSeeder`) e o Swagger/OpenAPI fica disponível em `/openapi`.

Variáveis relevantes em `appsettings.json` / `appsettings.Development.json`:
- `ConnectionStrings:DefaultConnection` — string de conexão do PostgreSQL
- `Jwt:Key/Issuer/Audience` — configuração do token JWT (troque a chave em produção)
- `Brapi:BaseUrl` — API externa usada para cotação de ativos sob demanda

### 3. Frontend

```bash
cd MyFinanceFrontEnd
cp .env.example .env   # ajuste VITE_API_BASE_URL se a API não estiver em localhost:5146
npm install
npm run dev
```

O Vite sobe em `http://localhost:5173` — essa origem já está liberada no CORS
da API (`Program.cs`, política `FrontendDev`).

### Scripts do frontend

| Comando | Ação |
|---------|------|
| `npm run dev` | ambiente de desenvolvimento (hot reload) |
| `npm run build` | build de produção (`tsc -b` + `vite build`) |
| `npm run preview` | serve o build de produção localmente |
| `npm run lint` | lint (oxlint) |

### Rodando os testes do backend

```bash
cd MyFinances
dotnet test
```

## Autenticação

A API usa JWT Bearer; todos os endpoints exigem usuário autenticado por
padrão (`FallbackPolicy` em `Program.cs`). Use `AuthController` para
login/registro e envie o token retornado no header `Authorization: Bearer <token>`.

## Documentação por módulo

Resumo vivo de cada módulo (regras cobertas, modelo de dados, decisões) em
`docs/`:
- [`docs/modulo-cartao-credito.md`](docs/modulo-cartao-credito.md)
- [`docs/investimentos.md`](docs/investimentos.md)
