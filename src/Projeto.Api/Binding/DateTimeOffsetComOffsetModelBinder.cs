using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Projeto.Api.Binding;

/// <summary>
/// RN14 — aceita <see cref="DateTimeOffset"/> na query string SOMENTE com offset explícito.
///
/// O binder padrão do ASP.NET aceita "2025-03-10T00:00:00" e, sem avisar, assume o fuso da
/// MÁQUINA DO SERVIDOR — o resultado mudaria conforme onde a API roda. Aqui o texto bruto é
/// validado antes de qualquer conversão: sem "Z" ou "±hh:mm", a requisição recebe 400.
/// Com o offset garantido pelo formato, o parse nunca consulta o fuso local.
/// </summary>
internal sealed partial class DateTimeOffsetComOffsetModelBinder : IModelBinder
{
    private const string MensagemFormato =
        "Use data e hora ISO 8601 com offset explícito, ex.: 2025-03-10T00:00:00-03:00 ou 2025-03-10T03:00:00Z. " +
        "Na URL, codifique '+' como %2B (ex.: %2B05:30).";

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valores = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valores == ValueProviderResult.None)
        {
            return Task.CompletedTask; // parâmetro ausente → null (período aberto)
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valores);
        var chave = bindingContext.ModelName; // a resposta 400 converte as chaves para camelCase (Program.cs)

        if (valores.Length > 1)
        {
            return Falhar(bindingContext, chave, "Informe o parâmetro uma única vez.");
        }

        var texto = valores.FirstValue;
        if (string.IsNullOrEmpty(texto))
        {
            return Task.CompletedTask;
        }

        if (!FormatoComOffset().IsMatch(texto)
            || !DateTimeOffset.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.None, out var instante))
        {
            return Falhar(bindingContext, chave, $"Valor '{texto}' inválido. {MensagemFormato}");
        }

        bindingContext.Result = ModelBindingResult.Success(instante);
        return Task.CompletedTask;
    }

    private static Task Falhar(ModelBindingContext contexto, string chave, string mensagem)
    {
        contexto.ModelState.TryAddModelError(chave, mensagem);
        contexto.Result = ModelBindingResult.Failed();
        return Task.CompletedTask;
    }

    // yyyy-MM-ddTHH:mm[:ss[.fffffff]] seguido OBRIGATORIAMENTE de Z ou ±hh:mm.
    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(:\d{2}(\.\d{1,7})?)?(Z|[+-]\d{2}:\d{2})$", RegexOptions.CultureInvariant)]
    private static partial Regex FormatoComOffset();
}
