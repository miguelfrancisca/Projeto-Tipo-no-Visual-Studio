/// <reference types="vitest/config" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// O frontend sempre chama a API em `/api/...` (URL relativa).
// Em dev, o Vite repassa para a API local; no Docker, o nginx faz o mesmo papel (frontend/nginx.conf).
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: process.env.VITE_API_URL ?? 'http://localhost:5000',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api/, ''),
      },
    },
  },
  test: {
    include: ['src/**/*.test.ts'],
    // Os testes de datas trocam process.env.TZ; em "forks" isso vale para o processo real
    // (em worker threads o env é uma cópia e a troca de fuso não teria efeito).
    pool: 'forks',
  },
})
