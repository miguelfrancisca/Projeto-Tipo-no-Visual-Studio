// Tipos espelhando os DTOs da API (src/MiniGestaoPedidos.Application). Datas chegam como ISO 8601 UTC.

export interface Cliente {
  id: number
  nome: string
  email: string
}

export interface Produto {
  id: number
  nome: string
  sku: string
  preco: number
  estoque: number
}

export interface PedidoResumo {
  id: number
  clienteId: number
  clienteNome: string
  dataPedido: string
  valorTotal: number
  quantidadeItens: number
}

export interface PedidoItem {
  produtoId: number
  produtoNome: string
  quantidade: number
  precoUnitario: number
  subtotal: number
}

export interface Pedido {
  id: number
  clienteId: number
  clienteNome: string
  dataPedido: string
  valorTotal: number
  itens: PedidoItem[]
}

export interface Pagina<T> {
  itens: T[]
  pagina: number
  tamanhoPagina: number
  totalItens: number
  totalPaginas: number
}

export interface FiltroPedidos {
  clienteId?: number
  dataInicio?: string // yyyy-MM-dd, dia LOCAL do navegador (vira `inicio` com offset no client)
  dataFim?: string // yyyy-MM-dd, dia LOCAL, inclusivo (vira `fim` exclusivo no client)
  pagina: number
  tamanhoPagina: number
}

export interface CriarPedidoRequest {
  clienteId: number
  itens: { produtoId: number; quantidade: number }[]
}

/** RFC 7807 — formato de todos os erros da API. */
export interface ProblemDetails {
  title?: string
  status?: number
  detail?: string
  errors?: Record<string, string[]>
  [extensao: string]: unknown
}
