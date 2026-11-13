import { expect, test, type Page } from '@playwright/test'

/**
 * Filtro por período no fuso do NAVEGADOR (RN14).
 *
 * Fatos do seed determinístico (conferidos via MCP do SQL Server), em torno de 10/03/2025:
 *   - 00:00–03:00 UTC do dia 10 → em São Paulo (UTC-03:00) ainda é dia 09;
 *   - 00:00–03:00 UTC do dia 11 → em São Paulo ainda é dia 10.
 * O total do dia é 31 nos dois fusos (saem 4, entram 4): contar linhas NÃO detecta o bug,
 * por isso os testes conferem QUAIS pedidos aparecem e a data exibida em cada linha.
 */
const BORDA = {
  utcDia10MasSpDia09: ['#14864', '#4725', '#17114', '#6975'],
  utcDia11MasSpDia10: ['#18197', '#8058', '#10308', '#169'],
}

const tabelaLinhas = (page: Page) => page.getByRole('table', { name: 'Pedidos' }).locator('tbody tr')
const paginacao = (page: Page) => page.getByRole('navigation', { name: 'Paginação' })

/** Aplica o filtro de período e devolve a URL da requisição feita à API. */
async function filtrarPeriodo(page: Page, de: string, ate: string) {
  const form = page.getByRole('form', { name: 'Filtros de pedidos' })
  await form.getByLabel('De', { exact: true }).fill(de)
  await form.getByLabel('Até', { exact: true }).fill(ate)
  const requisicao = page.waitForRequest((r) => r.url().includes('/api/pedidos?'))
  await form.getByRole('button', { name: 'Filtrar' }).click()
  const url = new URL((await requisicao).url())
  await expect(paginacao(page)).toContainText('Página 1 de')
  return url.searchParams
}

/** Percorre todas as páginas e devolve [número do pedido, texto da data] de cada linha. */
async function todasAsLinhas(page: Page) {
  const resultado: { pedido: string; data: string }[] = []
  for (;;) {
    for (const linha of await tabelaLinhas(page).all()) {
      const celulas = await linha.locator('td').allTextContents()
      resultado.push({ pedido: celulas[0], data: celulas[1] })
    }
    const proxima = paginacao(page).getByRole('button', { name: 'Próxima →' })
    if (await proxima.isDisabled()) return resultado
    const atual = await paginacao(page).locator('span').textContent()
    await proxima.click()
    await expect(paginacao(page).locator('span')).not.toHaveText(atual!)
  }
}

test.beforeEach(async ({ page }) => {
  await page.goto('/')
  await expect(tabelaLinhas(page).first()).toBeVisible()
})

test.describe('navegador em UTC', () => {
  test.use({ timezoneId: 'UTC' })

  test('um único dia: envia limites com offset +00:00 e mantém os pedidos do dia UTC', async ({ page }) => {
    const params = await filtrarPeriodo(page, '2025-03-10', '2025-03-10')

    expect(params.get('inicio')).toBe('2025-03-10T00:00:00+00:00')
    expect(params.get('fim')).toBe('2025-03-11T00:00:00+00:00')
    expect(params.has('dataInicio')).toBe(false)

    const linhas = await todasAsLinhas(page)
    const pedidos = linhas.map((l) => l.pedido)
    expect(linhas).toHaveLength(31)
    for (const p of BORDA.utcDia10MasSpDia09) expect(pedidos).toContain(p)
    for (const p of BORDA.utcDia11MasSpDia10) expect(pedidos).not.toContain(p)
    for (const l of linhas) expect(l.data).toContain('10/03/2025')
  })
})

test.describe('navegador em America/Sao_Paulo', () => {
  test.use({ timezoneId: 'America/Sao_Paulo' })

  test('um único dia: pedidos perto da meia-noite UTC caem no dia local correto', async ({ page }) => {
    const params = await filtrarPeriodo(page, '2025-03-10', '2025-03-10')

    // Meia-noite LOCAL de cada limite, com o offset daquele instante.
    expect(params.get('inicio')).toBe('2025-03-10T00:00:00-03:00')
    expect(params.get('fim')).toBe('2025-03-11T00:00:00-03:00')

    const linhas = await todasAsLinhas(page)
    const pedidos = linhas.map((l) => l.pedido)
    expect(linhas).toHaveLength(31)
    // Ex.: #169 foi feito em 11/03 02:41 UTC = 10/03 23:41 em São Paulo → TEM de aparecer.
    for (const p of BORDA.utcDia11MasSpDia10) expect(pedidos).toContain(p)
    // Ex.: #14864 foi feito em 10/03 00:16 UTC = 09/03 21:16 em São Paulo → NÃO pode aparecer.
    for (const p of BORDA.utcDia10MasSpDia09) expect(pedidos).not.toContain(p)
    // O que a tela mostra é coerente com o filtro.
    for (const l of linhas) expect(l.data).toContain('10/03/2025')
  })

  test('intervalo de vários dias: inclui o último dia local inteiro', async ({ page }) => {
    const params = await filtrarPeriodo(page, '2025-03-10', '2025-03-12')

    expect(params.get('inicio')).toBe('2025-03-10T00:00:00-03:00')
    expect(params.get('fim')).toBe('2025-03-13T00:00:00-03:00')

    const linhas = await todasAsLinhas(page)
    const pedidos = linhas.map((l) => l.pedido)
    expect(linhas).toHaveLength(92)
    for (const p of BORDA.utcDia11MasSpDia10) expect(pedidos).toContain(p)
    for (const p of BORDA.utcDia10MasSpDia09) expect(pedidos).not.toContain(p)
    for (const l of linhas) expect(l.data).toMatch(/^1[012]\/03\/2025/)
  })
})
