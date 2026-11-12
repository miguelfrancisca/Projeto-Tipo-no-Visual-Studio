import { expect, test, type APIRequestContext, type Locator, type Page } from '@playwright/test'

/**
 * Testes E2E contra a stack REAL do docker compose (nginx → API .NET → SQL Server com o seed).
 * Nenhum mock: as requisições vão até o banco.
 *
 * Reprodutibilidade
 * - O seed (database/init/02-seed.sql) é determinístico: mesmos dados em toda execução.
 * - Filtros e paginação são verificados num período PASSADO (março/2025). Pedidos criados
 *   (por estes testes ou à mão) sempre recebem a data atual do servidor (RN08), então esses
 *   números nunca mudam, por mais vezes que a suíte rode.
 * - O teste de criação lê clientes/produtos da API e compara estoque antes/depois, sem
 *   depender de valores fixos que ele mesmo altera.
 */

// Fatos do seed determinístico (conferidos via MCP do SQL Server).
const SEED = {
  clienteSemHomonimo: { id: 50, nome: 'João Oliveira', pedidosEm2025: 58 },
  dia: { data: '2025-03-10', exibida: '10/03/2025', pedidos: 31, primeiroPedidoId: 5808 },
  tamanhoPagina: 10,
}

interface Produto {
  id: number
  nome: string
  preco: number
  estoque: number
}

const moeda = (valor: number) => new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(valor)

const tabela = (page: Page) => page.getByRole('table', { name: 'Pedidos' })
const linhas = (page: Page) => tabela(page).locator('tbody tr')
const paginacao = (page: Page) => page.getByRole('navigation', { name: 'Paginação' })
const filtros = (page: Page) => page.getByRole('form', { name: 'Filtros de pedidos' })

async function filtrar(page: Page, valores: { clienteId?: number; de?: string; ate?: string }) {
  const form = filtros(page)
  if (valores.clienteId) await form.getByLabel('Cliente').selectOption(String(valores.clienteId))
  if (valores.de) await form.getByLabel('De', { exact: true }).fill(valores.de)
  if (valores.ate) await form.getByLabel('Até', { exact: true }).fill(valores.ate)
  await form.getByRole('button', { name: 'Filtrar' }).click()
}

async function todasContem(locator: Locator, texto: string) {
  for (const linha of await locator.all()) await expect(linha).toContainText(texto)
}

async function produtos(request: APIRequestContext) {
  return (await (await request.get('/api/produtos')).json()) as Produto[]
}

async function abrirModal(page: Page) {
  await page.getByRole('button', { name: '+ Novo pedido' }).click()
  const modal = page.getByRole('dialog', { name: 'Novo pedido' })
  await expect(modal).toBeVisible()
  return modal
}

test.beforeEach(async ({ page }) => {
  await page.goto('/')
  await expect(tabela(page)).toBeVisible()
})

// 1 ──────────────────────────────────────────────────────────────────────────
test('1. abre a aplicação e exibe a listagem de pedidos', async ({ page }) => {
  await expect(page.getByRole('heading', { name: 'Pedidos' })).toBeVisible()
  await expect(tabela(page).getByRole('columnheader')).toHaveText(['Nº', 'Data', 'Cliente', 'Itens', 'Valor total'])
  await expect(linhas(page)).toHaveCount(SEED.tamanhoPagina)
  await expect(paginacao(page)).toContainText(/Página 1 de \d+ · [\d.]+ pedidos/)
  await expect(paginacao(page).getByRole('button', { name: '← Anterior' })).toBeDisabled()
})

// 2 ──────────────────────────────────────────────────────────────────────────
test('2. filtra pedidos por cliente', async ({ page }) => {
  const { id, nome } = SEED.clienteSemHomonimo

  await filtrar(page, { clienteId: id })

  await expect(linhas(page).first()).toContainText(nome)
  await todasContem(linhas(page), nome)

  // Limpar volta à listagem geral.
  await filtros(page).getByRole('button', { name: 'Limpar' }).click()
  await expect(filtros(page).getByLabel('Cliente')).toHaveValue('')
  await expect(paginacao(page)).not.toContainText(/· [1-9]\d? pedidos$/)
})

// 3 ──────────────────────────────────────────────────────────────────────────
test('3. filtra pedidos por período (inclui o dia final inteiro)', async ({ page }) => {
  const { data, exibida, pedidos } = SEED.dia

  await filtrar(page, { de: data, ate: data })

  await expect(paginacao(page)).toContainText(`· ${pedidos} pedidos`)
  await todasContem(linhas(page), exibida)
})

