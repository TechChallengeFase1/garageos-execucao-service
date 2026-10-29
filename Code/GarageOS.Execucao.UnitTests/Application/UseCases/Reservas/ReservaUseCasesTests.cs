using FluentAssertions;
using GarageOS.Execucao.Application.Abstractions;
using GarageOS.Execucao.Application.DTOs.Reservas;
using GarageOS.Execucao.Application.UseCases.Reservas;
using GarageOS.Execucao.Domain.Entities;
using GarageOS.Execucao.Domain.Enums;
using GarageOS.Execucao.Domain.Exceptions;
using GarageOS.Execucao.Domain.Repositories;
using Moq;
using EstoqueEntity = GarageOS.Execucao.Domain.Entities.Estoque;

namespace GarageOS.Execucao.UnitTests.Application.UseCases.Reservas;

public class ReservaUseCasesTests
{
    private readonly Mock<IReservaPecasRepository> _reservas = new();
    private readonly Mock<IEstoqueRepository> _estoques = new();
    private readonly Mock<IMetricasDeNegocio> _metricas = new();

    private readonly EstoqueEntity _filtro = new("Filtro", 10, 45m, DateTime.UtcNow, "Bosch");
    private readonly EstoqueEntity _pastilha = new("Pastilha", 2, 120m, DateTime.UtcNow, "TRW");

    private ReservarPecasUseCase Reservar() => new(_reservas.Object, _estoques.Object, _metricas.Object);

