using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MiniGestaoPedidos.Api.Binding;
using MiniGestaoPedidos.Api.Errors;
using MiniGestaoPedidos.Application;
using MiniGestaoPedidos.Infrastructure;
using MiniGestaoPedidos.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("PedidosDb")
    ?? throw new InvalidOperationException("Connection string 'PedidosDb' não configurada.");

builder.Services
    .AddApplication()
    .AddInfrastructure(connectionString);

builder.Services
    .AddControllers(options =>
        // RN14: DateTimeOffset na URL só com offset explícito (nunca o fuso do servidor).
        options.ModelBinderProviders.Insert(0, new DateTimeOffsetComOffsetModelBinderProvider()))
    .ConfigureApiBehaviorOptions(options =>
        // Erros de binding (formato da URL/JSON) no mesmo formato e idioma dos erros de validação.
        options.InvalidModelStateResponseFactory = context =>
            new BadRequestObjectResult(new ValidationProblemDetails(
                context.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .ToDictionary(
                        e => JsonNamingPolicy.CamelCase.ConvertName(e.Key), // "Inicio" → "inicio", como no GlobalExceptionHandler
                        e => e.Value!.Errors.Select(erro => erro.ErrorMessage).ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Requisição inválida",
                Detail = "Um ou mais campos são inválidos.",
            }));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("sqlserver");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    options.IncludeXmlComments(xml);
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Swagger habilitado em todos os ambientes: é um projeto de avaliação, sem dados sensíveis.
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

/// <summary>Exposto para a WebApplicationFactory dos testes de integração.</summary>
public partial class Program;
