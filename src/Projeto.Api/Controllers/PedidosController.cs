using Microsoft.AspNetCore.Mvc;
using Projeto.Application.Common;
using Projeto.Application.Pedidos;

namespace Projeto.Api.Controllers;

[ApiController]
[Route("pedidos")]
[Produces("application/json")]
public sealed class PedidosController(IPedidoService pedidoService) : ControllerBase
{
    /// <summary>Cria um pedido com itens, validando cliente e estoque (spec §5.1).</summary>
    [HttpPost]
    [ProducesResponseType<PedidoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PedidoResponse>> Criar(CriarPedidoRequest request, CancellationToken cancellationToken)
    {
        var pedido = await pedidoService.CriarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(ObterPorId), new { id = pedido.Id }, pedido);
    }

    /// <summary>Lista pedidos com filtros por cliente e período, paginada (spec §5.2).</summary>
    [HttpGet]
    [ProducesResponseType<PaginaResultado<PedidoResumoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginaResultado<PedidoResumoResponse>>> Listar(
        [FromQuery] ListarPedidosQuery query, CancellationToken cancellationToken) =>
        Ok(await pedidoService.ListarAsync(query, cancellationToken));

    /// <summary>Detalhe de um pedido com itens (spec §5.4).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<PedidoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PedidoResponse>> ObterPorId(int id, CancellationToken cancellationToken) =>
        Ok(await pedidoService.ObterPorIdAsync(id, cancellationToken));
}
