using FluentAssertions;
using GarageOS.Contracts.V1;
using GarageOS.Execucao.Application.Abstractions;
using GarageOS.Execucao.Application.DTOs.Execucoes;
using GarageOS.Execucao.Application.UseCases.Execucoes;
using GarageOS.Execucao.Domain.Entities;
using GarageOS.Execucao.Domain.Enums;
using GarageOS.Execucao.Domain.Exceptions;
using GarageOS.Execucao.Domain.ReadModels;
using GarageOS.Execucao.Domain.Repositories;
using Moq;

namespace GarageOS.Execucao.UnitTests.Application.UseCases.Execucoes;

public class ExecucaoUseCasesTests
{
    private readonly Mock<IOrdemDeExecucaoRepository> _repository = new();
    private readonly Mock<IMetricasDeNegocio> _metricas = new();
    private readonly Mock<IReservaPecasRepository> _reservas = new();
    private readonly Mock<IEstoqueRepository> _estoques = new();
    private readonly Mock<IPublicadorDeEventos> _publicador = new();

    private AlterarStatusExecucaoUseCase AlterarStatus() =>
        new(_repository.Object, _reservas.Object, _estoques.Object, _publicador.Object, _metricas.Object);

    private static OrdemDeExecucao NovaOrdem(Guid? osId = null) =>
        new(osId ?? Guid.NewGuid(), "OS-2026-00042", Guid.NewGuid(), [(Guid.NewGuid(), "Revisão")]);

    [Fact]
    public async Task Criar_OSNova_DeveColocarNaFila()
    {
        var request = new CriarOrdemDeExecucaoRequest
        {
            OrdemDeServicoId = Guid.NewGuid(),
            NumeroOS = "OS-2026-00042",
            Itens = [new ItemExecucaoRequest { ServicoId = Guid.NewGuid(), Nome = "Revisão" }]
        };

        var (ordem, criada) = await new CriarOrdemDeExecucaoUseCase(_repository.Object).ExecutarAsync(request);

        criada.Should().BeTrue();
        ordem.Status.Should().Be("NaFila");
        ordem.Itens.Should().ContainSingle(i => i.Nome == "Revisão");
        _repository.Verify(r => r.AdicionarAsync(It.IsAny<OrdemDeExecucao>()), Times.Once);
    }

    [Fact]
    public async Task Criar_OSJaNaFila_DeveDevolverExistente()
    {
        var existente = NovaOrdem();
        _repository.Setup(r => r.ObterPorOrdemDeServicoIdAsync(existente.OrdemDeServicoId)).ReturnsAsync(existente);

        var (ordem, criada) = await new CriarOrdemDeExecucaoUseCase(_repository.Object).ExecutarAsync(
            new CriarOrdemDeExecucaoRequest { OrdemDeServicoId = existente.OrdemDeServicoId, NumeroOS = "x", Itens = [] });

        criada.Should().BeFalse();
        ordem.Id.Should().Be(existente.Id);
        _repository.Verify(r => r.AdicionarAsync(It.IsAny<OrdemDeExecucao>()), Times.Never);
    }

    [Fact]
    public async Task Listar_DevePassarOFiltro()
    {
        _repository.Setup(r => r.ListarFilaAsync(StatusExecucao.EmReparo)).ReturnsAsync([NovaOrdem()]);

        var fila = await new ListarFilaDeExecucaoUseCase(_repository.Object).ExecutarAsync(StatusExecucao.EmReparo);

        fila.Should().HaveCount(1);
    }

    [Fact]
    public async Task Obter_Existente_DeveRetornarComHistorico()
    {
        var ordem = NovaOrdem();
        _repository.Setup(r => r.ObterPorIdAsync(ordem.Id)).ReturnsAsync(ordem);

        var resposta = await new ObterOrdemDeExecucaoUseCase(_repository.Object).ExecutarAsync(ordem.Id);

        resposta.NumeroOS.Should().Be("OS-2026-00042");
        resposta.Historico.Should().ContainSingle(h => h.StatusNovo == "NaFila" && h.StatusAnterior == null);
    }

