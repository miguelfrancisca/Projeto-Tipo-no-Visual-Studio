using Microsoft.AspNetCore.Mvc;
using MiniGestaoPedidos.Application.Relatorios;

namespace MiniGestaoPedidos.Api.Controllers;

[ApiController]
[Route("relatorios")]
[Produces("application/json")]
public sealed class RelatoriosController(IRelatorioService relatorioService) : ControllerBase
{
    /// <summary>Os 5 clientes com maior valor total de pedidos no período (spec §5.3).</summary>
    [HttpGet("top-clientes")]
    [ProducesResponseType<IReadOnlyList<TopClienteResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TopClienteResponse>>> TopClientes(
        [FromQuery] TopClientesQuery query, CancellationToken cancellationToken) =>
        Ok(await relatorioService.TopClientesAsync(query, cancellationToken));
}
