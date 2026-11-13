using MiniGestaoPedidos.Domain.Exceptions;

namespace MiniGestaoPedidos.Domain.Entities;

public class Pedido
{
    private readonly List<ItemPedido> _itens = [];

    // Usado pelo EF Core e por Criar.
    protected Pedido()
    {
    }

    public int Id { get; private set; }

    public int ClienteId { get; private set; }

    public Cliente Cliente { get; private set; } = null!;

    public DateTime DataPedido { get; private set; }

    public decimal ValorTotal { get; private set; }

    public IReadOnlyCollection<ItemPedido> Itens => _itens.AsReadOnly();

    /// <summary>
    /// Cria um pedido e baixa o estoque dos produtos.
    /// Todas as regras são validadas ANTES de qualquer baixa: se um item falhar,
    /// nenhum estoque é alterado (CA06).
    /// </summary>
    public static Pedido Criar(Cliente cliente, DateTime dataPedido, IReadOnlyCollection<ItemSolicitado> itens)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        ArgumentNullException.ThrowIfNull(itens);

        if (itens.Count == 0)
        {
            throw new DomainException("O pedido deve ter pelo menos um item."); // RN02
        }

        var repetido = itens.GroupBy(i => i.Produto.Id).FirstOrDefault(g => g.Count() > 1);
        if (repetido is not null)
        {
            throw new DomainException(
                $"O produto {repetido.Key} está repetido no pedido; informe-o uma única vez com a quantidade total."); // RN05
        }

        foreach (var item in itens)
        {
            if (item.Quantidade <= 0)
            {
                throw new DomainException(
                    $"A quantidade do produto '{item.Produto.Nome}' deve ser maior que zero."); // RN03
            }

            if (!item.Produto.TemEstoque(item.Quantidade))
            {
                throw new EstoqueInsuficienteException(
                    item.Produto.Id, item.Produto.Nome, item.Produto.Estoque, item.Quantidade); // RN06
            }
        }

        var pedido = new Pedido
        {
            Cliente = cliente,
            ClienteId = cliente.Id,
            DataPedido = dataPedido,
        };

        foreach (var item in itens)
        {
            item.Produto.BaixarEstoque(item.Quantidade);
            pedido._itens.Add(new ItemPedido(item.Produto, item.Quantidade));
        }

        pedido.ValorTotal = pedido._itens.Sum(i => i.Subtotal); // RN09
        return pedido;
    }
}
