using System.Net;
using System.Text;
using System.Text.Json;
using MiniGestaoPedidos.Api.IntegrationTests.Infra;

namespace MiniGestaoPedidos.Api.IntegrationTests.Erros;

/// <summary>
/// Caracterização: fixa o formato ATUAL do 404 e do 415, que por decisão NÃO seguem o contrato
/// do 400 (spec §5). Se um destes testes falhar, um desses formatos mudou sem decisão explícita.
/// </summary>
public sealed class FormatoErros404E415Tests(ApiFactory factory) : TesteDeApi(factory)
{
    [Fact]
    public async Task RotaInexistente_Get_Retorna404NoFormatoPadraoDoAspNet()
    {
        using var resposta = await Http.GetAsync("/rota-inexistente");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        var json = await LerJsonAsync(resposta);
        Assert.Equal(["type", "title", "status"], Propriedades(json));
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.5", json.GetProperty("type").GetString());
        Assert.Equal("Not Found", json.GetProperty("title").GetString());
        Assert.Equal(404, json.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task PedidoInexistente_Get_Retorna404DoHandlerSemTraceId()
    {
        using var resposta = await Http.GetAsync("/pedidos/987654");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        var json = await LerJsonAsync(resposta);
        Assert.Equal(["type", "title", "status", "detail"], Propriedades(json));
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.5", json.GetProperty("type").GetString());
        Assert.Equal("Recurso não encontrado", json.GetProperty("title").GetString());
        Assert.Equal(404, json.GetProperty("status").GetInt32());
        Assert.Equal("Pedido 987654 não encontrado.", json.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task ContentTypeNaoSuportado_Post_Retorna415NoFormatoPadraoDoMvc()
    {
        using var conteudo = new StringContent("x", Encoding.UTF8, "text/plain");

        using var resposta = await Http.PostAsync("/pedidos", conteudo);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, resposta.StatusCode);
        Assert.Equal("application/json", resposta.Content.Headers.ContentType?.MediaType);
        var json = await LerJsonAsync(resposta);
        Assert.Equal(["type", "title", "status", "traceId"], Propriedades(json));
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.16", json.GetProperty("type").GetString());
        Assert.Equal("Unsupported Media Type", json.GetProperty("title").GetString());
        Assert.Equal(415, json.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("traceId").GetString()));
    }

    private static List<string> Propriedades(JsonElement json) =>
        json.EnumerateObject().Select(p => p.Name).ToList();
}
