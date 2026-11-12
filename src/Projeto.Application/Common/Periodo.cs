namespace Projeto.Application.Common;

/// <summary>
/// Converte um período de datas (inclusivo nos dois extremos, como na spec) num
/// intervalo semiaberto [Inicio, FimExclusivo) — formato que permite index seek
/// e inclui o dia final inteiro (CA11).
/// </summary>
public static class Periodo
{
    public static DateTime? Inicio(DateOnly? data) =>
        data?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    public static DateTime? FimExclusivo(DateOnly? data) =>
        data?.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
}
