using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MiniGestaoPedidos.Api.IntegrationTests.Infra;

namespace MiniGestaoPedidos.Api.IntegrationTests.Erros;

/// <summary>
/// CA23 — todo 400 segue o formato da spec §5, qualquer que seja a origem do erro:
/// validação (FluentValidation, via GlobalExceptionHandler) ou binding (JSON, corpo, query).
/// Cada cenário exige a chave exata e a mensagem exata, em português.
/// </summary>
public sealed class ContratoErro400Tests(ApiFactory factory) : TesteDeApi(factory)
{
    private const string TipoBadRequest = "https://tools.ietf.org/html/rfc9110#section-15.5.1";

    /// <summary>As seis propriedades de primeiro nível do 400 (spec §5), em ordem ordinal.</summary>
    private static readonly string[] PropriedadesDoContrato =
        new[] { "type", "title", "status", "detail", "errors", "traceId" }.Order(StringComparer.Ordinal).ToArray();

    // Mensagens de binding definidas por este contrato (spec §5: em português, sem nada interno).
    private const string MensagemCorpoInvalido = "O corpo da requisição está vazio ou não é um JSON válido.";
    private const string MensagemValorInvalido = "Valor inválido ou em formato incorreto.";

    // Mensagens já existentes (FluentValidation e binder de DateTimeOffset), copiadas do código.
    private const string MensagemSemItens = "O pedido deve ter pelo menos um item.";
    private const string MensagemPagina = "A página deve ser maior ou igual a 1.";
    private const string MensagemPeriodoMisturado =
        "Informe o período com inicio/fim (instantes com offset) ou com dataInicio/dataFim (dias em UTC), não com os dois.";
    private const string MensagemSemOffset =
        "Valor '2025-03-10T00:00:00' inválido. " +
        "Use data e hora ISO 8601 com offset explícito, ex.: 2025-03-10T00:00:00-03:00 ou 2025-03-10T03:00:00Z. " +
        "Na URL, codifique '+' como %2B (ex.: %2B05:30).";

    /// <summary>
    /// Trechos de mensagens internas que o ASP.NET e o System.Text.Json produzem hoje
    /// (copiados de respostas reais). Nenhuma mensagem do contrato pode contê-los.
    /// </summary>
    private static readonly string[] MensagensInternasConhecidas =
    [
        "The request field is required",            // [ApiController]: parâmetro de corpo obrigatório
        "A non-empty request body is required",     // corpo vazio
        "is not valid for",                         // "The value 'abc' is not valid for ClienteId."
        "The JSON value could not be converted to", // tipo errado no JSON
        "Expected start of a property name",        // JSON sintaticamente inválido
        "The input was not valid",                  // mensagem genérica do MVC
        "The supplied value is invalid",            // mensagem genérica do model binding
        "LineNumber",
        "BytePositionInLine",
        "Path: $",
        "MiniGestaoPedidos.",                       // nome de tipo .NET do projeto
        "System.",                                  // nome de tipo .NET do framework
    ];

    /// <summary>caso, método, URL, corpo (null = sem corpo), chave esperada, mensagem esperada.</summary>
    public static IEnumerable<object?[]> CasosDoCA23 =>
    [
        [
            "A: lista de itens vazia", "POST", "/pedidos",
            """{"clienteId":1,"itens":[]}""",
            "itens", MensagemSemItens,
        ],
        [
            "B: pagina=0", "GET", "/pedidos?pagina=0",
            null,
            "pagina", MensagemPagina,
        ],
        [
            "C: JSON sintaticamente inválido", "POST", "/pedidos",
            """{"clienteId":1,""",
            "corpo", MensagemCorpoInvalido,
        ],
        [
            "C2: tipo errado num campo do corpo", "POST", "/pedidos",
            """{"clienteId":"abc","itens":[]}""",
            "clienteId", MensagemValorInvalido,
        ],
        [
            "D: tipo errado num item", "POST", "/pedidos",
            """{"clienteId":1,"itens":[{"produtoId":1,"quantidade":"x"}]}""",
            "itens[0].quantidade", MensagemValorInvalido,
        ],
        [
            "E: corpo vazio", "POST", "/pedidos",
            "",
            "corpo", MensagemCorpoInvalido,
        ],
        [
            "E2: corpo null", "POST", "/pedidos",
            "null",
            "corpo", MensagemCorpoInvalido,
        ],
        [
            "F: clienteId=abc na query", "GET", "/pedidos?clienteId=abc",
            null,
            "clienteId", "O valor 'abc' não é válido para este campo.",
        ],
        [
            "G: inicio sem offset", "GET", "/pedidos?inicio=2025-03-10T00:00:00",
            null,
            "inicio", MensagemSemOffset,
        ],
        [
            "H: período misturado", "GET", "/pedidos?dataInicio=2025-03-10&fim=2025-03-11T00:00:00Z",
            null,
            "periodo", MensagemPeriodoMisturado,
        ],
    ];

