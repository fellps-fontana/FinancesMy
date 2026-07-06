import { Wallet } from "lucide-react"

// Painel de marca do split-screen (mockup 01 Login, frame DESKTOP). Visivel a
// partir do breakpoint lg; no mobile o cabecalho equivalente (icone + titulo)
// fica dentro do proprio LoginPage, pois o mockup mobile nao tem esse painel.
export function LoginBrandPanel() {
  return (
    <div className="hidden w-1/2 flex-col justify-center bg-accent-deep px-20 py-16 lg:flex">
      <div className="mb-10 flex size-13 items-center justify-center rounded-xl bg-card">
        <Wallet className="size-6 text-accent-soft" strokeWidth={1.6} />
      </div>
      <h1 className="max-w-[420px] text-[28px] leading-snug font-medium text-text-primary">
        Suas finanças, num só lugar.
      </h1>
      <p className="mt-4 max-w-[380px] text-sm leading-relaxed text-accent-soft">
        Controle contas, cartões e categorias com clareza — sem enfeite, só o
        que importa.
      </p>
    </div>
  )
}