    [Fact]
    public async Task Obter_Inexistente_DeveLancar()
    {
        var acao = () => new ObterOrdemDeExecucaoUseCase(_repository.Object).ExecutarAsync(Guid.NewGuid());

        await acao.Should().ThrowAsync<OrdemDeExecucaoNaoEncontradaException>();
    }

    [Fact]
    public async Task AlterarStatus_DeveGravarEEmitirMetrica()
    {
        var ordem = NovaOrdem();
        _repository.Setup(r => r.ObterPorIdAsync(ordem.Id)).ReturnsAsync(ordem);

        var resposta = await AlterarStatus()
            .ExecutarAsync(ordem.Id, new AlterarStatusExecucaoRequest { Status = StatusExecucao.EmDiagnostico });

        resposta.Status.Should().Be("EmDiagnostico");
        _repository.Verify(r => r.AtualizarAsync(ordem), Times.Once);
        _metricas.Verify(m => m.ExecucaoStatusAlterado("OS-2026-00042", "NaFila", "EmDiagnostico", It.IsAny<double>()), Times.Once);
    }

    [Fact]
    public async Task AlterarStatus_TransicaoInvalida_NaoDeveGravar()
    {
        var ordem = NovaOrdem();
        _repository.Setup(r => r.ObterPorIdAsync(ordem.Id)).ReturnsAsync(ordem);

        var acao = () => AlterarStatus()
            .ExecutarAsync(ordem.Id, new AlterarStatusExecucaoRequest { Status = StatusExecucao.Concluida });

        await acao.Should().ThrowAsync<TransicaoDeStatusInvalidaException>();
        _repository.Verify(r => r.AtualizarAsync(It.IsAny<OrdemDeExecucao>()), Times.Never);
    }

    [Fact]
    public async Task AlterarStatus_Inexistente_DeveLancar()
    {
        var acao = () => AlterarStatus()
            .ExecutarAsync(Guid.NewGuid(), new AlterarStatusExecucaoRequest { Status = StatusExecucao.EmDiagnostico });

        await acao.Should().ThrowAsync<OrdemDeExecucaoNaoEncontradaException>();
    }

    private OrdemDeExecucao EmReparoComServicoFinalizado()
    {
        var ordem = NovaOrdem();
        ordem.AlterarStatus(StatusExecucao.EmDiagnostico);
        ordem.AlterarStatus(StatusExecucao.EmReparo);
        var item = ordem.Itens.Single();
        ordem.AlterarStatusItem(item.Id, StatusItemExecucao.Iniciado);
        ordem.AlterarStatusItem(item.Id, StatusItemExecucao.Finalizado);
        _repository.Setup(r => r.ObterPorIdAsync(ordem.Id)).ReturnsAsync(ordem);
        return ordem;
    }

