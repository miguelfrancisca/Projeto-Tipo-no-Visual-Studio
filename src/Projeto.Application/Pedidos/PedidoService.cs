using FluentValidation;
using Projeto.Application.Abstractions;
using Projeto.Application.Common;
using Projeto.Application.Common.Exceptions;
using Projeto.Domain.Entities;

namespace Projeto.Application.Pedidos;

public sealed class PedidoService(
    IClienteRepository clientes,
    IProdutoRepository produtos,
    IPedidoRepository pedidos,
    IPedidoQueries pedidoQueries,
    IUnitOfWork unitOfWork,
    IValidator<CriarPedidoRequest> criarValidator,
    IValidator<ListarPedidosQuery> listarValidator,
    TimeProvider relogio) : IPedidoService
{
    public async Task<PedidoResponse> CriarAsync(CriarPedidoRequest request, CancellationToken cancellationToken)
    {
        // 400 — formato (RN02, RN03, RN05). Nada é consultado se a request for inválida.
        await criarValidator.ValidateAndThrowAsync(request, cancellationToken);
        var itensRequest = request.Itens!;

        // 422 — RN01
        var cliente = await clientes.ObterPorIdAsync(request.ClienteId, cancellationToken)
            ?? throw new RegraNegocioException($"Cliente {request.ClienteId} não encontrado.");

        // 422 — RN04
        var ids = itensRequest.Select(i => i.ProdutoId).ToList();
        var encontrados = (await produtos.ObterPorIdsAsync(ids, cancellationToken)).ToDictionary(p => p.Id);
        var faltantes = ids.Where(id => !encontrados.ContainsKey(id)).ToList();
        if (faltantes.Count > 0)
        {
            throw new RegraNegocioException($"Produto(s) não encontrado(s): {string.Join(", ", faltantes)}.");
        }

        // Domínio: RN06 (estoque), RN07 (preço congelado), RN09 (total). RN08: data do servidor.
        var pedido = Pedido.Criar(
            cliente,
            AgoraEmMilissegundos(),
            itensRequest.Select(i => new ItemSolicitado(encontrados[i.ProdutoId], i.Quantidade)).ToList());

        pedidos.Adicionar(pedido);
        await unitOfWork.SalvarAlteracoesAsync(cancellationToken); // RN10: atômico + concorrência otimista

        return PedidoResponse.De(pedido);
    }

    public async Task<PedidoResponse> ObterPorIdAsync(int id, CancellationToken cancellationToken) =>
        await pedidoQueries.ObterPorIdAsync(id, cancellationToken)
            ?? throw new NaoEncontradoException($"Pedido {id} não encontrado.");

    public async Task<PaginaResultado<PedidoResumoResponse>> ListarAsync(
        ListarPedidosQuery query, CancellationToken cancellationToken)
    {
        await listarValidator.ValidateAndThrowAsync(query, cancellationToken); // RN11, RN12
        return await pedidoQueries.ListarAsync(query, cancellationToken);
    }

    /// <summary>
    /// A coluna é datetime2(3): truncar aqui garante que a resposta do POST
    /// mostre exatamente a mesma data que um GET posterior.
    /// </summary>
    private DateTime AgoraEmMilissegundos()
    {
        var agora = relogio.GetUtcNow().UtcDateTime;
        return agora.AddTicks(-(agora.Ticks % TimeSpan.TicksPerMillisecond));
    }
}
