using Projeto.Application.Common;

namespace Projeto.Application.Pedidos;

/// <summary>Filtros da listagem (query string de GET /pedidos). Todos opcionais.</summary>
public sealed class ListarPedidosQuery : IFiltroPeriodo
{
    public const int TamanhoPaginaPadrao = 10;
    public const int TamanhoPaginaMaximo = 100;

    public int? ClienteId { get; init; }

    /// <summary>Dia inicial em UTC (yyyy-MM-dd), inclusivo. Compatibilidade — prefira <see cref="Inicio"/>.</summary>
    public DateOnly? DataInicio { get; init; }

    /// <summary>Dia final em UTC (yyyy-MM-dd), inclusivo. Compatibilidade — prefira <see cref="Fim"/>.</summary>
    public DateOnly? DataFim { get; init; }

    /// <summary>Início do período (inclusivo), ISO 8601 com offset obrigatório. Ex.: 2025-03-10T00:00:00-03:00.</summary>
    public DateTimeOffset? Inicio { get; init; }

    /// <summary>Fim do período (EXCLUSIVO), ISO 8601 com offset obrigatório. Ex.: 2025-03-11T00:00:00-03:00.</summary>
    public DateTimeOffset? Fim { get; init; }

    public int Pagina { get; init; } = 1;

    public int TamanhoPagina { get; init; } = TamanhoPaginaPadrao;
}
