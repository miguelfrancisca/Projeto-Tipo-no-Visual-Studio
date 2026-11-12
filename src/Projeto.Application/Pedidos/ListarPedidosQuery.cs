namespace Projeto.Application.Pedidos;

/// <summary>Filtros da listagem (query string de GET /pedidos). Todos opcionais.</summary>
public sealed class ListarPedidosQuery
{
    public const int TamanhoPaginaPadrao = 10;
    public const int TamanhoPaginaMaximo = 100;

    public int? ClienteId { get; init; }

    public DateOnly? DataInicio { get; init; }

    public DateOnly? DataFim { get; init; }

    public int Pagina { get; init; } = 1;

    public int TamanhoPagina { get; init; } = TamanhoPaginaPadrao;
}
