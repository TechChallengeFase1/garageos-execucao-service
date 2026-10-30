using FluentAssertions;
using GarageOS.Contracts.V1;
using GarageOS.Execucao.Application.Abstractions;
using GarageOS.Execucao.Application.UseCases.Execucoes;
using GarageOS.Execucao.Application.UseCases.Reservas;
using GarageOS.Execucao.Domain.Entities;
using GarageOS.Execucao.Domain.Enums;
using GarageOS.Execucao.Domain.Repositories;
using GarageOS.Execucao.Infrastructure.Mensageria.Consumidores;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using EstoqueEntity = GarageOS.Execucao.Domain.Entities.Estoque;

namespace GarageOS.Execucao.UnitTests.Infrastructure.Mensageria;

/// <summary>Os consumidores da saga, com os use cases de verdade e repositórios simulados</summary>
public class ConsumidoresTests : IAsyncLifetime
{
    private readonly Mock<IReservaPecasRepository> _reservas = new();
    private readonly Mock<IEstoqueRepository> _estoques = new();
    private readonly Mock<IOrdemDeExecucaoRepository> _ordens = new();
    private readonly Mock<IMetricasDeNegocio> _metricas = new();
    private readonly EstoqueEntity _filtro = new("Filtro", 5, 40m, DateTime.UtcNow, "Bosch");

    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;

    public async Task InitializeAsync()
    {
        _estoques.Setup(r => r.ObterPorIdsAsync(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync([_filtro]);

        _provider = new ServiceCollection()
            .AddSingleton(_reservas.Object)
            .AddSingleton(_estoques.Object)
            .AddSingleton(_ordens.Object)
            .AddSingleton(_metricas.Object)
            .AddScoped<ReservarPecasUseCase>()
            .AddScoped<LiberarReservaUseCase>()
            .AddScoped<CriarOrdemDeExecucaoUseCase>()
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<ReservarPecasConsumer>();
                x.AddConsumer<LiberarReservaPecasConsumer>();
                x.AddConsumer<IniciarExecucaoConsumer>();
            })
            .BuildServiceProvider(true);

        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }

    [Fact]
    public async Task ReservarPecas_ComSaldo_DevePublicarPecasReservadas()
    {
        var osId = Guid.NewGuid();

        await _harness.Bus.Publish(new ReservarPecas(osId, [new PecaDoPedido(_filtro.Id, 2)]));

        (await _harness.Published.Any<PecasReservadas>(m => m.Context.Message.OrdemDeServicoId == osId)).Should().BeTrue();
        _filtro.QuantidadeReservada.Should().Be(2);
        _reservas.Verify(r => r.AdicionarAsync(It.IsAny<ReservaPecas>()), Times.Once);
    }

    [Fact]
    public async Task ReservarPecas_SemSaldo_DevePublicarPecasIndisponiveis()
    {
        var osId = Guid.NewGuid();

        await _harness.Bus.Publish(new ReservarPecas(osId, [new PecaDoPedido(_filtro.Id, 50)]));

        (await _harness.Published.Any<PecasIndisponiveis>(m => m.Context.Message.OrdemDeServicoId == osId)).Should().BeTrue();
        (await _harness.Published.Any<PecasReservadas>()).Should().BeFalse();
        _filtro.QuantidadeReservada.Should().Be(0);
    }

    [Fact]
    public async Task ReservarPecas_PecaInexistente_DevePublicarPecasIndisponiveis()
    {
        await _harness.Bus.Publish(new ReservarPecas(Guid.NewGuid(), [new PecaDoPedido(Guid.NewGuid(), 1)]));

        (await _harness.Published.Any<PecasIndisponiveis>()).Should().BeTrue();
    }

    [Fact]
    public async Task ReservarPecas_Repetida_NaoDeveReservarDeNovo()
    {
        var osId = Guid.NewGuid();
        _reservas.Setup(r => r.ObterPorOrdemDeServicoIdAsync(osId)).ReturnsAsync(new ReservaPecas(osId, [(_filtro.Id, 2)]));

        await _harness.Bus.Publish(new ReservarPecas(osId, [new PecaDoPedido(_filtro.Id, 2)]));

        (await _harness.Published.Any<PecasReservadas>(m => m.Context.Message.OrdemDeServicoId == osId)).Should().BeTrue();
        _filtro.QuantidadeReservada.Should().Be(0);
        _reservas.Verify(r => r.AdicionarAsync(It.IsAny<ReservaPecas>()), Times.Never);
    }

    [Fact]
    public async Task LiberarReserva_Ativa_DeveDevolverEConfirmar()
    {
        var osId = Guid.NewGuid();
        _filtro.Reservar(3);
        var reserva = new ReservaPecas(osId, [(_filtro.Id, 3)]);
        _reservas.Setup(r => r.ObterPorOrdemDeServicoIdAsync(osId)).ReturnsAsync(reserva);

        await _harness.Bus.Publish(new LiberarReservaPecas(osId, "Pagamento recusado"));

        (await _harness.Published.Any<ReservaLiberada>(m => m.Context.Message.OrdemDeServicoId == osId)).Should().BeTrue();
        reserva.Status.Should().Be(StatusReserva.Liberada);
        _filtro.QuantidadeReservada.Should().Be(0);
    }

    [Fact]
    public async Task LiberarReserva_SemReserva_DeveConfirmarSemErro()
    {
        var osId = Guid.NewGuid();

        await _harness.Bus.Publish(new LiberarReservaPecas(osId, "OS sem peças"));

        (await _harness.Published.Any<ReservaLiberada>(m => m.Context.Message.OrdemDeServicoId == osId)).Should().BeTrue();
        (await _harness.Published.Any<Fault>()).Should().BeFalse();
    }

    [Fact]
    public async Task LiberarReserva_JaConsumida_DeveConfirmarSemErro()
    {
        var osId = Guid.NewGuid();
        var reserva = new ReservaPecas(osId, [(_filtro.Id, 1)]);
        reserva.Consumir();
        _reservas.Setup(r => r.ObterPorOrdemDeServicoIdAsync(osId)).ReturnsAsync(reserva);

        await _harness.Bus.Publish(new LiberarReservaPecas(osId, "Execução falhou"));

        (await _harness.Published.Any<ReservaLiberada>(m => m.Context.Message.OrdemDeServicoId == osId)).Should().BeTrue();
        (await _harness.Published.Any<Fault>()).Should().BeFalse();
    }

    [Fact]
    public async Task IniciarExecucao_DeveColocarNaFilaEPublicar()
    {
        var osId = Guid.NewGuid();

        await _harness.Bus.Publish(new IniciarExecucao(osId, "OS-2026-00042", [new ServicoDoPedido(Guid.NewGuid(), "Revisão")]));

        (await _harness.Published.Any<ExecucaoIniciada>(m => m.Context.Message.OrdemDeServicoId == osId)).Should().BeTrue();
        _ordens.Verify(r => r.AdicionarAsync(It.Is<OrdemDeExecucao>(o => o.OrdemDeServicoId == osId && o.Status == StatusExecucao.NaFila)), Times.Once);
    }
}
