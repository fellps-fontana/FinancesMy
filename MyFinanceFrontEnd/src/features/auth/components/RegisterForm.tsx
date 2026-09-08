import type { FormEvent } from "react"
import { Alert, AlertDescription } from "@/shared/ui/alert"
import { Button } from "@/shared/ui/button"
import { Input } from "@/shared/ui/input"
import { Label } from "@/shared/ui/label"

type RegisterFormProps = {
  username: string
  email: string
  senha: string
  isSubmitting: boolean
  errorMessage: string | null
  onUsernameChange: (value: string) => void
  onEmailChange: (value: string) => void
  onSenhaChange: (value: string) => void
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
}

// Componente burro: so exibe estado e dispara callbacks. Nenhuma chamada de
// rede ou logica de sessao vive aqui - isso fica no LoginPage/AuthContext.
export function RegisterForm({
  username,
  email,
  senha,
  isSubmitting,
  errorMessage,
  onUsernameChange,
  onEmailChange,
  onSenhaChange,
  onSubmit,
}: RegisterFormProps) {
  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-3.5">
      {errorMessage && (
        <Alert variant="destructive">
          <AlertDescription>{errorMessage}</AlertDescription>
        </Alert>
      )}

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="username">Nome de usuário</Label>
        <Input
          id="username"
          name="username"
          autoComplete="username"
          autoFocus
          required
          minLength={3}
          value={username}
          onChange={(event) => onUsernameChange(event.target.value)}
          className="h-auto rounded-lg bg-card px-3.5 py-3.5"
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="email">E-mail</Label>
        <Input
          id="email"
          name="email"
          type="email"
          autoComplete="email"
          required
          value={email}
          onChange={(event) => onEmailChange(event.target.value)}
          className="h-auto rounded-lg bg-card px-3.5 py-3.5"
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="senha">Senha</Label>
        <Input
          id="senha"
          name="senha"
          type="password"
          autoComplete="new-password"
          required
          minLength={8}
          value={senha}
          onChange={(event) => onSenhaChange(event.target.value)}
          className="h-auto rounded-lg bg-card px-3.5 py-3.5"
        />
        <span className="text-[12px] text-text-faint">Mínimo de 8 caracteres</span>
      </div>

      <Button
        type="submit"
        disabled={isSubmitting}
        className="mt-2 h-auto w-full rounded-lg bg-primary p-3.5 text-sm font-medium text-background hover:bg-primary/80"
      >
        {isSubmitting ? "Criando conta..." : "Criar conta"}
      </Button>
    </form>
  )
}
