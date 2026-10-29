using FluentAssertions;
using GarageOS.Execucao.Domain.Entities;
using GarageOS.Execucao.Domain.Enums;
using GarageOS.Execucao.Domain.Exceptions;

namespace GarageOS.Execucao.UnitTests.Domain.Entities;

public class OrdemDeExecucaoTests
{
    private static OrdemDeExecucao NovaOrdem() =>
        new(Guid.NewGuid(), "OS-2026-00001", null, [(Guid.NewGuid(), "Troca de óleo"), (Guid.NewGuid(), "Alinhamento")]);

    private static void FinalizarTodos(OrdemDeExecucao ordem)
    {
        foreach (var item in ordem.Itens.ToList())
        {
            ordem.AlterarStatusItem(item.Id, StatusItemExecucao.Iniciado);
            ordem.AlterarStatusItem(item.Id, StatusItemExecucao.Finalizado);
        }
    }

    [Fact]
    public void Construtor_DeveEntrarNaFilaComHistorico()
    {
        var ordem = NovaOrdem();

        ordem.Status.Should().Be(StatusExecucao.NaFila);
        ordem.Itens.Should().HaveCount(2);
        ordem.Historico.Should().ContainSingle()
            .Which.StatusNovo.Should().Be(StatusExecucao.NaFila);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("OS-1", false)]
    public void Construtor_ComDadosInvalidos_DeveLancar(string numeroOS, bool comItens)
    {
        var itens = comItens ? new[] { (Guid.NewGuid(), "Serviço") } : [];

        var acao = () => new OrdemDeExecucao(Guid.NewGuid(), numeroOS, null, itens);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Construtor_SemOrdemDeServico_DeveLancar()
    {
        var acao = () => new OrdemDeExecucao(Guid.Empty, "OS-1", null, [(Guid.NewGuid(), "Serviço")]);

        acao.Should().Throw<ArgumentException>();
    }

    // Tabela completa: (de, para, permitido)
    [Theory]
    [InlineData(StatusExecucao.NaFila, StatusExecucao.EmDiagnostico, true)]
    [InlineData(StatusExecucao.NaFila, StatusExecucao.Falhou, true)]
    [InlineData(StatusExecucao.NaFila, StatusExecucao.EmReparo, false)]
    [InlineData(StatusExecucao.NaFila, StatusExecucao.Concluida, false)]
    [InlineData(StatusExecucao.EmDiagnostico, StatusExecucao.EmReparo, true)]
    [InlineData(StatusExecucao.EmDiagnostico, StatusExecucao.Falhou, true)]
    [InlineData(StatusExecucao.EmDiagnostico, StatusExecucao.NaFila, false)]
    [InlineData(StatusExecucao.EmDiagnostico, StatusExecucao.Concluida, false)]
    [InlineData(StatusExecucao.EmReparo, StatusExecucao.Concluida, true)]
    [InlineData(StatusExecucao.EmReparo, StatusExecucao.Falhou, true)]
    [InlineData(StatusExecucao.EmReparo, StatusExecucao.EmDiagnostico, false)]
    [InlineData(StatusExecucao.Concluida, StatusExecucao.Falhou, false)]
    [InlineData(StatusExecucao.Falhou, StatusExecucao.NaFila, false)]
    public void TransicaoPermitida_DeveSeguirATabela(StatusExecucao de, StatusExecucao para, bool esperado)
    {
        OrdemDeExecucao.TransicaoPermitida(de, para).Should().Be(esperado);
    }

    [Fact]
    public void AlterarStatus_CaminhoFeliz_DeveRegistrarHistorico()
    {
        var ordem = NovaOrdem();

        ordem.AlterarStatus(StatusExecucao.EmDiagnostico);
        ordem.AlterarStatus(StatusExecucao.EmReparo);
        FinalizarTodos(ordem);
        ordem.AlterarStatus(StatusExecucao.Concluida);

        ordem.Status.Should().Be(StatusExecucao.Concluida);
        ordem.Historico.Select(h => h.StatusNovo).Should().Equal(
            StatusExecucao.NaFila, StatusExecucao.EmDiagnostico, StatusExecucao.EmReparo, StatusExecucao.Concluida);
    }

    [Fact]
    public void AlterarStatus_TransicaoInvalida_DeveLancar()
    {
        var ordem = NovaOrdem();

        var acao = () => ordem.AlterarStatus(StatusExecucao.EmReparo);

        acao.Should().Throw<TransicaoDeStatusInvalidaException>();
        ordem.Status.Should().Be(StatusExecucao.NaFila);
    }

    [Fact]
    public void AlterarStatus_ConcluirComServicoPendente_DeveLancar()
    {
        var ordem = NovaOrdem();
        ordem.AlterarStatus(StatusExecucao.EmDiagnostico);
        ordem.AlterarStatus(StatusExecucao.EmReparo);

        var acao = () => ordem.AlterarStatus(StatusExecucao.Concluida);

        acao.Should().Throw<TransicaoDeStatusInvalidaException>();
    }

    [Fact]
    public void AlterarStatus_FalhaSemMotivo_DeveLancar()
    {
        var acao = () => NovaOrdem().AlterarStatus(StatusExecucao.Falhou, " ");

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AlterarStatus_FalhaComMotivo_DeveGuardarMotivo()
    {
        var ordem = NovaOrdem();

        ordem.AlterarStatus(StatusExecucao.Falhou, "Peça errada");

        ordem.Status.Should().Be(StatusExecucao.Falhou);
        ordem.MotivoFalha.Should().Be("Peça errada");
        ordem.Historico.Last().Motivo.Should().Be("Peça errada");
    }

    [Fact]
    public void AlterarStatusItem_IniciarEFinalizar_DeveRegistrarTempos()
    {
        var ordem = NovaOrdem();
        var item = ordem.Itens.First();

        ordem.AlterarStatusItem(item.Id, StatusItemExecucao.Iniciado);
        ordem.AlterarStatusItem(item.Id, StatusItemExecucao.Finalizado);

        item.Status.Should().Be(StatusItemExecucao.Finalizado);
        item.IniciadaEm.Should().NotBeNull();
        item.FinalizadaEm.Should().NotBeNull();
    }

    [Fact]
    public void AlterarStatusItem_FinalizarSemIniciar_DeveLancar()
    {
        var ordem = NovaOrdem();

        var acao = () => ordem.AlterarStatusItem(ordem.Itens.First().Id, StatusItemExecucao.Finalizado);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AlterarStatusItem_IniciarDuasVezes_DeveLancar()
    {
        var ordem = NovaOrdem();
        var id = ordem.Itens.First().Id;
        ordem.AlterarStatusItem(id, StatusItemExecucao.Iniciado);

        var acao = () => ordem.AlterarStatusItem(id, StatusItemExecucao.Iniciado);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AlterarStatusItem_FinalizarDuasVezes_DeveLancar()
    {
        var ordem = NovaOrdem();
        var id = ordem.Itens.First().Id;
        ordem.AlterarStatusItem(id, StatusItemExecucao.Iniciado);
        ordem.AlterarStatusItem(id, StatusItemExecucao.Finalizado);

        var acao = () => ordem.AlterarStatusItem(id, StatusItemExecucao.Finalizado);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AlterarStatusItem_StatusCriada_DeveLancar()
    {
        var ordem = NovaOrdem();

        var acao = () => ordem.AlterarStatusItem(ordem.Itens.First().Id, StatusItemExecucao.Criada);

        acao.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AlterarStatusItem_ItemInexistente_DeveLancar()
    {
        var acao = () => NovaOrdem().AlterarStatusItem(Guid.NewGuid(), StatusItemExecucao.Iniciado);

        acao.Should().Throw<ItemExecucaoNaoEncontradoException>();
    }

    [Fact]
    public void AlterarStatusItem_ExecucaoEncerrada_DeveLancar()
    {
        var ordem = NovaOrdem();
        ordem.AlterarStatus(StatusExecucao.Falhou, "Cliente desistiu");

        var acao = () => ordem.AlterarStatusItem(ordem.Itens.First().Id, StatusItemExecucao.Iniciado);

        acao.Should().Throw<TransicaoDeStatusInvalidaException>();
    }

    [Fact]
    public void ItemExecucao_ComDadosInvalidos_DeveLancar()
    {
        var semServico = () => new ItemExecucao(Guid.NewGuid(), Guid.Empty, "Serviço");
        var semNome = () => new ItemExecucao(Guid.NewGuid(), Guid.NewGuid(), "");

        semServico.Should().Throw<ArgumentException>();
        semNome.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ItemReserva_ComDadosInvalidos_DeveLancar()
    {
        var semPeca = () => new ItemReserva(Guid.NewGuid(), Guid.Empty, 1);

        semPeca.Should().Throw<ArgumentException>();
    }
}