    [Fact]
    public async Task Concluir_DeveConsumirReservaEPublicarExecucaoFinalizada()
    {
        var ordem = EmReparoComServicoFinalizado();
        var peca = new GarageOS.Execucao.Domain.Entities.Estoque("Filtro", 10, 40m, DateTime.UtcNow, "Bosch");
        peca.Reservar(2);
        var reserva = new ReservaPecas(ordem.OrdemDeServicoId, [(peca.Id, 2)]);
        _reservas.Setup(r => r.ObterPorOrdemDeServicoIdAsync(ordem.OrdemDeServicoId)).ReturnsAsync(reserva);
        _estoques.Setup(r => r.ObterPorIdsAsync(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync([peca]);

        await AlterarStatus().ExecutarAsync(ordem.Id, new AlterarStatusExecucaoRequest { Status = StatusExecucao.Concluida });

        reserva.Status.Should().Be(StatusReserva.Consumida);
        peca.Quantidade.Should().Be(8);
        _publicador.Verify(p => p.PublicarAsync(It.Is<ExecucaoFinalizada>(e => e.OrdemDeServicoId == ordem.OrdemDeServicoId), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.AtualizarAsync(ordem), Times.Once);
    }

    [Fact]
    public async Task Concluir_SemReserva_DevePublicarMesmoAssim()
    {
        var ordem = EmReparoComServicoFinalizado();

        await AlterarStatus().ExecutarAsync(ordem.Id, new AlterarStatusExecucaoRequest { Status = StatusExecucao.Concluida });

        _publicador.Verify(p => p.PublicarAsync(It.IsAny<ExecucaoFinalizada>(), It.IsAny<CancellationToken>()), Times.Once);
        _estoques.Verify(r => r.ObterPorIdsAsync(It.IsAny<IEnumerable<Guid>>()), Times.Never);
    }

    [Fact]
    public async Task Falhar_DevePublicarExecucaoFalhouComOMotivo()
    {
        var ordem = NovaOrdem();
        _repository.Setup(r => r.ObterPorIdAsync(ordem.Id)).ReturnsAsync(ordem);

        await AlterarStatus().ExecutarAsync(ordem.Id, new AlterarStatusExecucaoRequest { Status = StatusExecucao.Falhou, Motivo = "Motor travado" });

        _publicador.Verify(p => p.PublicarAsync(It.Is<ExecucaoFalhou>(e => e.Motivo == "Motor travado"), It.IsAny<CancellationToken>()), Times.Once);
        _reservas.Verify(r => r.ObterPorOrdemDeServicoIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task AlterarStatusItem_DeveIniciarServicoEGravar()
    {
        var ordem = NovaOrdem();
        var item = ordem.Itens.First();
        _repository.Setup(r => r.ObterPorIdAsync(ordem.Id)).ReturnsAsync(ordem);

        var resposta = await new AlterarStatusItemExecucaoUseCase(_repository.Object)
            .ExecutarAsync(ordem.Id, item.Id, new AlterarStatusItemExecucaoRequest { Status = StatusItemExecucao.Iniciado });

        resposta.Itens.Single().Status.Should().Be("Iniciado");
        _repository.Verify(r => r.AtualizarAsync(ordem), Times.Once);
    }

    [Fact]
    public async Task AlterarStatusItem_ExecucaoInexistente_DeveLancar()
    {
        var acao = () => new AlterarStatusItemExecucaoUseCase(_repository.Object)
            .ExecutarAsync(Guid.NewGuid(), Guid.NewGuid(), new AlterarStatusItemExecucaoRequest { Status = StatusItemExecucao.Iniciado });

        await acao.Should().ThrowAsync<OrdemDeExecucaoNaoEncontradaException>();
    }

    [Theory]
    [InlineData(0.5, "30s")]
    [InlineData(45, "45min")]
    [InlineData(60, "1h")]
    [InlineData(135, "2h 15min")]
    public async Task Aging_DeveFormatarOTempoMedio(double minutos, string esperado)
    {
        _repository.Setup(r => r.CalcularAgingAsync())
            .ReturnsAsync([new AgingServico(Guid.NewGuid(), "Revisão", 3, minutos)]);

        var linhas = await new CalcularAgingServicosUseCase(_repository.Object).ExecutarAsync();

        var linha = linhas.Single();
        linha.TempoMedioFormatado.Should().Be(esperado);
        linha.TotalExecucoes.Should().Be(3);
        linha.ServicoNome.Should().Be("Revisão");
    }

    [Fact]
    public async Task Aging_DeveOrdenarPorNome()
    {
        _repository.Setup(r => r.CalcularAgingAsync()).ReturnsAsync(
        [
            new AgingServico(Guid.NewGuid(), "Troca de óleo", 1, 30),
            new AgingServico(Guid.NewGuid(), "Alinhamento", 1, 20)
        ]);

        var linhas = await new CalcularAgingServicosUseCase(_repository.Object).ExecutarAsync();

        linhas.Select(l => l.ServicoNome).Should().Equal("Alinhamento", "Troca de óleo");
    }
}
