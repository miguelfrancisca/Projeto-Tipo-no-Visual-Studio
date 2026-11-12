using System.Reflection;
using Projeto.Api.Errors;
using Projeto.Application;
using Projeto.Infrastructure;
using Projeto.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("PedidosDb")
    ?? throw new InvalidOperationException("Connection string 'PedidosDb' não configurada.");

builder.Services
    .AddApplication()
    .AddInfrastructure(connectionString);

builder.Services.AddControllers();
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
