import path from 'node:path'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { VitePWA } from 'vite-plugin-pwa'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
    VitePWA({
      // Registro manual em src/app/main.tsx via `virtual:pwa-register`, nao
      // injetado automaticamente no index.html.
      injectRegister: null,
      registerType: 'autoUpdate',
      manifest: {
        name: 'MyFinances',
        short_name: 'MyFinances',
        description: 'Financas pessoais: contas manuais e Open Finance num so painel.',
        start_url: '/',
        display: 'standalone',
        // Cor de acento (roxo) da identidade visual, ver
        // .claude/context/identidade-visual.md, token "accent".
        theme_color: '#7F77DD',
        // bg-base do tema escuro (direcao visual primaria do app), usado
        // como fundo da splash screen na instalacao.
        background_color: '#0e0d13',
        icons: [
          {
            src: '/icons/icon-192.png',
            sizes: '192x192',
            type: 'image/png',
            purpose: 'any',
          },
          {
            src: '/icons/icon-512.png',
            sizes: '512x512',
            type: 'image/png',
            purpose: 'any',
          },
          {
            src: '/icons/icon-maskable-512.png',
            sizes: '512x512',
            type: 'image/png',
            purpose: 'maskable',
          },
        ],
      },
      workbox: {
        // So o shell/assets estaticos do build (JS, CSS, HTML, fontes,
        // icones) entram no precache do service worker.
        globPatterns: ['**/*.{js,css,html,svg,png,woff,woff2}'],
        // Nenhuma rota de API entra em cache de runtime: dado financeiro
        // nao pode ficar stale offline sem aviso claro ao usuario (ver
        // .claude/context/regra-de-negocio.md e clean-code.md, tratamento
        // de erro). API roda em origem propria (VITE_API_BASE_URL), fora do
        // navigateFallback do shell da SPA.
        navigateFallbackDenylist: [/^\/api\//],
        runtimeCaching: [],
      },
    }),
  ],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
})