test('3b. combina cliente e período', async ({ page }) => {
  const { id, nome, pedidosEm2025 } = SEED.clienteSemHomonimo

  await filtrar(page, { clienteId: id, de: '2025-01-01', ate: '2025-12-31' })

  await expect(paginacao(page)).toContainText(`· ${pedidosEm2025} pedidos`)
  await todasContem(linhas(page), nome)
  await todasContem(linhas(page), '/2025')
})

// 4 ──────────────────────────────────────────────────────────────────────────
test('4. pagina os resultados', async ({ page }) => {
  const { data, pedidos, primeiroPedidoId } = SEED.dia
  const totalPaginas = Math.ceil(pedidos / SEED.tamanhoPagina) // 4
  const anterior = paginacao(page).getByRole('button', { name: '← Anterior' })
  const proxima = paginacao(page).getByRole('button', { name: 'Próxima →' })

  await filtrar(page, { de: data, ate: data })

  // Página 1: 10 linhas, ordenadas da mais recente; "Anterior" desabilitado.
  await expect(paginacao(page)).toContainText(`Página 1 de ${totalPaginas}`)
  await expect(linhas(page)).toHaveCount(SEED.tamanhoPagina)
  await expect(linhas(page).first()).toContainText(`#${primeiroPedidoId}`)
  await expect(anterior).toBeDisabled()
  const idsPagina1 = await linhas(page).locator('td:first-child').allTextContents()

  // Página 2: outras 10 linhas, sem repetir as da página 1.
  await proxima.click()
  await expect(paginacao(page)).toContainText(`Página 2 de ${totalPaginas}`)
  await expect(linhas(page)).toHaveCount(SEED.tamanhoPagina)
  const idsPagina2 = await linhas(page).locator('td:first-child').allTextContents()
  expect(idsPagina2.filter((id) => idsPagina1.includes(id))).toEqual([])

  // Última página: o resto (31 = 10 + 10 + 10 + 1); "Próxima" desabilitado.
  await proxima.click()
  await proxima.click()
  await expect(paginacao(page)).toContainText(`Página ${totalPaginas} de ${totalPaginas}`)
  await expect(linhas(page)).toHaveCount(pedidos - SEED.tamanhoPagina * (totalPaginas - 1))
  await expect(proxima).toBeDisabled()

  // Voltar funciona.
  await anterior.click()
  await expect(paginacao(page)).toContainText(`Página ${totalPaginas - 1} de ${totalPaginas}`)
})

// 5 ──────────────────────────────────────────────────────────────────────────
test('5. abre e fecha o modal de criação de pedido', async ({ page }) => {
  const modal = await abrirModal(page)

  await expect(modal.getByLabel('Cliente')).toHaveValue('')
  await expect(modal.getByLabel('Produto 1')).toBeVisible()
  await expect(modal.getByLabel('Total do pedido')).toHaveText(moeda(0))
  await expect(modal.getByRole('button', { name: 'Salvar pedido' })).toBeDisabled()

  await modal.getByRole('button', { name: 'Cancelar' }).click()
  await expect(modal).toBeHidden()

  // Esc também fecha.
  await abrirModal(page)
  await page.keyboard.press('Escape')
  await expect(page.getByRole('dialog')).toBeHidden()
})

// 6 ──────────────────────────────────────────────────────────────────────────
test('6. cria um pedido válido e o vê no topo da listagem', async ({ page, request }) => {
  const cliente = SEED.clienteSemHomonimo
  const [p1, p2] = (await produtos(request)).filter((p) => p.estoque >= 10)

  const modal = await abrirModal(page)
  await modal.getByLabel('Cliente').selectOption(String(cliente.id))
  await modal.getByLabel('Produto 1').selectOption(String(p1.id))
  await modal.getByLabel('Quantidade').fill('2')
  await modal.getByRole('button', { name: '+ Adicionar item' }).click()
  await modal.getByLabel('Produto 2').selectOption(String(p2.id))

  const totalEsperado = p1.preco * 2 + p2.preco
  await expect(modal.getByLabel('Total do pedido')).toHaveText(moeda(totalEsperado))

  const post = page.waitForResponse((r) => r.url().endsWith('/api/pedidos') && r.request().method() === 'POST')
  await modal.getByRole('button', { name: 'Salvar pedido' }).click()
  const resposta = await post
  expect(resposta.status()).toBe(201)
  const criado = (await resposta.json()) as { id: number; valorTotal: number }
  expect(criado.valorTotal).toBeCloseTo(totalEsperado, 2)

  await expect(modal).toBeHidden()
  await expect(page.getByRole('status')).toContainText(`Pedido #${criado.id} criado para ${cliente.nome}`)
  const primeira = linhas(page).first()
  await expect(primeira).toContainText(`#${criado.id}`)
  await expect(primeira).toContainText(cliente.nome)
  await expect(primeira).toContainText(moeda(totalEsperado))

  // Persistido de verdade: aparece ao filtrar pelo cliente e o estoque foi baixado no banco.
  await filtrar(page, { clienteId: cliente.id })
  await expect(linhas(page).first()).toContainText(`#${criado.id}`)
  const depois = await produtos(request)
  expect(depois.find((p) => p.id === p1.id)?.estoque).toBe(p1.estoque - 2)
  expect(depois.find((p) => p.id === p2.id)?.estoque).toBe(p2.estoque - 1)
})

