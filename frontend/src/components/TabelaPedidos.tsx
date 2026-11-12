import type { PedidoResumo } from '../api/types'
import { formatarDataHora, formatarMoeda } from '../format'

interface Props {
  pedidos: PedidoResumo[]
  destacarId?: number
}

export function TabelaPedidos({ pedidos, destacarId }: Props) {
  if (pedidos.length === 0) {
    return <p className="vazio">Nenhum pedido encontrado para os filtros informados.</p>
  }

  return (
    <table className="tabela" aria-label="Pedidos">
      <thead>
        <tr>
          <th scope="col">Nº</th>
          <th scope="col">Data</th>
          <th scope="col">Cliente</th>
          <th scope="col" className="numero">
            Itens
          </th>
          <th scope="col" className="numero">
            Valor total
          </th>
        </tr>
      </thead>
      <tbody>
        {pedidos.map((p) => (
          <tr key={p.id} className={p.id === destacarId ? 'destaque' : undefined}>
            <td>#{p.id}</td>
            <td>{formatarDataHora(p.dataPedido)}</td>
            <td>{p.clienteNome}</td>
            <td className="numero">{p.quantidadeItens}</td>
            <td className="numero">{formatarMoeda(p.valorTotal)}</td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}
