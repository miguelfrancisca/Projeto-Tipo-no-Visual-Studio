const moeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })
const dataHora = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' })

export const formatarMoeda = (valor: number) => moeda.format(valor)

/** A API devolve UTC; exibe no fuso do navegador. */
export const formatarDataHora = (iso: string) => dataHora.format(new Date(iso))