    private void EstoquesDisponiveis(params EstoqueEntity[] estoques) =>
        _estoques.Setup(r => r.ObterPorIdsAsync(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(estoques.ToList());

    private ReservarPecasRequest Pedido(Guid osId, params (Guid Id, int Qtd)[] itens) => new()
    {
        OrdemDeServicoId = osId,
        Itens = itens.Select(i => new ItemReservaRequest { EstoqueId = i.Id, Quantidade = i.Qtd }).ToList()
    };

    [Fact]
    public async Task Reservar_ComSaldo_DeveReservarTodasEGravarUmaVez()
    {
        var osId = Guid.NewGuid();
        EstoquesDisponiveis(_filtro, _pastilha);

        var (reserva, criada) = await Reservar().ExecutarAsync(Pedido(osId, (_filtro.Id, 3), (_pastilha.Id, 2)));

        criada.Should().BeTrue();
        reserva.OrdemDeServicoId.Should().Be(osId);
        reserva.Status.Should().Be("Ativa");
        _filtro.QuantidadeReservada.Should().Be(3);
        _pastilha.QuantidadeReservada.Should().Be(2);
        _reservas.Verify(r => r.AdicionarAsync(It.IsAny<ReservaPecas>()), Times.Once);
    }

    [Fact]
    public async Task Reservar_UmaPecaSemSaldo_NaoDeveGravarNada()
    {
        EstoquesDisponiveis(_filtro, _pastilha);

        var acao = () => Reservar().ExecutarAsync(Pedido(Guid.NewGuid(), (_filtro.Id, 1), (_pastilha.Id, 5)));

        await acao.Should().ThrowAsync<EstoqueInsuficienteException>();
        _reservas.Verify(r => r.AdicionarAsync(It.IsAny<ReservaPecas>()), Times.Never);
        _metricas.Verify(m => m.ReservaRecusada(It.IsAny<Guid>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Reservar_PecaInexistente_DeveLancarNaoEncontrado()
    {
        EstoquesDisponiveis(_filtro);

        var acao = () => Reservar().ExecutarAsync(Pedido(Guid.NewGuid(), (Guid.NewGuid(), 1)));

        await acao.Should().ThrowAsync<EstoqueNaoEncontradoException>();
        _reservas.Verify(r => r.AdicionarAsync(It.IsAny<ReservaPecas>()), Times.Never);
    }

    [Fact]
    public async Task Reservar_OSJaReservada_DeveDevolverExistenteSemReservarDeNovo()
    {
        var osId = Guid.NewGuid();
        var existente = new ReservaPecas(osId, [(_filtro.Id, 1)]);
        _reservas.Setup(r => r.ObterPorOrdemDeServicoIdAsync(osId)).ReturnsAsync(existente);

        var (reserva, criada) = await Reservar().ExecutarAsync(Pedido(osId, (_filtro.Id, 1)));

        criada.Should().BeFalse();
        reserva.Id.Should().Be(existente.Id);
        _estoques.Verify(r => r.ObterPorIdsAsync(It.IsAny<IEnumerable<Guid>>()), Times.Never);
        _reservas.Verify(r => r.AdicionarAsync(It.IsAny<ReservaPecas>()), Times.Never);
    }

    [Fact]
    public async Task Liberar_Ativa_DeveDevolverSaldoEGravar()
    {
        var osId = Guid.NewGuid();
        _filtro.Reservar(3);
        var reserva = new ReservaPecas(osId, [(_filtro.Id, 3)]);
        _reservas.Setup(r => r.ObterPorOrdemDeServicoIdAsync(osId)).ReturnsAsync(reserva);
        EstoquesDisponiveis(_filtro);

        await new LiberarReservaUseCase(_reservas.Object, _estoques.Object).ExecutarAsync(osId);

        reserva.Status.Should().Be(StatusReserva.Liberada);
        _filtro.QuantidadeReservada.Should().Be(0);
        _reservas.Verify(r => r.AtualizarAsync(reserva), Times.Once);
    }

    [Fact]
    public async Task Liberar_JaLiberada_NaoDeveMexerNoEstoque()
    {
        var osId = Guid.NewGuid();
        var reserva = new ReservaPecas(osId, [(_filtro.Id, 3)]);
        reserva.Liberar();
        _reservas.Setup(r => r.ObterPorOrdemDeServicoIdAsync(osId)).ReturnsAsync(reserva);

        await new LiberarReservaUseCase(_reservas.Object, _estoques.Object).ExecutarAsync(osId);

        _estoques.Verify(r => r.ObterPorIdsAsync(It.IsAny<IEnumerable<Guid>>()), Times.Never);
        _reservas.Verify(r => r.AtualizarAsync(It.IsAny<ReservaPecas>()), Times.Never);
    }

    [Fact]
    public async Task Liberar_SemReserva_DeveLancarNaoEncontrada()
    {
        var acao = () => new LiberarReservaUseCase(_reservas.Object, _estoques.Object).ExecutarAsync(Guid.NewGuid());

        await acao.Should().ThrowAsync<ReservaNaoEncontradaException>();
    }

    [Fact]
    public async Task Consumir_Ativa_DeveDarBaixaEGravar()
    {
        var osId = Guid.NewGuid();
        _filtro.Reservar(4);
        var reserva = new ReservaPecas(osId, [(_filtro.Id, 4)]);
        _reservas.Setup(r => r.ObterPorOrdemDeServicoIdAsync(osId)).ReturnsAsync(reserva);
        EstoquesDisponiveis(_filtro);

        await new ConsumirReservaUseCase(_reservas.Object, _estoques.Object).ExecutarAsync(osId);

        reserva.Status.Should().Be(StatusReserva.Consumida);
        _filtro.Quantidade.Should().Be(6);
        _filtro.QuantidadeReservada.Should().Be(0);
        _reservas.Verify(r => r.AtualizarAsync(reserva), Times.Once);
    }

    [Fact]
    public async Task Consumir_JaConsumida_NaoDeveBaixarDuasVezes()
    {
        var osId = Guid.NewGuid();
        var reserva = new ReservaPecas(osId, [(_filtro.Id, 4)]);
        reserva.Consumir();
        _reservas.Setup(r => r.ObterPorOrdemDeServicoIdAsync(osId)).ReturnsAsync(reserva);

        await new ConsumirReservaUseCase(_reservas.Object, _estoques.Object).ExecutarAsync(osId);

        _filtro.Quantidade.Should().Be(10);
        _reservas.Verify(r => r.AtualizarAsync(It.IsAny<ReservaPecas>()), Times.Never);
    }

    [Fact]
    public async Task Consumir_SemReserva_DeveLancarNaoEncontrada()
    {
        var acao = () => new ConsumirReservaUseCase(_reservas.Object, _estoques.Object).ExecutarAsync(Guid.NewGuid());

        await acao.Should().ThrowAsync<ReservaNaoEncontradaException>();
    }

    [Fact]
    public async Task Obter_Existente_DeveRetornar()
    {
        var osId = Guid.NewGuid();
        _reservas.Setup(r => r.ObterPorOrdemDeServicoIdAsync(osId)).ReturnsAsync(new ReservaPecas(osId, [(_filtro.Id, 1)]));

        var resposta = await new ObterReservaUseCase(_reservas.Object).ExecutarAsync(osId);

        resposta.Itens.Should().ContainSingle(i => i.EstoqueId == _filtro.Id && i.Quantidade == 1);
    }

    [Fact]
    public async Task Obter_Inexistente_DeveLancar()
    {
        var acao = () => new ObterReservaUseCase(_reservas.Object).ExecutarAsync(Guid.NewGuid());

        await acao.Should().ThrowAsync<ReservaNaoEncontradaException>();
    }
}
