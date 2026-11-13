using FluentValidation;

namespace MiniGestaoPedidos.Application.Common;

/// <summary>Regras de período comuns (RN11, RN14). Incluído pelos validators das consultas com <c>Include</c>.</summary>
public sealed class PeriodoValidator : AbstractValidator<IFiltroPeriodo>
{
    public PeriodoValidator()
    {
        RuleFor(f => f)
            .Must(f => !(Periodo.UsaInstantes(f) && Periodo.UsaDias(f)))
            .OverridePropertyName("periodo")
            .WithMessage("Informe o período com inicio/fim (instantes com offset) ou com dataInicio/dataFim (dias em UTC), não com os dois.");

        RuleFor(f => f.DataFim)
            .GreaterThanOrEqualTo(f => f.DataInicio)
            .When(f => f.DataInicio.HasValue && f.DataFim.HasValue)
            .WithMessage("A data final deve ser igual ou posterior à data inicial.");

        // DateTimeOffset compara INSTANTES (normaliza o offset), não o horário local escrito.
        RuleFor(f => f.Fim)
            .GreaterThan(f => f.Inicio)
            .When(f => f.Inicio.HasValue && f.Fim.HasValue)
            .WithMessage("O fim do período deve ser posterior ao início (o fim é exclusivo).");
    }
}
