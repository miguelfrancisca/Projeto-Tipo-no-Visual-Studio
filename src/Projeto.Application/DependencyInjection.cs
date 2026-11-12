using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Projeto.Application.Pedidos;
using Projeto.Application.Relatorios;

namespace Projeto.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IPedidoService, PedidoService>();
        services.AddScoped<IRelatorioService, RelatorioService>();

        return services;
    }
}
