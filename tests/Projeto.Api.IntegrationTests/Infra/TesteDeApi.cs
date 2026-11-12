using System.Net.Http.Json;
using System.Text.Json;

namespace Projeto.Api.IntegrationTests.Infra;

/// <summary>Base dos testes: banco limpo antes de cada teste, HttpClient e acesso direto ao banco.</summary>
[Collection(ApiCollection.Nome)]
public abstract class TesteDeApi(ApiFactory factory) : IAsyncLifetime
{
    protected HttpClient Http { get; } = factory.CreateClient();

    protected BancoDeTeste Banco { get; } = new(factory.ConnectionString);

    public Task InitializeAsync() => factory.LimparBancoAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    protected static async Task<T> LerAsync<T>(HttpResponseMessage resposta) =>
        (await resposta.Content.ReadFromJsonAsync<T>())!;

    protected static async Task<JsonElement> LerJsonAsync(HttpResponseMessage resposta) =>
        await resposta.Content.ReadFromJsonAsync<JsonElement>();
}
