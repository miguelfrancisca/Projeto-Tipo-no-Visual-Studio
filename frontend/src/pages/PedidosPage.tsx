import { useCallback, useEffect, useState } from 'react'
import { api } from '../api/client'
import type { Cliente, FiltroPedidos, Pagina, Pedido, PedidoResumo } from '../api/types'
import { FiltrosPedidos, type ValoresFiltro } from '../components/FiltrosPedidos'
import { MensagemErro } from '../components/MensagemErro'
import { NovoPedidoModal } from '../components/NovoPedidoModal'
import { Paginacao } from '../components/Paginacao'
import { TabelaPedidos } from '../components/TabelaPedidos'
import { formatarMoeda } from '../format'

const TAMANHO_PAGINA = 10
const FILTRO_INICIAL: FiltroPedidos = { pagina: 1, tamanhoPagina: TAMANHO_PAGINA }

export function PedidosPage() {
  const [clientes, setClientes] = useState<Cliente[]>([])
  const [filtro, setFiltro] = useState<FiltroPedidos>(FILTRO_INICIAL)
  const [versaoFiltros, setVersaoFiltros] = useState(0) // remonta o formulário de filtros ao limpar
  const [resultado, setResultado] = useState<Pagina<PedidoResumo>>()
  const [carregando, setCarregando] = useState(true)
  const [erro, setErro] = useState<unknown>()
  const [modalAberto, setModalAberto] = useState(false)
  const [criado, setCriado] = useState<Pedido>()

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

  const filtrar = (valores: ValoresFiltro) => {
    setCriado(undefined)
    setFiltro({ ...valores, pagina: 1, tamanhoPagina: TAMANHO_PAGINA })
  }

  // Após criar: limpa filtros e volta à 1ª página, onde o pedido novo aparece no topo (ordem por data desc).
  const aoCriar = (pedido: Pedido) => {
    setModalAberto(false)
    setCriado(pedido)
    setVersaoFiltros((v) => v + 1)
    setFiltro({ ...FILTRO_INICIAL })
  }

  return (
    <main className="pagina">
      <header className="cabecalho">
        <h1>Pedidos</h1>
        <button type="button" onClick={() => setModalAberto(true)}>
          + Novo pedido
        </button>
      </header>

      <FiltrosPedidos key={versaoFiltros} clientes={clientes} onFiltrar={filtrar} />

      {criado && (
        <div className="alerta alerta-sucesso" role="status">
          Pedido #{criado.id} criado para {criado.clienteNome} — {formatarMoeda(criado.valorTotal)}.
        </div>
      )}

      {erro !== undefined && <MensagemErro erro={erro} />}

      <section aria-busy={carregando} className={carregando ? 'carregando' : undefined}>
        {resultado && <TabelaPedidos pedidos={resultado.itens} destacarId={criado?.id} />}
        {resultado && (
          <Paginacao
            pagina={resultado.pagina}
            totalPaginas={resultado.totalPaginas}
            totalItens={resultado.totalItens}
            onMudar={(pagina) => setFiltro((f) => ({ ...f, pagina }))}
          />
        )}
      </section>

      {modalAberto && (
        <NovoPedidoModal clientes={clientes} onFechar={() => setModalAberto(false)} onCriado={aoCriar} />
      )}
    </main>
  )
}
