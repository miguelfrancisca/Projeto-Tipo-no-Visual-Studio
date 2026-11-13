/**
 * Conversão de dias escolhidos pelo usuário (yyyy-MM-dd, sem fuso) em instantes com offset
 * explícito, no fuso do NAVEGADOR (RN14). A API recebe esses instantes e converte para UTC.
 *
 * Um período de dias locais [primeiro, último] vira o intervalo semiaberto
 *   [meia-noite local do primeiro dia, meia-noite local do dia SEGUINTE ao último)
 * e cada limite leva o offset do SEU instante. Em fusos com horário de verão os dois offsets
 * podem diferir (dias de 23 ou 25 horas) — por isso nunca se usa o offset de "agora".
 */

const FORMATO_DIA = /^(\d{4})-(\d{2})-(\d{2})$/

/** Meia-noite local de `dia`, como ISO 8601 com offset. Ex. em São Paulo: 2025-03-10T00:00:00-03:00 */
export function inicioDoDiaLocal(dia: string): string {
  return comOffset(meiaNoiteLocal(dia, 0))
}

/** Meia-noite local do dia seguinte a `dia` (limite exclusivo que inclui `dia` inteiro). */
export function inicioDoDiaSeguinteLocal(dia: string): string {
  return comOffset(meiaNoiteLocal(dia, 1))
}

function meiaNoiteLocal(dia: string, diasAdiante: number): Date {
  const partes = FORMATO_DIA.exec(dia)
  if (!partes) throw new Error(`Dia inválido: "${dia}" (esperado yyyy-MM-dd).`)
  const [, ano, mes, d] = partes.map(Number)
  // O construtor com componentes usa o fuso local e normaliza estouros (31/12 + 1 → 01/01).
  // Se a meia-noite não existir naquele dia (horário de verão começando às 00:00), o motor
  // avança para o primeiro instante válido (ex.: 01:00) — que continua sendo o início do dia.
  return new Date(ano, mes - 1, d + diasAdiante)
}

/** Formata o instante com os campos LOCAIS e o offset vigente NAQUELE instante. */
function comOffset(data: Date): string {
  const dois = (n: number) => String(n).padStart(2, '0')
  const minutosAFrenteDeUtc = -data.getTimezoneOffset()
  const sinal = minutosAFrenteDeUtc >= 0 ? '+' : '-'
  const absoluto = Math.abs(minutosAFrenteDeUtc)

  return (
    `${String(data.getFullYear()).padStart(4, '0')}-${dois(data.getMonth() + 1)}-${dois(data.getDate())}` +
    `T${dois(data.getHours())}:${dois(data.getMinutes())}:${dois(data.getSeconds())}` +
    `${sinal}${dois(Math.floor(absoluto / 60))}:${dois(absoluto % 60)}`
  )
}
