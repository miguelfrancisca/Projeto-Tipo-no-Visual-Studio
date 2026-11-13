namespace MiniGestaoPedidos.Api.IntegrationTests.Infra;

/// <summary>Um único container SQL Server para todas as classes de teste (subir um por classe custaria ~15 s cada).</summary>
[CollectionDefinition(Nome)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Nome = "api";
}
