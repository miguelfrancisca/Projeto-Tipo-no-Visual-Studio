using System.Diagnostics;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MiniGestaoPedidos.Application.Common.Exceptions;
using MiniGestaoPedidos.Domain.Exceptions;

namespace MiniGestaoPedidos.Api.Errors;

/// <summary>
/// Traduz exceções conhecidas em ProblemDetails (RFC 7807) com o status da spec §5.
/// Controllers não fazem try/catch.
/// </summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>Título e detalhe de todo 400 (spec §5), também usados por <see cref="ErrosDeBinding"/>.</summary>
    internal const string TituloRequisicaoInvalida = "Requisição inválida";
    internal const string DetalheRequisicaoInvalida = "Um ou mais campos são inválidos.";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ValidationException ex => Validacao(ex, httpContext),
            NaoEncontradoException ex => Problema(StatusCodes.Status404NotFound, "Recurso não encontrado", ex.Message),
            ConflitoConcorrenciaException ex => Problema(StatusCodes.Status409Conflict, "Conflito de concorrência", ex.Message),
            EstoqueInsuficienteException ex => EstoqueInsuficiente(ex),
            DomainException ex => Problema(StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada", ex.Message),
            RegraNegocioException ex => Problema(StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada", ex.Message),
            _ => null,
        };

        if (problem is null)
        {
            logger.LogError(exception, "Erro não tratado ao processar {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
            return false; // o middleware padrão devolve 500 sem expor detalhes
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ValidationProblemDetails Validacao(ValidationException ex, HttpContext httpContext)
    {
        var problem = new ValidationProblemDetails(ex.Errors
            .GroupBy(e => CamelCase(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray()))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = TituloRequisicaoInvalida,
            Detail = DetalheRequisicaoInvalida,
        };

        // Spec §5: todo 400 tem traceId. Só aqui (e não em CustomizeProblemDetails) para não mudar 404/409/422.
        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        return problem;
    }

    private static ProblemDetails EstoqueInsuficiente(EstoqueInsuficienteException ex)
    {
        var problem = Problema(StatusCodes.Status422UnprocessableEntity, "Estoque insuficiente", ex.Message);
        problem.Extensions["produtoId"] = ex.ProdutoId;
        problem.Extensions["disponivel"] = ex.Disponivel;
        problem.Extensions["solicitado"] = ex.Solicitado;
        return problem;
    }

    private static ProblemDetails Problema(int status, string titulo, string detalhe) =>
        new() { Status = status, Title = titulo, Detail = detalhe };

    /// <summary>"Itens[0].Quantidade" → "itens[0].quantidade", alinhado ao JSON camelCase da API.</summary>
    internal static string CamelCase(string caminho) =>
        string.Join('.', caminho.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}
