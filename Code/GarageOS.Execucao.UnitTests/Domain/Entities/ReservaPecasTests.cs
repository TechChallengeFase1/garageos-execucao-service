using FluentAssertions;
using GarageOS.Execucao.Domain.Entities;
using GarageOS.Execucao.Domain.Enums;
using GarageOS.Execucao.Domain.Exceptions;

namespace GarageOS.Execucao.UnitTests.Domain.Entities;

public class ReservaPecasTests
{
    private static readonly Guid PecaA = Guid.NewGuid();
    private static readonly Guid PecaB = Guid.NewGuid();

    private static ReservaPecas NovaReserva() =>
        new(Guid.NewGuid(), [(PecaA, 2), (PecaB, 1)]);

    [Fact]
    public void Construtor_DeveCriarReservaAtiva()
    {
        var reserva = NovaReserva();

        reserva.Status.Should().Be(StatusReserva.Ativa);
        reserva.Itens.Should().HaveCount(2);
    }

    [Fact]
    public void Construtor_MesmaPecaEmDuasLinhas_DeveSomarNumaLinhaSo()
    {
        var reserva = new ReservaPecas(Guid.NewGuid(), [(PecaA, 2), (PecaA, 3)]);

        reserva.Itens.Should().ContainSingle()
            .Which.Quantidade.Should().Be(5);
    }

    [Fact]
    public void Construtor_SemItens_DeveLancarArgumentException()
    {
        var acao = () => new ReservaPecas(Guid.NewGuid(), []);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Construtor_SemOrdemDeServico_DeveLancarArgumentException()
    {
        var acao = () => new ReservaPecas(Guid.Empty, [(PecaA, 1)]);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Construtor_ItemComQuantidadeZero_DeveLancarArgumentException()
    {
        var acao = () => new ReservaPecas(Guid.NewGuid(), [(PecaA, 0)]);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Liberar_Ativa_DeveLiberarEDevolverTrue()
    {
        var reserva = NovaReserva();

        reserva.Liberar().Should().BeTrue();
        reserva.Status.Should().Be(StatusReserva.Liberada);
    }

    [Fact]
    public void Liberar_JaLiberada_DeveSerIdempotente()
    {
        var reserva = NovaReserva();
        reserva.Liberar();

        reserva.Liberar().Should().BeFalse();
        reserva.Status.Should().Be(StatusReserva.Liberada);
    }

    [Fact]
    public void Liberar_Consumida_DeveLancar()
    {
        var reserva = NovaReserva();
        reserva.Consumir();

        var acao = () => reserva.Liberar();

        acao.Should().Throw<ReservaStatusInvalidoException>();
    }

    [Fact]
    public void Consumir_Ativa_DeveConsumirEDevolverTrue()
    {
        var reserva = NovaReserva();

        reserva.Consumir().Should().BeTrue();
        reserva.Status.Should().Be(StatusReserva.Consumida);
    }

    [Fact]
    public void Consumir_JaConsumida_DeveSerIdempotente()
    {
        var reserva = NovaReserva();
        reserva.Consumir();

        reserva.Consumir().Should().BeFalse();
    }

    [Fact]
    public void Consumir_Liberada_DeveLancar()
    {
        var reserva = NovaReserva();
        reserva.Liberar();

        var acao = () => reserva.Consumir();

        acao.Should().Throw<ReservaStatusInvalidoException>();
    }
}
