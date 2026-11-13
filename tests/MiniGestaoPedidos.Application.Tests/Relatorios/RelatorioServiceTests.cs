using FluentValidation;
using NSubstitute;
using MiniGestaoPedidos.Application.Relatorios;

namespace MiniGestaoPedidos.Application.Tests.Relatorios;

public class RelatorioServiceTests
{
    private readonly IRelatorioQueries _queries = Substitute.For<IRelatorioQueries>();
    private readonly RelatorioService _service;

    public RelatorioServiceTests()
    {
        _service = new RelatorioService(_queries, new TopClientesQueryValidator());
    }

    [Fact]
    public async Task TopClientesAsync_PeriodoValido_ConsultaIntervaloSemiabertoComLimite5()
    {
        var query = new TopClientesQuery { DataInicio = new DateOnly(2026, 1, 1), DataFim = new DateOnly(2026, 12, 31) };

        await _service.TopClientesAsync(query, CancellationToken.None);

        await _queries.Received(1).TopClientesAsync(
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            5,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task TopClientesAsync_PeriodoIncompleto_LancaValidationException(bool comInicio, bool comFim)
    {
        var query = new TopClientesQuery
        {
            DataInicio = comInicio ? new DateOnly(2026, 1, 1) : null,
            DataFim = comFim ? new DateOnly(2026, 1, 31) : null,
        };

        await Assert.ThrowsAsync<ValidationException>(() => _service.TopClientesAsync(query, CancellationToken.None)); // CA14

        await _queries.DidNotReceiveWithAnyArgs().TopClientesAsync(default, default, default, default);
    }

    [Fact]
    public async Task TopClientesAsync_PeriodoInvertido_LancaValidationException()
    {
        var query = new TopClientesQuery { DataInicio = new DateOnly(2026, 2, 1), DataFim = new DateOnly(2026, 1, 1) };

        await Assert.ThrowsAsync<ValidationException>(() => _service.TopClientesAsync(query, CancellationToken.None));
    }
}
