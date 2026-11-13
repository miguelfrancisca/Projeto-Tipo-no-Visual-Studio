import { afterEach, describe, expect, it } from 'vitest'
import { inicioDoDiaLocal, inicioDoDiaSeguinteLocal } from './datas'

/**
 * O fuso é trocado por teste via process.env.TZ (o Node recarrega o fuso ao alterar TZ).
 * Roda no pool "forks" (vite.config.ts), em que process.env é o do processo real.
 */
const fusoOriginal = process.env.TZ
afterEach(() => {
  process.env.TZ = fusoOriginal
})

function noFuso(fuso: string) {
  process.env.TZ = fuso
}

const horasEntre = (inicio: string, fim: string) => (Date.parse(fim) - Date.parse(inicio)) / 3_600_000

describe('UTC', () => {
  it('um dia vai de 00:00+00:00 a 00:00+00:00 do dia seguinte', () => {
    noFuso('UTC')

    expect(inicioDoDiaLocal('2025-03-10')).toBe('2025-03-10T00:00:00+00:00')
    expect(inicioDoDiaSeguinteLocal('2025-03-10')).toBe('2025-03-11T00:00:00+00:00')
  })
})

describe('America/Sao_Paulo', () => {
  it('um dia: meia-noite local com offset -03:00', () => {
    noFuso('America/Sao_Paulo')

    expect(inicioDoDiaLocal('2025-03-10')).toBe('2025-03-10T00:00:00-03:00')
    expect(inicioDoDiaSeguinteLocal('2025-03-10')).toBe('2025-03-11T00:00:00-03:00')
  })

  it('vários dias: o fim é a meia-noite seguinte ao último dia', () => {
    noFuso('America/Sao_Paulo')

    const inicio = inicioDoDiaLocal('2025-03-10')
    const fim = inicioDoDiaSeguinteLocal('2025-03-12')

    expect(fim).toBe('2025-03-13T00:00:00-03:00')
    expect(horasEntre(inicio, fim)).toBe(72)
  })

  it('o instante enviado corresponde a 03:00 UTC (o pedido de 02:41 UTC do dia 11 fica dentro)', () => {
    noFuso('America/Sao_Paulo')

    expect(new Date(inicioDoDiaSeguinteLocal('2025-03-10')).toISOString()).toBe('2025-03-11T03:00:00.000Z')
    expect(Date.parse('2025-03-11T02:41:00Z')).toBeLessThan(Date.parse(inicioDoDiaSeguinteLocal('2025-03-10')))
  })

  it('horário de verão antigo (04/11/2018): a meia-noite não existia; o dia começa às 01:00-02:00', () => {
    noFuso('America/Sao_Paulo')

    expect(inicioDoDiaSeguinteLocal('2018-11-03')).toBe('2018-11-04T01:00:00-02:00')
    expect(inicioDoDiaLocal('2018-11-04')).toBe('2018-11-04T01:00:00-02:00')
    expect(horasEntre(inicioDoDiaLocal('2018-11-03'), inicioDoDiaSeguinteLocal('2018-11-03'))).toBe(24)
    expect(horasEntre(inicioDoDiaLocal('2018-11-04'), inicioDoDiaSeguinteLocal('2018-11-04'))).toBe(23)
  })
})

describe('transições de horário de verão (America/New_York)', () => {
  it('início do horário de verão (09/03/2025): limites com offsets diferentes, dia de 23 h', () => {
    noFuso('America/New_York')

    const inicio = inicioDoDiaLocal('2025-03-09')
    const fim = inicioDoDiaSeguinteLocal('2025-03-09')

    expect(inicio).toBe('2025-03-09T00:00:00-05:00')
    expect(fim).toBe('2025-03-10T00:00:00-04:00')
    expect(horasEntre(inicio, fim)).toBe(23)
  })

  it('fim do horário de verão (02/11/2025): dia de 25 h', () => {
    noFuso('America/New_York')

    const inicio = inicioDoDiaLocal('2025-11-02')
    const fim = inicioDoDiaSeguinteLocal('2025-11-02')

    expect(inicio).toBe('2025-11-02T00:00:00-04:00')
    expect(fim).toBe('2025-11-03T00:00:00-05:00')
    expect(horasEntre(inicio, fim)).toBe(25)
  })
})

describe('offsets positivos e fracionários', () => {
  it('Asia/Kolkata usa +05:30', () => {
    noFuso('Asia/Kolkata')

    expect(inicioDoDiaLocal('2025-03-10')).toBe('2025-03-10T00:00:00+05:30')
  })

  it('Asia/Kathmandu usa +05:45', () => {
    noFuso('Asia/Kathmandu')

    expect(inicioDoDiaSeguinteLocal('2025-03-10')).toBe('2025-03-11T00:00:00+05:45')
  })
})

describe('calendário', () => {
  it('vira o mês e o ano', () => {
    noFuso('America/Sao_Paulo')

    expect(inicioDoDiaSeguinteLocal('2025-12-31')).toBe('2026-01-01T00:00:00-03:00')
    expect(inicioDoDiaSeguinteLocal('2024-02-28')).toBe('2024-02-29T00:00:00-03:00')
    expect(inicioDoDiaSeguinteLocal('2025-02-28')).toBe('2025-03-01T00:00:00-03:00')
  })

  it('rejeita formato inválido', () => {
    expect(() => inicioDoDiaLocal('10/03/2025')).toThrow('Dia inválido')
    expect(() => inicioDoDiaLocal('')).toThrow('Dia inválido')
  })
})
