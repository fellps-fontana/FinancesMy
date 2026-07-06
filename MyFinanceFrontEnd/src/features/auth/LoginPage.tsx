import { useState, type FormEvent } from "react"
import { useNavigate } from "react-router-dom"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/shared/ui/card"
import { useAuth } from "@/features/auth/useAuth"
import { LoginForm } from "@/features/auth/components/LoginForm"

// Container: guarda estado de UI (campos do formulario) e liga o submit ao
// AuthContext, que e quem sabe como falar com o backend e onde guardar sessao.
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
    <div className="flex min-h-svh items-center justify-center bg-background px-4">
      <Card className="w-full max-w-sm">
        <CardHeader>
          <CardTitle className="text-[19px] text-text-primary">Entrar no MyFinances</CardTitle>
          <CardDescription className="text-text-body">Acesse com seu usuario ou email</CardDescription>
        </CardHeader>
        <CardContent>
          <LoginForm
            usernameOrEmail={usernameOrEmail}
            senha={senha}
            isSubmitting={isLoggingIn}
            errorMessage={loginError}
            onUsernameOrEmailChange={setUsernameOrEmail}
            onSenhaChange={setSenha}
            onSubmit={handleSubmit}
          />
        </CardContent>
      </Card>
    </div>
  )
}
