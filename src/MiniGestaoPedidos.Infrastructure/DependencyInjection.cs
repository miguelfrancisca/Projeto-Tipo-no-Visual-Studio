using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniGestaoPedidos.Application.Abstractions;
using MiniGestaoPedidos.Application.Catalogo;
using MiniGestaoPedidos.Application.Pedidos;
using MiniGestaoPedidos.Application.Relatorios;
using MiniGestaoPedidos.Infrastructure.Persistence;
using MiniGestaoPedidos.Infrastructure.Persistence.Queries;
using MiniGestaoPedidos.Infrastructure.Persistence.Repositories;

namespace MiniGestaoPedidos.Infrastructure;

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
