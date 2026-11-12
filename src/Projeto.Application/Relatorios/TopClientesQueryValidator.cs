using FluentValidation;

namespace Projeto.Application.Relatorios;

public sealed class TopClientesQueryValidator : AbstractValidator<TopClientesQuery>
{
    public TopClientesQueryValidator()
    {
        RuleFor(q => q.DataInicio)
            .NotNull().WithMessage("Informe a data inicial do período.");

        RuleFor(q => q.DataFim)
            .NotNull().WithMessage("Informe a data final do período.");

        RuleFor(q => q.DataFim)
            .GreaterThanOrEqualTo(q => q.DataInicio)
            .When(q => q.DataInicio.HasValue && q.DataFim.HasValue)
            .WithMessage("A data final deve ser igual ou posterior à data inicial.");
    }
}
