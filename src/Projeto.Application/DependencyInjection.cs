using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Projeto.Application.Pedidos;

namespace Projeto.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IPedidoService, PedidoService>();

        return services;
    }
}
