# Global / Infra

Modulo transversal do front-end: nao pertence a uma feature de dominio, cobre
infraestrutura compartilhada por todas as telas (tema visual, instalabilidade
como PWA, navegacao mobile).

## Bloco L — Tema, PWA, nav mobile (TASK-148 a 152)

PR: https://github.com/fellps-fontana/FinancesMy/pull/50 (mergeado)

### Tema claro/escuro

- Paleta clara documentada em `context/identidade-visual.md` ("Cores — tema
  claro", "Cores — texto (tema claro)"), no padrao "card branco flutuando"
  (bg-surface mais claro que bg-base) — convencao de mercado (shadcn/Material),
  nao espelhamento algebrico do dark.
- Cores semanticas (accent roxo, positivo, negativo, alerta) sao as MESMAS
  hex nos dois temas — cor com significado e decisao de dominio, nao de tema.
  Atencao: como texto puro sobre fundo claro o contraste de positivo/negativo/
  alerta cai abaixo de WCAG aceitavel — uso deve ser sempre em badge/chip com
  fundo, nunca texto nu.
- Mecanismo: classe `.dark` no `<html>` (ausencia = claro); script inline
  sincrono no `<head>` de `index.html` aplica a classe ANTES do primeiro
  paint (le `localStorage["theme"]`, fallback `matchMedia`) — evita flash de
  tema errado.
- Estado em runtime: `ThemeContext`/`ThemeProvider` (Context API, mesmo
  padrao de `features/auth/AuthContext.tsx`), montado dentro de
  `app/AppShell.tsx` — nao precisou tocar `App.tsx`/`main.tsx`.
- `ThemeToggle` (`app/components/ThemeToggle.tsx`) fica no `UserFooter`, ao
  lado do botao "Sair" — visivel na sidebar desktop e na folha "Mais" da
  bottom tab bar mobile (o `UserFooter` e compartilhado pelas duas).

### PWA instalavel

- `vite-plugin-pwa` configurado em `vite.config.ts`, manifest inline (fonte
  unica, sem `public/manifest.json` separado): `theme_color` = accent roxo,
  `background_color` = bg-base dark, `display: standalone`.
- Icones em `public/icons/` (192, 512, maskable-512).
- Service worker cacheia so o shell estatico (JS/CSS/HTML/icones/fontes).
  `runtimeCaching: []` e `navigateFallbackDenylist: [/^\/api\//]` — nenhuma
  resposta de API financeira e cacheada, pra nao mostrar dado desatualizado
  sem aviso.
- Registro em `src/app/main.tsx` via `virtual:pwa-register`.

### Navegacao mobile

- `app/components/BottomTabBar.tsx` substitui o antigo drawer full-screen
  (< md): barra fixa no rodape com 4 destinos de maior uso (Inicio,
  Lancamentos, Cartao, Contas) + "Mais", que abre uma folha com os 5
  restantes (Investimentos, Contas fixas, Contas a receber, Categorias,
  Limites de gasto).
- Todos os 9 destinos de `NAV_ITEMS` continuam acessiveis; `MOBILE_MORE_ITEMS`
  e derivado por filter de `NAV_ITEMS` pra nao duplicar rota/icone/rotulo.
- Sidebar desktop (>= md) nao foi alterada.

### O que cada agent entregou

- **killua**: decisao de paleta + mecanismo de alternancia (arquitetura, sem
  codigo).
- **hanzo**: 3 tasks de implementacao (ThemeToggle/tokens, PWA, bottom tab
  bar).
- **style**: 1 rodada de correcao — `useTheme.ts` inicialmente usou um
  module-store com `useSyncExternalStore` em vez de Context API, com
  justificativa de que `AppShell.tsx` estaria fora do escopo permitido; a
  premissa era falsa (`AppShell.tsx` sempre esteve liberado na task). Corrigido
  para Context API real, seguindo o padrao de `AuthContext.tsx`. Veredito
  final: aprovado.

### Pendencia conhecida (fora de escopo deste bloco)

`vite build`/`vite dev` falham por conflito de versao pre-existente entre
`recharts` e `es-toolkit` (`GraficoEntradasSaidas.tsx`, modulo dashboard) —
confirmado via `git stash` que ja ocorria antes desta leva. `tsc -b` e
`oxlint` passam limpos em todos os arquivos deste bloco.
