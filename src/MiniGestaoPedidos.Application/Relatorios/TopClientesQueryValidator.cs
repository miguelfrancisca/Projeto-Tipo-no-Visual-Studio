using FluentValidation;
using MiniGestaoPedidos.Application.Common;

namespace MiniGestaoPedidos.Application.Relatorios;

public sealed class TopClientesQueryValidator : AbstractValidator<TopClientesQuery>
{
    public TopClientesQueryValidator()
    {
        Include(new PeriodoValidator()); // RN11, RN14

        // RN13: período completo, na forma escolhida.
        When(q => !Periodo.UsaInstantes(q), () =>
        {
            RuleFor(q => q.DataInicio).NotNull().WithMessage("Informe a data inicial do período.");
            RuleFor(q => q.DataFim).NotNull().WithMessage("Informe a data final do período.");
        });

        When(q => Periodo.UsaInstantes(q) && !Periodo.UsaDias(q), () =>
        {
            RuleFor(q => q.Inicio).NotNull().WithMessage("Informe o início do período.");
            RuleFor(q => q.Fim).NotNull().WithMessage("Informe o fim do período.");
        });
    }
}
