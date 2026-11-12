import type { Cliente, CriarPedidoRequest, FiltroPedidos, Pagina, Pedido, PedidoResumo, ProblemDetails, Produto } from './types'

const BASE = '/api'

/** Erro HTTP da API, com o ProblemDetails já interpretado. */
export class ApiError extends Error {
  readonly status: number
  readonly problem: ProblemDetails

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? `Erro ${status}`)
    this.status = status
    this.problem = problem
  }

  /** Mensagens de validação (400) achatadas, ou a mensagem principal. */
  get mensagens(): string[] {
    const errosDeCampo = Object.values(this.problem.errors ?? {}).flat()
    return errosDeCampo.length > 0 ? errosDeCampo : [this.message]
  }
}

async function request<T>(caminho: string, init?: RequestInit): Promise<T> {
  let resposta: Response
  try {
    resposta = await fetch(`${BASE}${caminho}`, {
      ...init,
      headers: { Accept: 'application/json', 'Content-Type': 'application/json', ...init?.headers },
    })
  } catch {
    throw new ApiError(0, { title: 'Não foi possível conectar à API.' })
  }

  if (!resposta.ok) {
    const problem = (await resposta.json().catch(() => ({}))) as ProblemDetails
    throw new ApiError(resposta.status, { title: resposta.statusText, ...problem })
  }

  return (await resposta.json()) as T
}

function queryString(filtro: FiltroPedidos): string {
  const params = new URLSearchParams()
  if (filtro.clienteId) params.set('clienteId', String(filtro.clienteId))
  if (filtro.dataInicio) params.set('dataInicio', filtro.dataInicio)
  if (filtro.dataFim) params.set('dataFim', filtro.dataFim)
  params.set('pagina', String(filtro.pagina))
  params.set('tamanhoPagina', String(filtro.tamanhoPagina))
  return params.toString()
}

export const api = {
  listarPedidos: (filtro: FiltroPedidos) => request<Pagina<PedidoResumo>>(`/pedidos?${queryString(filtro)}`),
  criarPedido: (pedido: CriarPedidoRequest) =>
    request<Pedido>('/pedidos', { method: 'POST', body: JSON.stringify(pedido) }),
  listarClientes: () => request<Cliente[]>('/clientes'),
  listarProdutos: () => request<Produto[]>('/produtos'),
}
