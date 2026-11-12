using System.Net;
using System.Net.Http.Json;
using Projeto.Api.IntegrationTests.Infra;
using Projeto.Application.Pedidos;

namespace Projeto.Api.IntegrationTests.Pedidos;

/// <summary>POST /pedidos — critérios de aceite CA01 a CA08 e RN10 (docs/spec.md §8).</summary>
public sealed class CriarPedidoTests(ApiFactory factory) : TesteDeApi(factory)
{
    [Fact]
    public async Task CA01_DadosValidos_Retorna201ComLocationTotalEBaixaEstoque()
    {
        var cliente = await Banco.InserirClienteAsync("Ana Souza");
        var teclado = await Banco.InserirProdutoAsync(preco: 149.90m, estoque: 10, nome: "Teclado");
        var mouse = await Banco.InserirProdutoAsync(preco: 59.90m, estoque: 5, nome: "Mouse");

        var resposta = await Http.PostAsJsonAsync("/pedidos", new
        {
            clienteId = cliente,
            itens = new[] { new { produtoId = teclado, quantidade = 2 }, new { produtoId = mouse, quantidade = 1 } },
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var pedido = await LerAsync<PedidoResponse>(resposta);
        Assert.Equal($"/pedidos/{pedido.Id}", resposta.Headers.Location!.AbsolutePath);
        Assert.Equal(359.70m, pedido.ValorTotal);
        Assert.Equal("Ana Souza", pedido.ClienteNome);
        Assert.Equal(8, await Banco.EstoqueAsync(teclado));
        Assert.Equal(4, await Banco.EstoqueAsync(mouse));
        Assert.Equal(2, await Banco.ContarAsync("ItensPedido"));

        // O Location aponta para um recurso que devolve o mesmo pedido (inclusive a data, CA01 + RN08).
        var obtido = await Http.GetFromJsonAsync<PedidoResponse>(resposta.Headers.Location);
        Assert.Equal(pedido.DataPedido, obtido!.DataPedido);
        Assert.Equal(pedido.ValorTotal, obtido.ValorTotal);
    }

    [Fact]
    public async Task CA02_ClienteInexistente_Retorna422ENadaEGravado()
    {
        var produto = await Banco.InserirProdutoAsync(estoque: 10);

        var resposta = await Http.PostAsJsonAsync("/pedidos", new
        {
            clienteId = 999_999,
            itens = new[] { new { produtoId = produto, quantidade = 1 } },
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        Assert.Equal(0, await Banco.ContarAsync("Pedidos"));
        Assert.Equal(10, await Banco.EstoqueAsync(produto));
    }

    [Theory]
    [InlineData("""{"clienteId":1,"itens":[]}""")]
    [InlineData("""{"clienteId":1}""")]
    public async Task CA03_SemItens_Retorna400(string corpo)
    {
        var resposta = await Http.PostAsync("/pedidos", new StringContent(corpo, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var problema = await LerJsonAsync(resposta);
        Assert.True(problema.GetProperty("errors").TryGetProperty("itens", out _));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task CA04_QuantidadeNaoPositiva_Retorna400(int quantidade)
    {
        var cliente = await Banco.InserirClienteAsync();
        var produto = await Banco.InserirProdutoAsync();

        var resposta = await Http.PostAsJsonAsync("/pedidos", new
        {
            clienteId = cliente,
            itens = new[] { new { produtoId = produto, quantidade } },
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var problema = await LerJsonAsync(resposta);
        Assert.True(problema.GetProperty("errors").TryGetProperty("itens[0].quantidade", out _));
    }

    [Fact]
    public async Task CA05_ProdutoInexistente_Retorna422()
    {
        var cliente = await Banco.InserirClienteAsync();

        var resposta = await Http.PostAsJsonAsync("/pedidos", new
        {
            clienteId = cliente,
            itens = new[] { new { produtoId = 424_242, quantidade = 1 } },
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        Assert.Contains("424242", (await LerJsonAsync(resposta)).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task CA06_EstoqueInsuficiente_Retorna422SemGravarNemAlterarNenhumEstoque()
    {
        var cliente = await Banco.InserirClienteAsync();
        var comEstoque = await Banco.InserirProdutoAsync(estoque: 10);
        var semEstoque = await Banco.InserirProdutoAsync(estoque: 2, nome: "Headset");

        var resposta = await Http.PostAsJsonAsync("/pedidos", new
        {
            clienteId = cliente,
            itens = new[] { new { produtoId = comEstoque, quantidade = 5 }, new { produtoId = semEstoque, quantidade = 3 } },
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        var problema = await LerJsonAsync(resposta);
        Assert.Equal(semEstoque, problema.GetProperty("produtoId").GetInt32());
        Assert.Equal(2, problema.GetProperty("disponivel").GetInt32());
        Assert.Equal(0, await Banco.ContarAsync("Pedidos"));
        Assert.Equal(0, await Banco.ContarAsync("ItensPedido"));
        Assert.Equal(10, await Banco.EstoqueAsync(comEstoque));
        Assert.Equal(2, await Banco.EstoqueAsync(semEstoque));
    }

    [Fact]
    public async Task CA07_ProdutoRepetido_Retorna400()
    {
        var cliente = await Banco.InserirClienteAsync();
        var produto = await Banco.InserirProdutoAsync();

        var resposta = await Http.PostAsJsonAsync("/pedidos", new
        {
            clienteId = cliente,
            itens = new[] { new { produtoId = produto, quantidade = 1 }, new { produtoId = produto, quantidade = 2 } },
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task CA08_PrecoAlteradoDepois_NaoAfetaPedidoExistente()
    {
        var cliente = await Banco.InserirClienteAsync();
        var produto = await Banco.InserirProdutoAsync(preco: 50m);
        var criado = await LerAsync<PedidoResponse>(await Http.PostAsJsonAsync("/pedidos", new
        {
            clienteId = cliente,
            itens = new[] { new { produtoId = produto, quantidade = 2 } },
        }));

        await Banco.AlterarPrecoAsync(produto, 80m);
        var obtido = await Http.GetFromJsonAsync<PedidoResponse>($"/pedidos/{criado.Id}");

        Assert.Equal(50m, obtido!.Itens.Single().PrecoUnitario);
        Assert.Equal(100m, obtido.ValorTotal);
    }

    [Fact]
    public async Task RN10_PedidosConcorrentesDoUltimoItem_SoUmVendeEEstoqueNuncaFicaNegativo()
    {
        var cliente = await Banco.InserirClienteAsync();
        var produto = await Banco.InserirProdutoAsync(estoque: 1);
        var corpo = new { clienteId = cliente, itens = new[] { new { produtoId = produto, quantidade = 1 } } };

        var respostas = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Http.PostAsJsonAsync("/pedidos", corpo)));

        Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.Created);
        // Quem perdeu a corrida recebe 409 (conflito de rowversion) ou 422 (já leu estoque 0).
        Assert.All(
            respostas.Where(r => r.StatusCode != HttpStatusCode.Created),
            r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.Conflict, HttpStatusCode.UnprocessableEntity }));
        Assert.Equal(0, await Banco.EstoqueAsync(produto));
        Assert.Equal(1, await Banco.ContarAsync("Pedidos"));
    }
}
