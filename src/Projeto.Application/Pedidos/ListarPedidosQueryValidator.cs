using FluentValidation;
using Projeto.Application.Common;

namespace Projeto.Application.Pedidos;

public sealed class ListarPedidosQueryValidator : AbstractValidator<ListarPedidosQuery>
{
    public ListarPedidosQueryValidator()
    {
        RuleFor(q => q.ClienteId)
            .GreaterThan(0).When(q => q.ClienteId.HasValue)
            .WithMessage("Informe um cliente válido.");

        Include(new PeriodoValidator()); // RN11, RN14

        RuleFor(q => q.Pagina)
            .GreaterThanOrEqualTo(1).WithMessage("A página deve ser maior ou igual a 1."); // RN12

        RuleFor(q => q.TamanhoPagina)
            .InclusiveBetween(1, ListarPedidosQuery.TamanhoPaginaMaximo)
            .WithMessage($"O tamanho da página deve estar entre 1 e {ListarPedidosQuery.TamanhoPaginaMaximo}."); // RN12
    }
}
