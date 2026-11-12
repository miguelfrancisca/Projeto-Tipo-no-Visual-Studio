import { useEffect, useId, useMemo, useState, type FormEvent } from 'react'
import { api } from '../api/client'
import type { Cliente, Pedido, Produto } from '../api/types'
import { formatarMoeda } from '../format'
import { MensagemErro } from './MensagemErro'

interface Props {
  clientes: Cliente[]
  onFechar: () => void
  onCriado: (pedido: Pedido) => void
}

interface Linha {
  chave: number
  produtoId: string
  quantidade: string
}

let proximaChave = 1
const novaLinha = (): Linha => ({ chave: proximaChave++, produtoId: '', quantidade: '1' })

/**
 * Criação de pedido (CA16). A validação em tela só evita erros óbvios;
 * as regras de verdade (cliente existente, estoque) são do servidor, e qualquer
 * recusa (400/409/422) é exibida aqui SEM perder o que foi preenchido.
 */
export function NovoPedidoModal({ clientes, onFechar, onCriado }: Props) {
  const tituloId = useId()
  const [produtos, setProdutos] = useState<Produto[]>([])
  const [clienteId, setClienteId] = useState('')
  const [linhas, setLinhas] = useState<Linha[]>(() => [novaLinha()])
  const [salvando, setSalvando] = useState(false)
  const [erro, setErro] = useState<unknown>()

  // Estoque sempre fresco ao abrir o modal.
  useEffect(() => {
    api.listarProdutos().then(setProdutos).catch(setErro)
  }, [])

  useEffect(() => {
    const aoTeclar = (e: KeyboardEvent) => e.key === 'Escape' && !salvando && onFechar()
    window.addEventListener('keydown', aoTeclar)
    return () => window.removeEventListener('keydown', aoTeclar)
  }, [onFechar, salvando])

  const produtoPorId = useMemo(() => new Map(produtos.map((p) => [String(p.id), p])), [produtos])

  const total = linhas.reduce((soma, l) => {
    const produto = produtoPorId.get(l.produtoId)
    return produto ? soma + produto.preco * (Number(l.quantidade) || 0) : soma
  }, 0)

  const valido =
    clienteId !== '' && linhas.every((l) => l.produtoId !== '' && Number.isInteger(Number(l.quantidade)) && Number(l.quantidade) > 0)

  const alterarLinha = (chave: number, mudanca: Partial<Linha>) =>
    setLinhas((atuais) => atuais.map((l) => (l.chave === chave ? { ...l, ...mudanca } : l)))

  async function salvar(e: FormEvent) {
    e.preventDefault()
    if (!valido) return

    setSalvando(true)
    setErro(undefined)
    try {
      const pedido = await api.criarPedido({
        clienteId: Number(clienteId),
        itens: linhas.map((l) => ({ produtoId: Number(l.produtoId), quantidade: Number(l.quantidade) })),
      })
      onCriado(pedido)
    } catch (err) {
      setErro(err)
      // O estoque pode ter mudado (422/409): recarrega para o usuário ajustar.
      api.listarProdutos().then(setProdutos).catch(() => undefined)
    } finally {
      setSalvando(false)
    }
  }

  return (
    <div className="modal-fundo" onClick={() => !salvando && onFechar()}>
      <div
        className="modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby={tituloId}
        onClick={(e) => e.stopPropagation()}
      >
        <form onSubmit={salvar}>
          <header className="modal-cabecalho">
            <h2 id={tituloId}>Novo pedido</h2>
            <button type="button" className="secundario icone" aria-label="Fechar" onClick={onFechar} disabled={salvando}>
              ✕
            </button>
          </header>

          {erro !== undefined && <MensagemErro erro={erro} />}

          <label>
            Cliente
            <select value={clienteId} onChange={(e) => setClienteId(e.target.value)} required>
              <option value="">Selecione…</option>
              {clientes.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.nome} — {c.email}
                </option>
              ))}
            </select>
          </label>

          <fieldset className="itens">
            <legend>Itens</legend>

            {linhas.map((linha, indice) => {
              const produto = produtoPorId.get(linha.produtoId)
              const quantidade = Number(linha.quantidade) || 0
              const selecionadosEmOutras = new Set(
                linhas.filter((l) => l.chave !== linha.chave).map((l) => l.produtoId),
              )

              return (
                <div className="item-linha" key={linha.chave}>
                  <label>
                    Produto {indice + 1}
                    <select
                      value={linha.produtoId}
                      onChange={(e) => alterarLinha(linha.chave, { produtoId: e.target.value })}
                      required
                    >
                      <option value="">Selecione…</option>
                      {produtos.map((p) => (
                        <option key={p.id} value={p.id} disabled={selecionadosEmOutras.has(String(p.id))}>
                          {p.nome} — {formatarMoeda(p.preco)} ({p.estoque} em estoque)
                        </option>
                      ))}
                    </select>
                  </label>

                  <label className="quantidade">
                    Quantidade
                    <input
                      type="number"
                      min={1}
                      step={1}
                      value={linha.quantidade}
                      onChange={(e) => alterarLinha(linha.chave, { quantidade: e.target.value })}
                      required
                    />
                  </label>

                  <span className="subtotal" aria-label={`Subtotal do item ${indice + 1}`}>
                    {produto ? formatarMoeda(produto.preco * quantidade) : '—'}
                  </span>

                  <button
                    type="button"
                    className="secundario icone"
                    aria-label={`Remover item ${indice + 1}`}
                    disabled={linhas.length === 1}
                    onClick={() => setLinhas((atuais) => atuais.filter((l) => l.chave !== linha.chave))}
                  >
                    ✕
                  </button>

                  {produto && quantidade > produto.estoque && (
                    <p className="aviso">Quantidade acima do estoque disponível ({produto.estoque}).</p>
                  )}
                </div>
              )
            })}

            <button
              type="button"
              className="secundario"
              onClick={() => setLinhas((atuais) => [...atuais, novaLinha()])}
              disabled={linhas.length >= produtos.length}
            >
              + Adicionar item
            </button>
          </fieldset>

          <footer className="modal-rodape">
            <strong>
              Total: <span aria-label="Total do pedido">{formatarMoeda(total)}</span>
            </strong>
            <div className="filtros-acoes">
              <button type="button" className="secundario" onClick={onFechar} disabled={salvando}>
                Cancelar
              </button>
              <button type="submit" disabled={!valido || salvando}>
                {salvando ? 'Salvando…' : 'Salvar pedido'}
              </button>
            </div>
          </footer>
        </form>
      </div>
    </div>
  )
}
