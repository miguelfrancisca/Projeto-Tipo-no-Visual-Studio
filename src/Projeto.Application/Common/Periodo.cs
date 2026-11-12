namespace Projeto.Application.Common;

/// <summary>
/// Converte o período pedido pelo cliente num intervalo UTC semiaberto [início, fim) — o formato
/// que permite index seek e que o banco (datas em UTC) entende. A conversão acontece aqui, na
/// Application, ANTES de qualquer consulta, e nunca usa o fuso da máquina do servidor (RN14).
/// </summary>
public static class Periodo
{
    /// <summary>Resolve qualquer uma das duas formas de <see cref="IFiltroPeriodo"/>.</summary>
    public static IntervaloUtc ParaUtc(IFiltroPeriodo filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        if (UsaInstantes(filtro) && UsaDias(filtro))
        {
            throw new InvalidOperationException("Período informado nas duas formas (dias em UTC e instantes); valide a consulta antes.");
        }

        // DateTimeOffset.UtcDateTime = DateTime - Offset: aritmética sobre o offset que veio na
        // requisição. Diferente de DateTime.ToUniversalTime(), não consulta TimeZoneInfo.Local.
        return UsaInstantes(filtro)
            ? new IntervaloUtc(filtro.Inicio?.UtcDateTime, filtro.Fim?.UtcDateTime)
            : new IntervaloUtc(Inicio(filtro.DataInicio), FimExclusivo(filtro.DataFim));
    }

    public static bool UsaInstantes(IFiltroPeriodo filtro) => filtro.Inicio.HasValue || filtro.Fim.HasValue;

    public static bool UsaDias(IFiltroPeriodo filtro) => filtro.DataInicio.HasValue || filtro.DataFim.HasValue;

    /// <summary>Forma legada: meia-noite UTC do dia.</summary>
    public static DateTime? Inicio(DateOnly? data) =>
        data?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    /// <summary>Forma legada: meia-noite UTC do dia seguinte (inclui o dia final inteiro, CA11).</summary>
    public static DateTime? FimExclusivo(DateOnly? data) =>
        data?.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
}
