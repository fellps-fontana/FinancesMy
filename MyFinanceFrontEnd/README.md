# MyFinanceFrontEnd

Frontend do app de financas pessoais (React + TypeScript + Vite), consumindo a
API REST em `MyFinances/MyFinances`.

## Como rodar

### 1. Backend

Precisa de um Postgres rodando com o banco `myfinances_dev` (ver
`MyFinances/MyFinances/appsettings.Development.json`).

```
cd MyFinances/MyFinances
dotnet run --launch-profile http
```

Sobe em `http://localhost:5146`. CORS ja liberado para `http://localhost:5173`
em Development.

### 2. Criar um usuario

Ainda nao existe tela de cadastro no front nem usuario seed no banco. Antes do
primeiro login, registre um usuario direto no endpoint:

```
curl -X POST http://localhost:5146/api/auth/registrar -H "Content-Type: application/json" -d "{\"username\":\"seu_usuario\",\"email\":\"voce@email.com\",\"senha\":\"sua_senha\"}"
```

(campos exatos em `MyFinances/MyFinances/DTOs/RegistrarUsuarioRequest.cs`)

### 3. Frontend

```
cd MyFinanceFrontEnd
copy .env.example .env
npm install
npm run dev
```

Abre em `http://localhost:5173`. `VITE_API_BASE_URL` em `.env` aponta para a
API (default `http://localhost:5146`).

## Sessao

Login via `POST /api/auth/login`, token JWT (8h, sem refresh) guardado em
`localStorage`. Expirou ou deu 401 numa chamada autenticada: logout automatico
e redirect para `/login`. Logout manual limpa token, usuario e cache do
React Query.

## Estrutura

```
src/
  app/        shell (providers, rotas, guarda de rota)
  shared/     client HTTP, UI base (shadcn), tipos e hooks compartilhados
  features/   um modulo por feature (auth, dashboard, contas, lancamentos,
              cartao, categorias), espelhando os modulos do dominio
```
