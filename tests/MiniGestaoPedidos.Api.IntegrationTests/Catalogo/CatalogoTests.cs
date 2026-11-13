using System.Net.Http.Json;
using MiniGestaoPedidos.Api.IntegrationTests.Infra;
using MiniGestaoPedidos.Application.Catalogo;

namespace MiniGestaoPedidos.Api.IntegrationTests.Catalogo;

/// <summary>Endpoints de apoio ao frontend (spec §5.4).</summary>
public sealed class CatalogoTests(ApiFactory factory) : TesteDeApi(factory)
{
    [Fact]
    public async Task GetClientes_RetornaOrdenadoPorNome()
    {
        await Banco.InserirClienteAsync("Zeca");
        await Banco.InserirClienteAsync("Ana");

        var clientes = await Http.GetFromJsonAsync<List<ClienteResponse>>("/clientes");

        Assert.Equal(["Ana", "Zeca"], clientes!.Select(c => c.Nome));
    }

    [Fact]
    public async Task GetProdutos_RetornaPrecoEEstoqueAtuais()
    {
        var id = await Banco.InserirProdutoAsync(preco: 19.90m, estoque: 7, nome: "Mousepad");

        var produtos = await Http.GetFromJsonAsync<List<ProdutoResponse>>("/produtos");

        var produto = Assert.Single(produtos!);
        Assert.Equal((id, 19.90m, 7), (produto.Id, produto.Preco, produto.Estoque));
    }
}
