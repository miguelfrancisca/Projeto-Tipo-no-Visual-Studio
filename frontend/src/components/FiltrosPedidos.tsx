import { useState, type FormEvent } from 'react'
import type { Cliente } from '../api/types'

export interface ValoresFiltro {
  clienteId?: number
  dataInicio?: string
  dataFim?: string
}

interface Props {
  clientes: Cliente[]
  onFiltrar: (valores: ValoresFiltro) => void
}

/** Filtros por cliente e período. Só dispara a busca ao clicar em "Filtrar". */
export function FiltrosPedidos({ clientes, onFiltrar }: Props) {
  const [clienteId, setClienteId] = useState('')
  const [dataInicio, setDataInicio] = useState('')
  const [dataFim, setDataFim] = useState('')

  const periodoInvalido = dataInicio !== '' && dataFim !== '' && dataInicio > dataFim

  function filtrar(e: FormEvent) {
    e.preventDefault()
    onFiltrar({
      clienteId: clienteId ? Number(clienteId) : undefined,
      dataInicio: dataInicio || undefined,
      dataFim: dataFim || undefined,
    })
  }

  function limpar() {
    setClienteId('')
    setDataInicio('')
    setDataFim('')
    onFiltrar({})
  }

  return (
    <form className="filtros" onSubmit={filtrar} aria-label="Filtros de pedidos">
      <label>
        Cliente
        <select value={clienteId} onChange={(e) => setClienteId(e.target.value)}>
          <option value="">Todos</option>
          {clientes.map((c) => (
            <option key={c.id} value={c.id}>
              {c.nome} — {c.email}
            </option>
          ))}
        </select>
      </label>

      <label>
        De
        <input type="date" value={dataInicio} onChange={(e) => setDataInicio(e.target.value)} />
      </label>

      <label>
        Até
        <input type="date" value={dataFim} onChange={(e) => setDataFim(e.target.value)} />
      </label>

      <div className="filtros-acoes">
        <button type="submit" disabled={periodoInvalido}>
          Filtrar
        </button>
        <button type="button" className="secundario" onClick={limpar}>
          Limpar
        </button>
      </div>

      {periodoInvalido && <p className="aviso">A data final deve ser igual ou posterior à inicial.</p>}
    </form>
  )
}
