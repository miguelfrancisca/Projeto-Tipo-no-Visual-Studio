namespace MiniGestaoPedidos.Application.Tests.Builders;

/// <summary>TimeProvider determinístico para testar RN08.</summary>
internal sealed class RelogioFixo(DateTimeOffset agora) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => agora;
}
