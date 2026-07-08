import type { FormEvent } from "react"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"

type LoginFormProps = {
  usernameOrEmail: string
  senha: string
  isSubmitting: boolean
  errorMessage: string | null
  onUsernameOrEmailChange: (value: string) => void
  onSenhaChange: (value: string) => void
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
}

// Componente burro: so exibe estado e dispara callbacks. Nenhuma chamada de
// rede ou logica de sessao vive aqui - isso fica no AuthContext/LoginPage.
//
// "Esqueci minha senha" e apenas texto estatico (nao e <a>/<button>): a v1 nao
// tem endpoint de recuperacao de senha (ver mockup 01 Login, sem contrapartida
// em regra-de-negocio.md/stack.md).
export function LoginForm({
  usernameOrEmail,
  senha,
  isSubmitting,
  errorMessage,
  onUsernameOrEmailChange,
  onSenhaChange,
  onSubmit,
}: LoginFormProps) {
  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-3.5">
      {errorMessage && (
        <Alert variant="destructive">
          <AlertDescription>{errorMessage}</AlertDescription>
        </Alert>
      )}

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="usernameOrEmail">E-mail</Label>
        <Input
          id="usernameOrEmail"
          name="usernameOrEmail"
          autoComplete="username"
          autoFocus
          required
          value={usernameOrEmail}
          onChange={(event) => onUsernameOrEmailChange(event.target.value)}
          className="h-auto rounded-lg bg-card px-3.5 py-3.5"
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="senha">Senha</Label>
        <Input
          id="senha"
          name="senha"
          type="password"
          autoComplete="current-password"
          required
          value={senha}
          onChange={(event) => onSenhaChange(event.target.value)}
          className="h-auto rounded-lg bg-card px-3.5 py-3.5"
        />
      </div>

      <div className="-mt-1 text-right">
        <span className="text-[13px] text-text-muted">Esqueci minha senha</span>
      </div>

      <Button
        type="submit"
        disabled={isSubmitting}
        className="mt-2 h-auto w-full rounded-lg bg-primary p-3.5 text-sm font-medium text-background hover:bg-primary/80"
      >
        {isSubmitting ? "Entrando..." : "Entrar"}
      </Button>
    </form>
  )
}
