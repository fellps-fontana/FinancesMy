/// <reference types="vite-plugin-pwa/client" />
import { StrictMode } from "react"
import { createRoot } from "react-dom/client"
import { registerSW } from "virtual:pwa-register"
import "@/index.css"
import App from "@/app/App"

// Service worker do shell da app (assets estaticos), configurado em
// vite.config.ts. Precacheia SO o build (JS/CSS/HTML/icones/fontes) -
// nenhuma resposta de API entra em cache, ver comentario de workbox em
// vite.config.ts e .claude/context/regra-de-negocio.md.
registerSW({ immediate: true })

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
