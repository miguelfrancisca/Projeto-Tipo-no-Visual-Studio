using System.Reflection;
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
    {
        // RN14: DateTimeOffset na URL só com offset explícito (nunca o fuso do servidor).
        options.ModelBinderProviders.Insert(0, new DateTimeOffsetComOffsetModelBinderProvider());
        ErrosDeBinding.ConfigurarMensagens(options.ModelBindingMessageProvider);
    })
    // Não expõe nomes de tipos .NET nem posições do leitor nas mensagens de JSON inválido (spec §5).
    .AddJsonOptions(options => options.AllowInputFormatterExceptionMessages = false)
    .ConfigureApiBehaviorOptions(options =>
        // Erros de binding (JSON, corpo, query, rota) no contrato do 400 da spec §5.
        options.InvalidModelStateResponseFactory = ErrosDeBinding.CriarResposta);
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
