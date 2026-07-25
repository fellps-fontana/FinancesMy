# Módulo: Usuário / Autenticação

## Visão geral

Cadastro e login do único usuário da aplicação. O app é **single-user, sem
multi-tenancy** — nenhuma outra tabela do domínio tem FK `usuario_id`; o
JWT existe só para trancar a API atrás de login, não para segregar dados
entre usuários. Módulo puramente de infraestrutura/segurança, sem regra de
negócio financeira: não tem item numerado próprio em
`regra-de-negocio.md` (é pré-requisito técnico do resto do sistema, não uma
regra de domínio).

## Regras implementadas

Não há regra de negócio de domínio aqui — as decisões abaixo são técnicas,
fixadas quando o módulo foi criado (predata o padrão de `demands.md`/
`tasks.md`; sem PR numerado rastreável):

- **Hash de senha:** `Microsoft.AspNetCore.Identity.PasswordHasher<Usuario>`
  (PBKDF2-HMAC-SHA256, nativo do SDK, sem dependência extra).
- **Login por username OU email** — `AuthService.BuscarUsuarioPorUsernameOuEmailAsync`
  aceita qualquer um dos dois no mesmo campo.
- **401 genérico em falha de login** — nunca revela se o erro foi usuário
  inexistente ou senha errada (`"Credenciais invalidas."` sempre).
- **Validação de entrada:** username ≥ 3 caracteres, senha ≥ 8, email
  validado via `System.Net.Mail.MailAddress`. Duplicidade de
  username/email checada antes do insert e novamente pela unique constraint
  do banco (`DbUpdateException` com `PostgresErrorCodes.UniqueViolation`
  tratado como 409 — cobre a race condition entre o check e o insert).
- **Registro não retorna token** — cadastro e login são atos separados; não
  há auto-login pós-registro.
- **Sem refresh token na v1** — o JWT expira (padrão 8h, configurável) e o
  usuário loga de novo. Logout é só descarte do token no client (JWT
  stateless, sem blacklist).
- **`[Authorize]` é o padrão global**, com `[AllowAnonymous]` explícito só
  em `POST /auth/registrar` e `POST /auth/login`.

## Modelo de dados e endpoints

`Usuario` (`Id`, `Username`, `Email`, `SenhaHash`, `CriadoEm`) — não consta
em `context/schema.dbml` (schema é focado no domínio financeiro; usuário é
tratado como infra da aplicação, não entidade de negócio).

Endpoints (`AuthController`, ambos `[AllowAnonymous]`):
- `POST /api/auth/registrar` — 201 com `UsuarioResponse`, 400 (validação),
  409 (username/email já em uso).
- `POST /api/auth/login` — 200 com `LoginResponse` (`token` + `usuario`),
  400 (validação), 401 (credenciais inválidas).

JWT (`JwtTokenService`): claims `sub` (id), `unique_name` (username), `jti`;
assinado HMAC-SHA256, chave/issuer/audience/expiração lidos de
`appsettings.json` seção `Jwt` (a chave exige mínimo 32 bytes UTF-8 — o
serviço falha no boot se a config estiver ausente ou fraca).

Frontend em `MyFinanceFrontEnd/src/features/auth/`: `AuthContext`/`useAuth`
guardam token + usuário em `localStorage` (`myfinances_token`,
`myfinances_user`, ver `shared/api/session.ts`); `LoginPage`/`LoginForm`
para login; `ProtectedRoute` (`app/ProtectedRoute.tsx`) redireciona pra
login quando não autenticado, e o client HTTP força logout+redirect em
qualquer resposta 401 (token expirado ou inválido).

## Lacunas conhecidas

- **Sem tela de registro no frontend.** `POST /auth/registrar` só é
  acessível via API direta (Swagger/curl) — coerente com o app ser
  single-user (o único cadastro acontece uma vez, na configuração inicial),
  mas é uma lacuna real se algum dia precisar recriar o usuário sem acesso
  ao backend.
- **Sem refresh token / "lembrar-me"** — sessão sempre expira em ~8h fixas,
  decisão assumida na v1 e nunca revisada.
- **Sem rota de troca de senha ou recuperação de senha** — não existe
  endpoint para isso hoje.
- **Histórico de execução não rastreável em detalhe:** módulo foi
  construído antes do padrão `demands.md`/`tasks.md`/ciclo `killua → mike →
  levi → style` descrito no `CLAUDE.md` global. As 10 tasks originais (ver
  `.claude/worktrees/modulo-usuario/.claude/tasks.md`, preservado como
  histórico) foram todas atribuídas a `levi`, sem registro de rodada de
  `style` ou `mike` documentada separadamente — não há achados de revisão
  para citar aqui.

## O que foi entregue

Sem PR numerado ou registro de agent-por-agent rastreável (módulo anterior
ao padrão de tracking atual). O que existe hoje em `main`: entidade +
migration, hash de senha, geração/validação de JWT, endpoints de
registro/login, middleware de autenticação global, e o client React com
persistência de sessão e rota protegida — cobertura funcional completa do
fluxo de login único, sem lacuna de regra de negócio pendente.
