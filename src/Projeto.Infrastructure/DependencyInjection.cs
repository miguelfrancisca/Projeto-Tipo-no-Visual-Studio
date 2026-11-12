using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Projeto.Application.Abstractions;
using Projeto.Application.Catalogo;
using Projeto.Application.Pedidos;
using Projeto.Application.Relatorios;
using Projeto.Infrastructure.Persistence;
using Projeto.Infrastructure.Persistence.Queries;
using Projeto.Infrastructure.Persistence.Repositories;

namespace Projeto.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

        // Escrita
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IProdutoRepository, ProdutoRepository>();
        services.AddScoped<IPedidoRepository, PedidoRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Leitura
        services.AddScoped<IPedidoQueries, PedidoQueries>();
        services.AddScoped<IRelatorioQueries, RelatorioQueries>();
        services.AddScoped<ICatalogoQueries, CatalogoQueries>();

        return services;
    }
}
