import { createContext, useContext } from "react"

// Alternancia de tema claro/escuro (identidade-visual.md, "Mecanismo de
// alternancia": "Estado da UI [...]: Context API no shell da app, mesmo
// padrao ja usado em features/auth/AuthContext.tsx"). O Provider (com o
// estado em si) e montado em app/AppShell.tsx - o proprio "shell da app" -
// porque este arquivo e .ts (sem JSX). Aqui ficam so o Context, os tipos e
// as funcoes puras compartilhadas entre o Provider e o hook de consumo,
// mesma divisao de features/auth/auth-context.ts + useAuth.ts.

export const THEME_STORAGE_KEY = "theme"

export type ThemePreference = "light" | "dark" | "system"
export type ResolvedTheme = "light" | "dark"

export type ThemeContextValue = {
  preference: ThemePreference
  resolvedTheme: ResolvedTheme
  setTheme: (next: ThemePreference) => void
  toggleTheme: () => void
}

export const ThemeContext = createContext<ThemeContextValue | null>(null)

export function readStoredThemePreference(): ThemePreference {
  const stored = window.localStorage.getItem(THEME_STORAGE_KEY)
  if (stored === "light" || stored === "dark" || stored === "system") {
    return stored
  }
  return "system"
}

function systemPrefersDarkTheme(): boolean {
  return window.matchMedia("(prefers-color-scheme: dark)").matches
}

export function resolveTheme(preference: ThemePreference): ResolvedTheme {
  return preference === "system" ? (systemPrefersDarkTheme() ? "dark" : "light") : preference
}

export function applyResolvedTheme(resolved: ResolvedTheme) {
  document.documentElement.classList.toggle("dark", resolved === "dark")
}

// Hook de consumo do ThemeContext - mesmo padrao de features/auth/useAuth.ts:
// le o contexto e lanca erro claro se usado fora do ThemeProvider (montado
// em AppShell.tsx, unico lugar da arvore que precisa dele hoje).
export function useTheme() {
  const context = useContext(ThemeContext)
  if (!context) {
    throw new Error("useTheme precisa ser usado dentro de um ThemeProvider")
  }
  return context
}
