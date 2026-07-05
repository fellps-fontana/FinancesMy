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
    <form onSubmit={onSubmit} className="flex flex-col gap-4">
      {errorMessage && (
        <Alert variant="destructive">
          <AlertDescription>{errorMessage}</AlertDescription>
        </Alert>
      )}

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="usernameOrEmail">Usuario ou email</Label>
        <Input
          id="usernameOrEmail"
          name="usernameOrEmail"
          autoComplete="username"
          autoFocus
          required
          value={usernameOrEmail}
          onChange={(event) => onUsernameOrEmailChange(event.target.value)}
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
        />
      </div>

      <Button type="submit" disabled={isSubmitting} className="mt-2 h-9 w-full">
        {isSubmitting ? "Entrando..." : "Entrar"}
      </Button>
    </form>
  )
}
