using Microsoft.AspNetCore.Mvc;
using MiniGestaoPedidos.Application.Catalogo;

namespace MiniGestaoPedidos.Api.Controllers;

[ApiController]
[Route("produtos")]
[Produces("application/json")]
public sealed class ProdutosController(ICatalogoQueries catalogo) : ControllerBase
{
    /// <summary>Lista de produtos com preço e estoque atuais (apoio ao frontend).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProdutoResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProdutoResponse>>> Listar(CancellationToken cancellationToken) =>
        Ok(await catalogo.ListarProdutosAsync(cancellationToken));
}
