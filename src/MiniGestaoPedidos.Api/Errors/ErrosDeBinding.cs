using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace MiniGestaoPedidos.Api.Errors;

/// <summary>
/// Erros de binding (JSON, corpo, query, rota) no contrato do 400 da spec §5.
/// <c>type</c> e <c>traceId</c> vêm do <see cref="ProblemDetailsFactory"/> do MVC; as chaves viram o
/// nome público do campo em camelCase e as mensagens ficam em português, sem nada interno do .NET.
/// </summary>
internal static class ErrosDeBinding
{
    internal const string ChaveCorpo = "corpo";
    internal const string MensagemCorpoInvalido = "O corpo da requisição está vazio ou não é um JSON válido.";
    internal const string MensagemValorInvalido = "Valor inválido ou em formato incorreto.";
    private const string MensagemValorObrigatorio = "Informe um valor para este campo.";
    private const string MensagemNumero = "O valor deve ser um número.";

    /// <summary>Mensagens do model binding em português, sem o nome interno (PascalCase) da propriedade.</summary>
    public static void ConfigurarMensagens(DefaultModelBindingMessageProvider mensagens)
    {
        mensagens.SetAttemptedValueIsInvalidAccessor((valor, _) => ValorNaoValido(valor));
        mensagens.SetNonPropertyAttemptedValueIsInvalidAccessor(ValorNaoValido);
        mensagens.SetValueIsInvalidAccessor(ValorNaoValido);
        mensagens.SetUnknownValueIsInvalidAccessor(_ => MensagemValorInvalido);
        mensagens.SetNonPropertyUnknownValueIsInvalidAccessor(() => MensagemValorInvalido);
        mensagens.SetValueMustBeANumberAccessor(_ => MensagemNumero);
        mensagens.SetNonPropertyValueMustBeANumberAccessor(() => MensagemNumero);
        mensagens.SetMissingBindRequiredValueAccessor(_ => MensagemValorObrigatorio);
        mensagens.SetMissingKeyOrValueAccessor(() => MensagemValorObrigatorio);
        mensagens.SetValueMustNotBeNullAccessor(_ => MensagemValorObrigatorio);
        mensagens.SetMissingRequestBodyRequiredValueAccessor(() => MensagemCorpoInvalido);
    }

    /// <summary>Usado como <c>InvalidModelStateResponseFactory</c>.</summary>
    public static IActionResult CriarResposta(ActionContext contexto)
    {
        var fabrica = contexto.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problema = fabrica.CreateValidationProblemDetails(
            contexto.HttpContext,
            Normalizar(contexto),
            StatusCodes.Status400BadRequest,
            GlobalExceptionHandler.TituloRequisicaoInvalida,
            detail: GlobalExceptionHandler.DetalheRequisicaoInvalida);

        // JsonResult, e não ObjectResult: o [Produces("application/json")] dos controllers substitui o
        // Content-Type de qualquer ObjectResult. JsonResult também ignora o Accept (nunca vira 406).
        return new JsonResult(problema)
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentType = "application/problem+json",
        };
    }

    /// <summary>
    /// Reescreve o ModelState com chaves públicas:
    /// "" e "$" (corpo vazio / JSON inválido) → "corpo"; "$.itens[0].quantidade" → "itens[0].quantidade";
    /// o nome do parâmetro C# de corpo (ex.: "request") só aparece, como "corpo", se for o único erro;
    /// chaves de query/rota → camelCase, com a mensagem do binding (já em português).
    /// </summary>
    private static ModelStateDictionary Normalizar(ActionContext contexto)
    {
        var parametrosDeCorpo = contexto.ActionDescriptor.Parameters
            .Where(p => p.BindingInfo?.BindingSource == BindingSource.Body)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);

        var erros = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var erroNoParametroDeCorpo = false;

        foreach (var (chave, entrada) in contexto.ModelState)
        {
            if (entrada.Errors.Count == 0)
            {
                continue;
            }

            if (parametrosDeCorpo.Contains(chave))
            {
                erroNoParametroDeCorpo = true;
            }
            else if (chave.Length == 0 || chave == "$")
            {
                Adicionar(erros, ChaveCorpo, MensagemCorpoInvalido);
            }
            else if (chave.StartsWith("$.", StringComparison.Ordinal))
            {
                Adicionar(erros, GlobalExceptionHandler.CamelCase(chave[2..]), MensagemValorInvalido);
            }
            else
            {
                foreach (var erro in entrada.Errors)
                {
                    var mensagem = string.IsNullOrWhiteSpace(erro.ErrorMessage) ? MensagemValorInvalido : erro.ErrorMessage;
                    Adicionar(erros, GlobalExceptionHandler.CamelCase(chave), mensagem);
                }
            }
        }

        if (erros.Count == 0 && erroNoParametroDeCorpo)
        {
            Adicionar(erros, ChaveCorpo, MensagemCorpoInvalido);
        }

        var modelState = new ModelStateDictionary();
        foreach (var (chave, mensagens) in erros)
        {
            foreach (var mensagem in mensagens)
            {
                modelState.AddModelError(chave, mensagem);
            }
        }

        return modelState;
    }

    private static void Adicionar(Dictionary<string, List<string>> erros, string chave, string mensagem)
    {
        if (!erros.TryGetValue(chave, out var mensagens))
        {
            erros[chave] = mensagens = [];
        }

        if (!mensagens.Contains(mensagem, StringComparer.Ordinal))
        {
            mensagens.Add(mensagem);
        }
    }

    private static string ValorNaoValido(string valor) => $"O valor '{valor}' não é válido para este campo.";
}
