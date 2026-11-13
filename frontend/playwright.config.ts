import { defineConfig, devices } from '@playwright/test'

/**
 * Testes E2E contra a stack completa do docker compose (web → nginx → api → SQL Server).
 * Pré-requisito: `docker compose up -d --build` na raiz do repositório.
 * Outra URL: E2E_BASE_URL=http://localhost:5173 npx playwright test (Vite + API local).
 */
export default defineConfig({
  testDir: './e2e',
  timeout: 30_000,
  expect: { timeout: 10_000 },
  fullyParallel: false, // os testes compartilham o banco do compose
  workers: 1,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? [['list'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:3000',
    // Fuso padrão fixo para as asserções não dependerem da máquina (local ou CI).
    // O comportamento em outros fusos é testado explicitamente em e2e/fuso-horario.spec.ts.
    timezoneId: 'UTC',
    locale: 'pt-BR',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
})
