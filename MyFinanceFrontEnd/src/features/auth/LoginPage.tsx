import { useState, type FormEvent } from "react"
import { useNavigate } from "react-router-dom"
import { Wallet } from "lucide-react"
import { useAuth } from "@/features/auth/useAuth"
import { LoginForm } from "@/features/auth/components/LoginForm"
import { LoginBrandPanel } from "@/features/auth/components/LoginBrandPanel"

// Container: guarda estado de UI (campos do formulario) e liga o submit ao
// AuthContext, que e quem sabe como falar com o backend e onde guardar sessao.
//
// Layout segue mockup 01 Login (dois frames responsivos da mesma tela):
// abaixo de lg, coluna unica centralizada (frame MOBILE); a partir de lg,
// split-screen com o painel de marca a esquerda (frame DESKTOP).
export function LoginPage() {
  const { login, isLoggingIn, loginError } = useAuth()
  const navigate = useNavigate()
  const [usernameOrEmail, setUsernameOrEmail] = useState("")
  const [senha, setSenha] = useState("")

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    try {
      await login({ usernameOrEmail, senha })
      navigate("/", { replace: true })
    } catch {
      // erro ja fica exposto via loginError e renderizado no LoginForm
    }
  }

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
            <p className="mt-1 text-[13px] text-text-muted">Entre para continuar</p>
          </div>
        </div>

        <div className="flex w-full max-w-[360px] flex-col gap-4">
          {/* titulo do frame DESKTOP: no mobile o cabecalho acima ja cumpre esse papel */}
          <div className="hidden lg:block">
            <h1 className="text-[19px] font-medium text-text-primary">Entrar</h1>
            <p className="mt-1 text-[13px] text-text-muted">Acesse sua conta</p>
          </div>
          <LoginForm
            usernameOrEmail={usernameOrEmail}
            senha={senha}
            isSubmitting={isLoggingIn}
            errorMessage={loginError}
            onUsernameOrEmailChange={setUsernameOrEmail}
            onSenhaChange={setSenha}
            onSubmit={handleSubmit}
          />

          {/* divisor + login alternativo: so existem no frame MOBILE do mockup */}
          <div className="flex items-center gap-2.5 lg:hidden">
            <div className="h-px flex-1 bg-border" />
            <span className="text-xs text-text-faint">ou</span>
            <div className="h-px flex-1 bg-border" />
          </div>

          {/*
            Visual identico a um botao primario, mas sem funcao: nao existe
            metodo de login por codigo de acesso no backend (ver ESCOPO da
            tarefa). aria-disabled sinaliza para leitores de tela que ainda
            nao faz nada.
          */}
          <button
            type="button"
            aria-disabled="true"
            className="rounded-lg border border-border bg-card p-3.5 text-sm font-medium text-text-body lg:hidden"
          >
            Entrar com código de acesso
          </button>

          {/* "Criar conta" e apenas texto estatico: nao ha tela de cadastro nesta etapa */}
          <div className="text-center text-[13px]">
            <span className="text-text-muted">Não tem conta? </span>
            <span className="text-accent-soft">Criar conta</span>
          </div>
        </div>
      </div>
    </div>
  )
}
