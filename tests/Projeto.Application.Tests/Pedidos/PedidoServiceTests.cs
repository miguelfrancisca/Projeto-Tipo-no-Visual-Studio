using FluentValidation;
using NSubstitute;
using Projeto.Application.Abstractions;
using Projeto.Application.Common.Exceptions;
using Projeto.Application.Pedidos;
using Projeto.Application.Tests.Builders;
using Projeto.Domain.Entities;
using Projeto.Domain.Exceptions;

namespace Projeto.Application.Tests.Pedidos;

public class PedidoServiceTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 12, 14, 30, 0, TimeSpan.Zero);

    private readonly IClienteRepository _clientes = Substitute.For<IClienteRepository>();
    private readonly IProdutoRepository _produtos = Substitute.For<IProdutoRepository>();
    private readonly IPedidoRepository _pedidos = Substitute.For<IPedidoRepository>();
    private readonly IPedidoQueries _queries = Substitute.For<IPedidoQueries>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly PedidoService _service;

    public PedidoServiceTests()
    {
        _service = new PedidoService(
            _clientes,
            _produtos,
            _pedidos,
            _queries,
            _unitOfWork,
            new CriarPedidoRequestValidator(),
            new ListarPedidosQueryValidator(),
            new RelogioFixo(Agora));
    }

    // ---------- Caminho feliz ----------

    [Fact]
    public async Task CriarAsync_DadosValidos_PersisteERetornaPedidoComTotal()
    {
        var cliente = Construtores.Cliente(id: 1, nome: "Ana Souza");
        var teclado = Construtores.Produto(id: 3, preco: 149.90m, estoque: 10, nome: "Teclado");
        var mouse = Construtores.Produto(id: 5, preco: 59.90m, estoque: 10, nome: "Mouse");
        ConfigurarCliente(cliente);
        ConfigurarProdutos(teclado, mouse);

        var resposta = await _service.CriarAsync(
            new CriarPedidoRequest(1, [new(3, 2), new(5, 1)]), CancellationToken.None);

        Assert.Equal(1, resposta.ClienteId);
        Assert.Equal("Ana Souza", resposta.ClienteNome);
        Assert.Equal(359.70m, resposta.ValorTotal);
        Assert.Equal(2, resposta.Itens.Count);
        _pedidos.Received(1).Adicionar(Arg.Any<Pedido>());
        await _unitOfWork.Received(1).SalvarAlteracoesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CriarAsync_DadosValidos_BaixaEstoque()
    {
        var produto = Construtores.Produto(id: 3, estoque: 10);
        ConfigurarCliente(Construtores.Cliente());
        ConfigurarProdutos(produto);

        await _service.CriarAsync(new CriarPedidoRequest(1, [new(3, 4)]), CancellationToken.None);

        Assert.Equal(6, produto.Estoque);
    }

    [Fact]
    public async Task CriarAsync_DadosValidos_UsaPrecoAtualDoProduto()
    {
        ConfigurarCliente(Construtores.Cliente());
        ConfigurarProdutos(Construtores.Produto(id: 3, preco: 42.00m));

        var resposta = await _service.CriarAsync(new CriarPedidoRequest(1, [new(3, 1)]), CancellationToken.None);

        Assert.Equal(42.00m, resposta.Itens.Single().PrecoUnitario); // RN07
    }

    [Fact]
    public async Task CriarAsync_DadosValidos_UsaDataDoRelogioDoServidor()
    {
        ConfigurarCliente(Construtores.Cliente());
        ConfigurarProdutos(Construtores.Produto(id: 3));

        var resposta = await _service.CriarAsync(new CriarPedidoRequest(1, [new(3, 1)]), CancellationToken.None);

        Assert.Equal(Agora.UtcDateTime, resposta.DataPedido); // RN08
    }

    [Fact]
    public async Task CriarAsync_RelogioComSubMilissegundos_TruncaParaPrecisaoDaColuna()
    {
        var relogio = new RelogioFixo(new DateTimeOffset(2026, 11, 12, 22, 50, 12, TimeSpan.Zero).AddTicks(1831478));
        var service = new PedidoService(
            _clientes, _produtos, _pedidos, _queries, _unitOfWork,
            new CriarPedidoRequestValidator(), new ListarPedidosQueryValidator(), relogio);
        ConfigurarCliente(Construtores.Cliente());
        ConfigurarProdutos(Construtores.Produto(id: 3));

        var resposta = await service.CriarAsync(new CriarPedidoRequest(1, [new(3, 1)]), CancellationToken.None);

        Assert.Equal(new DateTime(2026, 11, 12, 22, 50, 12, 183, DateTimeKind.Utc), resposta.DataPedido);
    }

    // ---------- Regras que dependem de dados (422) ----------

    [Fact]
    public async Task CriarAsync_ClienteInexistente_LancaRegraNegocioESemPersistir()
    {
        _clientes.ObterPorIdAsync(99, Arg.Any<CancellationToken>()).Returns((Cliente?)null);
        ConfigurarProdutos(Construtores.Produto(id: 3));

        var ex = await Assert.ThrowsAsync<RegraNegocioException>(
            () => _service.CriarAsync(new CriarPedidoRequest(99, [new(3, 1)]), CancellationToken.None));

        Assert.Contains("99", ex.Message); // RN01
        await NaoDevePersistir();
    }

    [Fact]
    public async Task CriarAsync_ProdutoInexistente_LancaRegraNegocioComIdsFaltantes()
    {
        ConfigurarCliente(Construtores.Cliente());
        ConfigurarProdutos(Construtores.Produto(id: 3)); // 7 e 8 não existem

        var ex = await Assert.ThrowsAsync<RegraNegocioException>(
            () => _service.CriarAsync(new CriarPedidoRequest(1, [new(3, 1), new(7, 1), new(8, 1)]), CancellationToken.None));

        Assert.Contains("7", ex.Message); // RN04
        Assert.Contains("8", ex.Message);
        await NaoDevePersistir();
    }

    [Fact]
    public async Task CriarAsync_EstoqueInsuficiente_LancaExcecaoSemBaixarNenhumEstoque()
    {
        var comEstoque = Construtores.Produto(id: 3, estoque: 10);
        var semEstoque = Construtores.Produto(id: 5, estoque: 1);
        ConfigurarCliente(Construtores.Cliente());
        ConfigurarProdutos(comEstoque, semEstoque);

        var ex = await Assert.ThrowsAsync<EstoqueInsuficienteException>(
            () => _service.CriarAsync(new CriarPedidoRequest(1, [new(3, 2), new(5, 2)]), CancellationToken.None));

        Assert.Equal(5, ex.ProdutoId); // RN06 / CA06
        Assert.Equal(10, comEstoque.Estoque);
        Assert.Equal(1, semEstoque.Estoque);
        await NaoDevePersistir();
    }

    // ---------- Validação de formato (400) ----------

    [Fact]
    public async Task CriarAsync_SemItens_LancaValidationExceptionSemConsultarBanco()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CriarAsync(new CriarPedidoRequest(1, []), CancellationToken.None)); // RN02

        await _clientes.DidNotReceiveWithAnyArgs().ObterPorIdAsync(default, default);
        await NaoDevePersistir();
    }

    [Fact]
    public async Task CriarAsync_QuantidadeZero_LancaValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CriarAsync(new CriarPedidoRequest(1, [new(3, 0)]), CancellationToken.None)); // RN03

        await NaoDevePersistir();
    }

    // ---------- Consulta ----------

    [Fact]
    public async Task ObterPorIdAsync_PedidoInexistente_LancaNaoEncontrado()
    {
        _queries.ObterPorIdAsync(123, Arg.Any<CancellationToken>()).Returns((PedidoResponse?)null);

        await Assert.ThrowsAsync<NaoEncontradoException>(() => _service.ObterPorIdAsync(123, CancellationToken.None));
    }

    [Fact]
    public async Task ListarAsync_PeriodoInvertido_LancaValidationExceptionSemConsultar()
    {
        var query = new ListarPedidosQuery { DataInicio = new DateOnly(2026, 2, 1), DataFim = new DateOnly(2026, 1, 1) };

        await Assert.ThrowsAsync<ValidationException>(() => _service.ListarAsync(query, CancellationToken.None)); // RN11

        await _queries.DidNotReceiveWithAnyArgs().ListarAsync(default!, default);
    }

    // ---------- Helpers ----------

    private void ConfigurarCliente(Cliente cliente) =>
        _clientes.ObterPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);

    private void ConfigurarProdutos(params Produto[] produtos) =>
        _produtos.ObterPorIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(call => produtos.Where(p => call.Arg<IReadOnlyCollection<int>>().Contains(p.Id)).ToList());

    private async Task NaoDevePersistir()
    {
        _pedidos.DidNotReceiveWithAnyArgs().Adicionar(default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SalvarAlteracoesAsync(default);
    }
}