// 7 ──────────────────────────────────────────────────────────────────────────
test('7a. estoque insuficiente: exibe o erro da API (422) e preserva o formulário', async ({ page, request }) => {
  // O seed tem produto com estoque 0; ninguém consegue comprá-lo, então ele continua em 0.
  const esgotado = (await produtos(request)).find((p) => p.estoque === 0)
  expect(esgotado, 'o seed deve ter um produto com estoque zero').toBeDefined()

  const modal = await abrirModal(page)
  await modal.getByLabel('Cliente').selectOption(String(SEED.clienteSemHomonimo.id))
  await modal.getByLabel('Produto 1').selectOption(String(esgotado!.id))
  await modal.getByLabel('Quantidade').fill('1')
  await expect(modal.getByText('Quantidade acima do estoque disponível (0).')).toBeVisible()

  const post = page.waitForResponse((r) => r.url().endsWith('/api/pedidos') && r.request().method() === 'POST')
  await modal.getByRole('button', { name: 'Salvar pedido' }).click()
  expect((await post).status()).toBe(422)

  await expect(modal.getByRole('alert')).toContainText(`Estoque insuficiente para o produto '${esgotado!.nome}'`)
  await expect(modal).toBeVisible()
  await expect(modal.getByLabel('Cliente')).toHaveValue(String(SEED.clienteSemHomonimo.id))
  await expect(modal.getByLabel('Produto 1')).toHaveValue(String(esgotado!.id))
  await expect(modal.getByLabel('Quantidade')).toHaveValue('1')
})

test('7b. formulário incompleto ou quantidade inválida não pode ser enviado', async ({ page, request }) => {
  const [produto] = (await produtos(request)).filter((p) => p.estoque >= 10)
  const modal = await abrirModal(page)
  const salvar = modal.getByRole('button', { name: 'Salvar pedido' })

  // Sem cliente.
  await modal.getByLabel('Produto 1').selectOption(String(produto.id))
  await expect(salvar).toBeDisabled()

  // Com cliente, mas quantidade zero / negativa / fracionária.
  await modal.getByLabel('Cliente').selectOption(String(SEED.clienteSemHomonimo.id))
  for (const invalida of ['0', '-1', '1.5']) {
    await modal.getByLabel('Quantidade').fill(invalida)
    await expect(salvar).toBeDisabled()
  }

  // Produto já usado em outra linha não pode ser escolhido de novo (RN05).
  await modal.getByLabel('Quantidade').fill('1')
  await modal.getByRole('button', { name: '+ Adicionar item' }).click()
  // toBeDisabled() não reconhece <option disabled> no Playwright 1.52; checa o atributo nativo.
  await expect(modal.getByLabel('Produto 2').locator(`option[value="${produto.id}"]`)).toHaveAttribute('disabled', '')

  // Linha vazia bloqueia; removida, volta a permitir.
  await expect(salvar).toBeDisabled()
  await modal.getByRole('button', { name: 'Remover item 2' }).click()
  await expect(salvar).toBeEnabled()
})

test('7c. período invertido no filtro é sinalizado e não é enviado', async ({ page }) => {
  const form = filtros(page)
  await form.getByLabel('De', { exact: true }).fill('2025-03-10')
  await form.getByLabel('Até', { exact: true }).fill('2025-03-01')

  await expect(form.getByText('A data final deve ser igual ou posterior à inicial.')).toBeVisible()
  await expect(form.getByRole('button', { name: 'Filtrar' })).toBeDisabled()
})
