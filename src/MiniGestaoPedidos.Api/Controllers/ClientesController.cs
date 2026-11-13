using Microsoft.AspNetCore.Mvc;
using MiniGestaoPedidos.Application.Catalogo;

namespace MiniGestaoPedidos.Api.Controllers;

[ApiController]
[Route("clientes")]
[Produces("application/json")]
public sealed class ClientesController(ICatalogoQueries catalogo) : ControllerBase
{
    /// <summary>Lista de clientes, ordenada por nome (apoio ao frontend).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ClienteResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClienteResponse>>> Listar(CancellationToken cancellationToken) =>
        Ok(await catalogo.ListarClientesAsync(cancellationToken));
}
