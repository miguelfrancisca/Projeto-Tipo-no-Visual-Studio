using MiniGestaoPedidos.Application.Common;

namespace MiniGestaoPedidos.Application.Relatorios;

/// <summary>
/// Query string de GET /relatorios/top-clientes. Período obrigatório (RN13), em uma das formas
/// de <see cref="IFiltroPeriodo"/> (RN14).
/// </summary>
public sealed class TopClientesQuery : IFiltroPeriodo
{
    public const int Limite = 5;

    /// <summary>Dia inicial em UTC (yyyy-MM-dd), inclusivo. Compatibilidade — prefira <see cref="Inicio"/>.</summary>
    public DateOnly? DataInicio { get; init; }

    /// <summary>Dia final em UTC (yyyy-MM-dd), inclusivo. Compatibilidade — prefira <see cref="Fim"/>.</summary>
    public DateOnly? DataFim { get; init; }

    /// <summary>Início do período (inclusivo), ISO 8601 com offset obrigatório.</summary>
    public DateTimeOffset? Inicio { get; init; }

    /// <summary>Fim do período (EXCLUSIVO), ISO 8601 com offset obrigatório.</summary>
    public DateTimeOffset? Fim { get; init; }
}
