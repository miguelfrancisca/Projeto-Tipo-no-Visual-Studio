namespace Projeto.Application.Common;

/// <summary>Intervalo semiaberto [Inicio, FimExclusivo) em UTC, pronto para a consulta. Null = sem limite.</summary>
public sealed record IntervaloUtc(DateTime? Inicio, DateTime? FimExclusivo);
