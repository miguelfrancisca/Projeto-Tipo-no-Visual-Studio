namespace Projeto.Application.Relatorios;

public sealed record TopClienteResponse(int ClienteId, string ClienteNome, decimal ValorTotal, int QuantidadePedidos);