    [Theory]
    [MemberData(nameof(CasosDoCA23))]
    public async Task Requisicao_CasoDoCA23_Retorna400NoFormatoDaSpec(
        string caso, string metodo, string url, string? corpo, string chaveEsperada, string mensagemEsperada)
    {
        using var requisicao = CriarRequisicao(metodo, url, corpo);

        using var resposta = await Http.SendAsync(requisicao);

        await AssertContrato400Async(resposta, caso, chaveEsperada, mensagemEsperada);
    }

    [Theory]
    [InlineData("/pedidos?clienteId=abc", "clienteId", "O valor 'abc' não é válido para este campo.")] // binding
    [InlineData("/pedidos?pagina=0", "pagina", MensagemPagina)]                                         // validação
    public async Task Requisicao_ComAcceptApplicationJson_Retorna400NoFormatoDaSpecENao406(
        string url, string chaveEsperada, string mensagemEsperada)
    {
        using var requisicao = CriarRequisicao("GET", url, corpo: null);
        requisicao.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var resposta = await Http.SendAsync(requisicao);

        await AssertContrato400Async(resposta, $"Accept: application/json em {url}", chaveEsperada, mensagemEsperada);
    }

    private static HttpRequestMessage CriarRequisicao(string metodo, string url, string? corpo)
    {
        var requisicao = new HttpRequestMessage(new HttpMethod(metodo), url);
        if (corpo is not null)
        {
            requisicao.Content = new StringContent(corpo, Encoding.UTF8, "application/json");
        }

        return requisicao;
    }

    private static async Task AssertContrato400Async(
        HttpResponseMessage resposta, string caso, string chaveEsperada, string mensagemEsperada)
    {
        var corpo = await resposta.Content.ReadAsStringAsync();
        var contexto = $"[{caso}] resposta: {corpo}";

        // Status e Content-Type.
        Assert.True(resposta.StatusCode == HttpStatusCode.BadRequest, $"status {(int)resposta.StatusCode} {contexto}");
        Assert.True(
            resposta.Content.Headers.ContentType?.MediaType == "application/problem+json",
            $"Content-Type '{resposta.Content.Headers.ContentType}' {contexto}");

        // Campos do ProblemDetails.
        using var json = JsonDocument.Parse(corpo);
        var raiz = json.RootElement;
        Assert.True(Texto(raiz, "type") == TipoBadRequest, $"type '{Texto(raiz, "type")}' {contexto}");
        Assert.True(Texto(raiz, "title") == "Requisição inválida", $"title '{Texto(raiz, "title")}' {contexto}");
        Assert.True(raiz.TryGetProperty("status", out var status) && status.GetInt32() == 400, $"status no corpo {contexto}");
        Assert.True(Texto(raiz, "detail") == "Um ou mais campos são inválidos.", $"detail '{Texto(raiz, "detail")}' {contexto}");
        Assert.False(string.IsNullOrWhiteSpace(Texto(raiz, "traceId")), $"traceId ausente {contexto}");

        // Conjunto exato de propriedades de primeiro nível (a ordem não faz parte do contrato).
        var propriedades = raiz.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal).ToArray();
        Assert.True(
            propriedades.SequenceEqual(PropriedadesDoContrato),
            $"propriedades esperadas [{string.Join(", ", PropriedadesDoContrato)}], obtidas [{string.Join(", ", propriedades)}] {contexto}");

        // Chaves: igualdade exata com o nome público em camelCase. Isso também garante que não
        // sobra nenhuma chave "request", "" ou começada por "$".
        var erros = raiz.GetProperty("errors").EnumerateObject().ToList();
        var chaves = erros.Select(e => e.Name).ToArray();
        Assert.True(
            chaves.SequenceEqual([chaveEsperada]),
            $"chaves esperadas [{chaveEsperada}], obtidas [{string.Join(", ", chaves.Select(c => $"\"{c}\""))}] {contexto}");

        // Mensagens: primeiro rejeita as internas conhecidas (falha mais clara), depois exige a exata.
        var mensagens = erros[0].Value.EnumerateArray().Select(m => m.GetString() ?? string.Empty).ToArray();
        foreach (var mensagem in mensagens)
        {
            foreach (var interna in MensagensInternasConhecidas)
            {
                Assert.False(
                    mensagem.Contains(interna, StringComparison.Ordinal),
                    $"mensagem interna do framework ('{interna}'): {mensagem} {contexto}");
            }
        }

        Assert.True(
            mensagens.SequenceEqual([mensagemEsperada]),
            $"mensagens esperadas [\"{mensagemEsperada}\"], obtidas [{string.Join(", ", mensagens.Select(m => $"\"{m}\""))}] {contexto}");
    }

    private static string? Texto(JsonElement raiz, string propriedade) =>
        raiz.TryGetProperty(propriedade, out var valor) && valor.ValueKind == JsonValueKind.String ? valor.GetString() : null;
}
