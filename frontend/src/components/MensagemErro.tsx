import { ApiError } from '../api/client'

interface Props {
  erro: unknown
}

/** Exibe um erro da API (ProblemDetails) ou um erro inesperado. */
export function MensagemErro({ erro }: Props) {
  const mensagens = erro instanceof ApiError ? erro.mensagens : ['Ocorreu um erro inesperado.']

  return (
    <div className="alerta alerta-erro" role="alert">
      {mensagens.length === 1 ? (
        mensagens[0]
      ) : (
        <ul>
          {mensagens.map((m) => (
            <li key={m}>{m}</li>
          ))}
        </ul>
      )}
    </div>
  )
}
