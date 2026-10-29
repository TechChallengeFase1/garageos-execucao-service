using FluentAssertions;
using GarageOS.Execucao.Domain.Entities;
using GarageOS.Execucao.Domain.Enums;
using GarageOS.Execucao.Domain.Exceptions;

namespace GarageOS.Execucao.UnitTests.Domain.Entities;

public class EstoqueReservaTests
{
    private static Estoque NovoEstoque(int quantidade = 10) =>
        new("Filtro de óleo", quantidade, 45.90m, DateTime.UtcNow, "Bosch");

    [Fact]
    public void Reservar_ComSaldo_DeveSepararSemTirarDoEstoque()
    {
        var estoque = NovoEstoque(10);

        estoque.Reservar(4);

        estoque.Quantidade.Should().Be(10);
        estoque.QuantidadeReservada.Should().Be(4);
        estoque.QuantidadeDisponivel.Should().Be(6);
        estoque.Status.Should().Be(StatusEstoque.Disponivel);
    }

    [Fact]
    public void Reservar_TudoQueHa_DeveDeixarIndisponivel()
    {
        var estoque = NovoEstoque(3);

        estoque.Reservar(3);

        estoque.QuantidadeDisponivel.Should().Be(0);
        estoque.Status.Should().Be(StatusEstoque.Indisponivel);
    }

    [Fact]
    public void Reservar_AlemDoDisponivel_DeveLancarEstoqueInsuficiente()
    {
        var estoque = NovoEstoque(5);
        estoque.Reservar(4);

        var acao = () => estoque.Reservar(2);

        acao.Should().Throw<EstoqueInsuficienteException>()
            .Which.EstoqueId.Should().Be(estoque.Id);
        estoque.QuantidadeReservada.Should().Be(4);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Reservar_QuantidadeNaoPositiva_DeveLancarArgumentException(int quantidade)
    {
        var acao = () => NovoEstoque().Reservar(quantidade);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LiberarReserva_DeveDevolverAoDisponivel()
    {
        var estoque = NovoEstoque(5);
        estoque.Reservar(5);

        estoque.LiberarReserva(5);

        estoque.QuantidadeReservada.Should().Be(0);
        estoque.Quantidade.Should().Be(5);
        estoque.Status.Should().Be(StatusEstoque.Disponivel);
    }

    [Fact]
    public void LiberarReserva_MaisQueOReservado_DeveLancarInvalidOperation()
    {
        var estoque = NovoEstoque(5);
        estoque.Reservar(1);

        var acao = () => estoque.LiberarReserva(2);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ConsumirReserva_DeveDarBaixaDefinitiva()
    {
        var estoque = NovoEstoque(10);
        estoque.Reservar(3);

        estoque.ConsumirReserva(3);

        estoque.Quantidade.Should().Be(7);
        estoque.QuantidadeReservada.Should().Be(0);
        estoque.QuantidadeDisponivel.Should().Be(7);
    }

    [Fact]
    public void ConsumirReserva_SemReserva_DeveLancarInvalidOperation()
    {
        var acao = () => NovoEstoque().ConsumirReserva(1);

        acao.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Devolver_DeveReporNoEstoque()
    {
        var estoque = NovoEstoque(0);

        estoque.Devolver(2);

        estoque.Quantidade.Should().Be(2);
        estoque.Status.Should().Be(StatusEstoque.Disponivel);
    }

    [Fact]
    public void Devolver_QuantidadeNaoPositiva_DeveLancarArgumentException()
    {
        var acao = () => NovoEstoque().Devolver(0);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Atualizar_AbaixoDoReservado_DeveLancarArgumentException()
    {
        var estoque = NovoEstoque(10);
        estoque.Reservar(6);

        var acao = () => estoque.Atualizar("Filtro", 5, 40m, DateTime.UtcNow, "Bosch");

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Atualizar_ComDadosValidos_DeveRecalcularStatusPeloDisponivel()
    {
        var estoque = NovoEstoque(10);
        estoque.Reservar(6);

        estoque.Atualizar("Filtro novo", 6, 50m, DateTime.UtcNow, "Mann");

        estoque.Nome.Should().Be("Filtro novo");
        estoque.Valor.Should().Be(50m);
        estoque.Status.Should().Be(StatusEstoque.Indisponivel);
    }

    [Theory]
    [InlineData("", 1, 1, "F")]
    [InlineData("N", -1, 1, "F")]
    [InlineData("N", 1, 0, "F")]
    [InlineData("N", 1, 1, "")]
    public void Atualizar_ComDadosInvalidos_DeveLancarArgumentException(string nome, int quantidade, decimal valor, string fornecedor)
    {
        var acao = () => NovoEstoque().Atualizar(nome, quantidade, valor, DateTime.UtcNow, fornecedor);

        acao.Should().Throw<ArgumentException>();
    }
}
