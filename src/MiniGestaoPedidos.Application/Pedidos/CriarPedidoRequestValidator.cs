using FluentValidation;

namespace MiniGestaoPedidos.Application.Pedidos;

/// <summary>Validações de formato da criação de pedido (HTTP 400).</summary>
public sealed class CriarPedidoRequestValidator : AbstractValidator<CriarPedidoRequest>
{
    public CriarPedidoRequestValidator()
    {
        RuleFor(r => r.ClienteId)
            .GreaterThan(0).WithMessage("Informe um cliente válido.");

        RuleFor(r => r.Itens)
            .NotEmpty().WithMessage("O pedido deve ter pelo menos um item."); // RN02

        RuleForEach(r => r.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.ProdutoId)
                .GreaterThan(0).WithMessage("Informe um produto válido.");

            item.RuleFor(i => i.Quantidade)
                .GreaterThan(0).WithMessage("A quantidade deve ser maior que zero."); // RN03
        });

        RuleFor(r => r.Itens)
            .Must(itens => itens!.Select(i => i.ProdutoId).Distinct().Count() == itens!.Count)
            .When(r => r.Itens is { Count: > 0 })
            .WithMessage("Um mesmo produto não pode aparecer em mais de um item; some as quantidades."); // RN05
    }
}
