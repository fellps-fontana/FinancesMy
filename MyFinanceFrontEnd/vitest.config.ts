import path from 'node:path'
import { defineConfig } from 'vitest/config'

// Config separada do vite.config.ts de proposito: o plugin PWA nao deve rodar
// nos testes unitarios (funcoes puras, ambiente node).
export default defineConfig({
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  test: {
    environment: 'node',
    include: ['src/**/*.test.ts'],
  },
})
