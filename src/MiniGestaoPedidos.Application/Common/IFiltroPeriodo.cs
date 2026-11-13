namespace MiniGestaoPedidos.Application.Common;

/// <summary>
/// Período de uma consulta, em UMA de duas formas (RN14):
/// <list type="bullet">
/// <item><see cref="Inicio"/>/<see cref="Fim"/>: instantes com offset explícito, fim exclusivo — no fuso de quem consulta.</item>
/// <item><see cref="DataInicio"/>/<see cref="DataFim"/>: dias inteiros em UTC, inclusivos — mantida por compatibilidade.</item>
/// </list>
/// </summary>
public interface IFiltroPeriodo
{
    DateOnly? DataInicio { get; }

    DateOnly? DataFim { get; }

    DateTimeOffset? Inicio { get; }

    DateTimeOffset? Fim { get; }
}
