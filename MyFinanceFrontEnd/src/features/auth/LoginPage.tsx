import { useEffect, useState, type FormEvent } from "react"
import { useLocation, useNavigate } from "react-router-dom"
import { Wallet } from "lucide-react"
import { useAuth } from "@/features/auth/useAuth"
import { LoginForm } from "@/features/auth/components/LoginForm"
import { RegisterForm } from "@/features/auth/components/RegisterForm"
import { LoginBrandPanel } from "@/features/auth/components/LoginBrandPanel"
import { registrar } from "@/features/auth/api"
import { ApiError } from "@/shared/api/client"

type LoginPageProps = {
  initialMode?: "login" | "register"
}

// Container: guarda estado de UI (campos do formulario de login e registro) e
// liga o submit ao AuthContext e a API de registro.
//
// Layout segue mockup 01 Login (dois frames responsivos da mesma tela):
// abaixo de lg, coluna unica centralizada (frame MOBILE); a partir de lg,
// split-screen com o painel de marca a esquerda (frame DESKTOP).
export function LoginPage({ initialMode = "login" }: LoginPageProps) {
  const { login, isLoggingIn, loginError } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  const isRegisterPath = location.pathname === "/registrar"
  const [mode, setMode] = useState<"login" | "register">(() =>
    isRegisterPath ? "register" : initialMode,
  )

  // Estados do formulario de login
  const [usernameOrEmail, setUsernameOrEmail] = useState("")
  const [senhaLogin, setSenhaLogin] = useState("")

  // Estados do formulario de criacao de conta
  const [username, setUsername] = useState("")
  const [email, setEmail] = useState("")
  const [senhaRegistro, setSenhaRegistro] = useState("")
  const [isRegistering, setIsRegistering] = useState(false)
  const [registerError, setRegisterError] = useState<string | null>(null)

  useEffect(() => {
    if (location.pathname === "/registrar") {
      setMode("register")
    } else if (location.pathname === "/login") {
      setMode("login")
    }
  }, [location.pathname])

  async function handleLoginSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    try {
      await login({ usernameOrEmail, senha: senhaLogin })
      navigate("/", { replace: true })
    } catch {
      // erro ja fica exposto via loginError e renderizado no LoginForm
    }
  }

  async function handleRegisterSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsRegistering(true)
    setRegisterError(null)

    try {
      await registrar({ username, email, senha: senhaRegistro })
      try {
        await login({ usernameOrEmail: username, senha: senhaRegistro })
        navigate("/", { replace: true })
      } catch {
        setUsernameOrEmail(username)
        setMode("login")
        navigate("/login", { replace: true })
      }
    } catch (error) {
      const message =
        error instanceof ApiError ? error.message : "Nao foi possivel criar a conta. Tente novamente."
      setRegisterError(message)
    } finally {
      setIsRegistering(false)
    }
  }

  function handleSwitchMode(targetMode: "login" | "register") {
    setRegisterError(null)
    setMode(targetMode)
    navigate(targetMode === "register" ? "/registrar" : "/login")
  }

  const isRegister = mode === "register"

  return (
    <div className="flex min-h-svh flex-col bg-background lg:flex-row">
      <LoginBrandPanel />

      <div className="flex flex-1 flex-col items-center justify-center gap-10 px-6 py-16 lg:px-16">
        {/* cabecalho do frame MOBILE: no desktop o LoginBrandPanel cumpre esse papel */}
        <div className="flex flex-col items-center gap-4 text-center lg:hidden">
          <div className="flex size-14 items-center justify-center rounded-xl bg-accent-deep">
            <Wallet className="size-6 text-accent-soft" strokeWidth={1.6} />
          </div>
          <div>
            <h1 className="text-[19px] font-medium text-text-primary">Financeiro</h1>
            <p className="mt-1 text-[13px] text-text-muted">
              {isRegister ? "Crie sua conta para começar" : "Entre para continuar"}
            </p>
          </div>
        </div>

        <div className="flex w-full max-w-[360px] flex-col gap-4">
          {/* titulo do frame DESKTOP: no mobile o cabecalho acima ja cumpre esse papel */}
          <div className="hidden lg:block">
            <h1 className="text-[19px] font-medium text-text-primary">
              {isRegister ? "Criar conta" : "Entrar"}
            </h1>
            <p className="mt-1 text-[13px] text-text-muted">
              {isRegister ? "Preencha seus dados para começar" : "Acesse sua conta"}
            </p>
          </div>

          {isRegister ? (
            <RegisterForm
              username={username}
              email={email}
              senha={senhaRegistro}
              isSubmitting={isRegistering || isLoggingIn}
              errorMessage={registerError}
              onUsernameChange={setUsername}
              onEmailChange={setEmail}
              onSenhaChange={setSenhaRegistro}
              onSubmit={handleRegisterSubmit}
            />
          ) : (
            <LoginForm
              usernameOrEmail={usernameOrEmail}
              senha={senhaLogin}
              isSubmitting={isLoggingIn}
              errorMessage={loginError}
              onUsernameOrEmailChange={setUsernameOrEmail}
              onSenhaChange={setSenhaLogin}
              onSubmit={handleLoginSubmit}
            />
          )}

          {/* divisor + login alternativo: so existem no modo login do frame MOBILE */}
          {!isRegister && (
            <>
              <div className="flex items-center gap-2.5 lg:hidden">
                <div className="h-px flex-1 bg-border" />
                <span className="text-xs text-text-faint">ou</span>
                <div className="h-px flex-1 bg-border" />
              </div>

              <button
                type="button"
                aria-disabled="true"
                className="rounded-lg border border-border bg-card p-3.5 text-sm font-medium text-text-body lg:hidden"
              >
                Entrar com código de acesso
              </button>
            </>
          )}

          {/* Alternancia entre login e criacao de conta */}
          <div className="text-center text-[13px]">
            <span className="text-text-muted">
              {isRegister ? "Já tem conta? " : "Não tem conta? "}
            </span>
            <button
              type="button"
              onClick={() => handleSwitchMode(isRegister ? "login" : "register")}
              className="cursor-pointer font-medium text-accent-soft hover:underline"
            >
              {isRegister ? "Entrar" : "Criar conta"}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
