using FluentAssertions;
using GarageOS.Execucao.Application.DTOs.Execucoes;
using GarageOS.Execucao.Application.DTOs.Reservas;
using GarageOS.Execucao.Application.Validators.Execucoes;
using GarageOS.Execucao.Application.Validators.Reservas;
using GarageOS.Execucao.Domain.Enums;

namespace GarageOS.Execucao.UnitTests.Application.Validators;

public class ValidatorsDaExecucaoTests
{
    [Fact]
    public void Reservar_Valido_DevePassar()
    {
        var request = new ReservarPecasRequest
        {
            OrdemDeServicoId = Guid.NewGuid(),
            Itens = [new ItemReservaRequest { EstoqueId = Guid.NewGuid(), Quantidade = 1 }]
        };

        new ReservarPecasValidator().Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Reservar_Invalido_DeveApontarCadaErro()
    {
        var request = new ReservarPecasRequest
        {
            OrdemDeServicoId = Guid.Empty,
            Itens = [new ItemReservaRequest { EstoqueId = Guid.Empty, Quantidade = 0 }]
        };

        var resultado = new ReservarPecasValidator().Validate(request);

        resultado.Errors.Select(e => e.ErrorMessage).Should().Contain([
            "Ordem de serviço é obrigatória.",
            "Item de estoque é obrigatório.",
            "Quantidade deve ser maior que zero."
        ]);
    }

    [Fact]
    public void Reservar_SemItens_DeveFalhar()
    {
        var resultado = new ReservarPecasValidator().Validate(new ReservarPecasRequest { OrdemDeServicoId = Guid.NewGuid() });

        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public void CriarExecucao_Valida_DevePassar()
    {
        var request = new CriarOrdemDeExecucaoRequest
        {
            OrdemDeServicoId = Guid.NewGuid(),
            NumeroOS = "OS-2026-00001",
            Itens = [new ItemExecucaoRequest { ServicoId = Guid.NewGuid(), Nome = "Revisão" }]
        };

        new CriarOrdemDeExecucaoValidator().Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CriarExecucao_Invalida_DeveFalhar()
    {
        var request = new CriarOrdemDeExecucaoRequest
        {
            OrdemDeServicoId = Guid.Empty,
            NumeroOS = "",
            Itens = [new ItemExecucaoRequest { ServicoId = Guid.Empty, Nome = "" }]
        };

        new CriarOrdemDeExecucaoValidator().Validate(request).Errors.Should().HaveCountGreaterThanOrEqualTo(4);
    }

    [Theory]
    [InlineData(StatusExecucao.EmDiagnostico, null, true)]
    [InlineData(StatusExecucao.Falhou, "Peça errada", true)]
    [InlineData(StatusExecucao.Falhou, null, false)]
    [InlineData(StatusExecucao.NaFila, null, false)]
    [InlineData((StatusExecucao)99, null, false)]
    public void AlterarStatus_DeveValidar(StatusExecucao status, string? motivo, bool valido)
    {
        var request = new AlterarStatusExecucaoRequest { Status = status, Motivo = motivo };

        new AlterarStatusExecucaoValidator().Validate(request).IsValid.Should().Be(valido);
    }
}
