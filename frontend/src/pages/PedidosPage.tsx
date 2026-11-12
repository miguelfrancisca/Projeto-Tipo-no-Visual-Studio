import { useCallback, useEffect, useState } from 'react'
import { api } from '../api/client'
import type { Cliente, FiltroPedidos, Pagina, PedidoResumo } from '../api/types'
import { FiltrosPedidos, type ValoresFiltro } from '../components/FiltrosPedidos'
import { MensagemErro } from '../components/MensagemErro'
import { Paginacao } from '../components/Paginacao'
import { TabelaPedidos } from '../components/TabelaPedidos'

const TAMANHO_PAGINA = 10

export function PedidosPage() {
  const [clientes, setClientes] = useState<Cliente[]>([])
  const [filtro, setFiltro] = useState<FiltroPedidos>({ pagina: 1, tamanhoPagina: TAMANHO_PAGINA })
  const [resultado, setResultado] = useState<Pagina<PedidoResumo>>()
  const [carregando, setCarregando] = useState(true)
  const [erro, setErro] = useState<unknown>()

  useEffect(() => {
    api.listarClientes().then(setClientes).catch(setErro)
  }, [])

  const carregar = useCallback(async () => {
    setCarregando(true)
    setErro(undefined)
    try {
      setResultado(await api.listarPedidos(filtro))
    } catch (e) {
      setErro(e)
    } finally {
      setCarregando(false)
    }
  }, [filtro])

  useEffect(() => {
    void carregar()
  }, [carregar])

  const filtrar = (valores: ValoresFiltro) => setFiltro({ ...valores, pagina: 1, tamanhoPagina: TAMANHO_PAGINA })

  return (
    <main className="pagina">
      <header className="cabecalho">
        <h1>Pedidos</h1>
      </header>

      <FiltrosPedidos clientes={clientes} onFiltrar={filtrar} />

      {erro !== undefined && <MensagemErro erro={erro} />}

      <section aria-busy={carregando} className={carregando ? 'carregando' : undefined}>
        {resultado && <TabelaPedidos pedidos={resultado.itens} />}
        {resultado && (
          <Paginacao
            pagina={resultado.pagina}
            totalPaginas={resultado.totalPaginas}
            totalItens={resultado.totalItens}
            onMudar={(pagina) => setFiltro((f) => ({ ...f, pagina }))}
          />
        )}
      </section>
    </main>
  )
}
