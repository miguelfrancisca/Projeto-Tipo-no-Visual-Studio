interface Props {
  pagina: number
  totalPaginas: number
  totalItens: number
  onMudar: (pagina: number) => void
}

export function Paginacao({ pagina, totalPaginas, totalItens, onMudar }: Props) {
  if (totalItens === 0) return null

  return (
    <nav className="paginacao" aria-label="Paginação">
      <button type="button" className="secundario" disabled={pagina <= 1} onClick={() => onMudar(pagina - 1)}>
        ← Anterior
      </button>
      <span>
        Página {pagina} de {totalPaginas} · {totalItens.toLocaleString('pt-BR')} pedidos
      </span>
      <button
        type="button"
        className="secundario"
        disabled={pagina >= totalPaginas}
        onClick={() => onMudar(pagina + 1)}
      >
        Próxima →
      </button>
    </nav>
  )
}
