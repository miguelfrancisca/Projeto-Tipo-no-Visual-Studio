namespace Projeto.Application.Relatorios;

/// <summary>Query string de GET /relatorios/top-clientes. Período obrigatório (RN13).</summary>
public sealed class TopClientesQuery
{
    public const int Limite = 5;

    public DateOnly? DataInicio { get; init; }

    public DateOnly? DataFim { get; init; }
}
