using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GarageOS.Execucao.Application.DTOs.Execucoes;
using GarageOS.Execucao.Domain.Enums;
using GarageOS.Execucao.IntegrationTests.Fixtures;
using GarageOS.Execucao.IntegrationTests.Helpers;

namespace GarageOS.Execucao.IntegrationTests.Api.Controllers;

public class ExecucoesControllerTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ExecucoesControllerTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<OrdemDeExecucaoResponse> CriarAsync(params string[] servicos)
    {
        var response = await _client.PostAsJsonAsync("/api/execucoes", new CriarOrdemDeExecucaoRequest
        {
            OrdemDeServicoId = Guid.NewGuid(),
            NumeroOS = "OS-2026-00100",
            Itens = servicos.Select(n => new ItemExecucaoRequest { ServicoId = Guid.NewGuid(), Nome = n }).ToList()
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<OrdemDeExecucaoResponse>(JsonDefaults.Options))!;
    }

    private Task<HttpResponseMessage> AlterarStatusAsync(Guid id, StatusExecucao status, string? motivo = null) =>
        _client.PatchAsJsonAsync($"/api/execucoes/{id}/status",
            new AlterarStatusExecucaoRequest { Status = status, Motivo = motivo }, JsonDefaults.Options);

    private Task<HttpResponseMessage> AlterarItemAsync(Guid id, Guid itemId, StatusItemExecucao status) =>
        _client.PatchAsJsonAsync($"/api/execucoes/{id}/itens/{itemId}/status",
            new AlterarStatusItemExecucaoRequest { Status = status }, JsonDefaults.Options);

    [Fact]
    public async Task POST_Criar_MesmaOSDuasVezes_DeveDevolverAExistente()
    {
        var request = new CriarOrdemDeExecucaoRequest
        {
            OrdemDeServicoId = Guid.NewGuid(),
            NumeroOS = "OS-2026-00101",
            Itens = [new ItemExecucaoRequest { ServicoId = Guid.NewGuid(), Nome = "Revisão" }]
        };

        var primeira = await _client.PostAsJsonAsync("/api/execucoes", request);
        var segunda = await _client.PostAsJsonAsync("/api/execucoes", request);

        primeira.StatusCode.Should().Be(HttpStatusCode.Created);
        segunda.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task POST_Criar_Invalido_DeveRetornar400()
    {
        var response = await _client.PostAsJsonAsync("/api/execucoes", new CriarOrdemDeExecucaoRequest());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CicloCompleto_DeveConcluirERegistrarHistoricoEAging()
    {
        var ordem = await CriarAsync("Troca de óleo");
        var itemId = ordem.Itens.Single().Id;

        (await AlterarStatusAsync(ordem.Id, StatusExecucao.EmDiagnostico)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await AlterarStatusAsync(ordem.Id, StatusExecucao.EmReparo)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await AlterarItemAsync(ordem.Id, itemId, StatusItemExecucao.Iniciado)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await AlterarItemAsync(ordem.Id, itemId, StatusItemExecucao.Finalizado)).StatusCode.Should().Be(HttpStatusCode.OK);
        var concluir = await AlterarStatusAsync(ordem.Id, StatusExecucao.Concluida);

        concluir.StatusCode.Should().Be(HttpStatusCode.OK);
        var final = await _client.GetFromJsonAsync<OrdemDeExecucaoResponse>($"/api/execucoes/{ordem.Id}", JsonDefaults.Options);
        final!.Status.Should().Be("Concluida");
        final.Historico.Select(h => h.StatusNovo).Should().Equal("NaFila", "EmDiagnostico", "EmReparo", "Concluida");

        var aging = await _client.GetFromJsonAsync<List<AgingServicoResponse>>("/api/execucoes/aging", JsonDefaults.Options);
        aging.Should().Contain(a => a.ServicoNome == "Troca de óleo" && a.TotalExecucoes >= 1);
    }

    [Fact]
    public async Task PATCH_Status_TransicaoInvalida_DeveRetornar409()
    {
        var ordem = await CriarAsync("Alinhamento");

        var response = await AlterarStatusAsync(ordem.Id, StatusExecucao.EmReparo);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PATCH_Status_FalhaSemMotivo_DeveRetornar400()
    {
        var ordem = await CriarAsync("Alinhamento");

        var response = await AlterarStatusAsync(ordem.Id, StatusExecucao.Falhou);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PATCH_Item_Inexistente_DeveRetornar404()
    {
        var ordem = await CriarAsync("Balanceamento");

        var response = await AlterarItemAsync(ordem.Id, Guid.NewGuid(), StatusItemExecucao.Iniciado);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_Fila_SemFiltro_NaoDeveTrazerEncerradas()
    {
        var falhou = await CriarAsync("Diagnóstico");
        await AlterarStatusAsync(falhou.Id, StatusExecucao.Falhou, "Cliente desistiu");
        var aberta = await CriarAsync("Revisão");

        var fila = await _client.GetFromJsonAsync<List<OrdemDeExecucaoResponse>>("/api/execucoes", JsonDefaults.Options);
        var falhas = await _client.GetFromJsonAsync<List<OrdemDeExecucaoResponse>>("/api/execucoes?status=Falhou", JsonDefaults.Options);

        fila.Should().Contain(o => o.Id == aberta.Id);
        fila.Should().NotContain(o => o.Id == falhou.Id);
        falhas.Should().Contain(o => o.Id == falhou.Id);
    }

    [Fact]
    public async Task GET_Execucao_Inexistente_DeveRetornar404()
    {
        var response = await _client.GetAsync($"/api/execucoes/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
