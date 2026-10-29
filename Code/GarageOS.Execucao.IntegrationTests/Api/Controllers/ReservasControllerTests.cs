using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GarageOS.Execucao.Application.DTOs.Estoques;
using GarageOS.Execucao.Application.DTOs.Reservas;
using GarageOS.Execucao.IntegrationTests.Fixtures;
using GarageOS.Execucao.IntegrationTests.Helpers;

namespace GarageOS.Execucao.IntegrationTests.Api.Controllers;

public class ReservasControllerTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ReservasControllerTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<EstoqueResponse> CriarEstoqueAsync(int quantidade)
    {
        var response = await _client.PostAsJsonAsync("/api/estoques", new CriarEstoqueRequest
        {
            Nome = $"Peça {Guid.NewGuid():N}"[..20],
            Quantidade = quantidade,
            Valor = 80m,
            DataEntrada = DateTime.UtcNow,
            Fornecedor = "Fornecedor"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EstoqueResponse>(JsonDefaults.Options))!;
    }

    private async Task<EstoqueResponse> ObterEstoqueAsync(Guid id) =>
        (await _client.GetFromJsonAsync<EstoqueResponse>($"/api/estoques/{id}", JsonDefaults.Options))!;

    private static ReservarPecasRequest Pedido(Guid osId, params (Guid Id, int Qtd)[] itens) => new()
    {
        OrdemDeServicoId = osId,
        Itens = itens.Select(i => new ItemReservaRequest { EstoqueId = i.Id, Quantidade = i.Qtd }).ToList()
    };

    [Fact]
    public async Task POST_Reservar_ComSaldo_DeveRetornar201ESepararAsPecas()
    {
        var peca = await CriarEstoqueAsync(10);
        var osId = Guid.NewGuid();

        var response = await _client.PostAsJsonAsync("/api/reservas", Pedido(osId, (peca.Id, 4)));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var estoque = await ObterEstoqueAsync(peca.Id);
        estoque.QuantidadeReservada.Should().Be(4);
        estoque.QuantidadeDisponivel.Should().Be(6);
        estoque.Quantidade.Should().Be(10);
    }

    [Fact]
    public async Task POST_Reservar_DuasVezes_DeveSerIdempotente()
    {
        var peca = await CriarEstoqueAsync(10);
        var osId = Guid.NewGuid();

        await _client.PostAsJsonAsync("/api/reservas", Pedido(osId, (peca.Id, 2)));
        var segunda = await _client.PostAsJsonAsync("/api/reservas", Pedido(osId, (peca.Id, 2)));

        segunda.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ObterEstoqueAsync(peca.Id)).QuantidadeReservada.Should().Be(2);
    }

    [Fact]
    public async Task POST_Reservar_UmaPecaSemSaldo_DeveRetornar409ENaoReservarNenhuma()
    {
        var comSaldo = await CriarEstoqueAsync(10);
        var semSaldo = await CriarEstoqueAsync(1);

        var response = await _client.PostAsJsonAsync("/api/reservas",
            Pedido(Guid.NewGuid(), (comSaldo.Id, 3), (semSaldo.Id, 2)));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ObterEstoqueAsync(comSaldo.Id)).QuantidadeReservada.Should().Be(0);
        (await ObterEstoqueAsync(semSaldo.Id)).QuantidadeReservada.Should().Be(0);
    }

    [Fact]
    public async Task POST_Reservar_PecaInexistente_DeveRetornar404()
    {
        var response = await _client.PostAsJsonAsync("/api/reservas", Pedido(Guid.NewGuid(), (Guid.NewGuid(), 1)));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_Reservar_SemItens_DeveRetornar400()
    {
        var response = await _client.PostAsJsonAsync("/api/reservas", new ReservarPecasRequest { OrdemDeServicoId = Guid.NewGuid() });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Reservar_PedidosSimultaneos_NaoDevemReservarMaisQueOSaldo()
    {
        // 5 OS disputando a mesma peça, que só tem saldo para 3.
        var peca = await CriarEstoqueAsync(3);

        var respostas = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ =>
            _client.PostAsJsonAsync("/api/reservas", Pedido(Guid.NewGuid(), (peca.Id, 1)))));

        var aceitas = respostas.Count(r => r.StatusCode == HttpStatusCode.Created);
        respostas.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.Created || r.StatusCode == HttpStatusCode.Conflict);
        var estoque = await ObterEstoqueAsync(peca.Id);
        estoque.QuantidadeReservada.Should().Be(aceitas);
        estoque.QuantidadeReservada.Should().BeLessThanOrEqualTo(3);
    }

    [Fact]
    public async Task Liberar_DeveDevolverOSaldo_EConsumirDepoisDeveFalhar()
    {
        var peca = await CriarEstoqueAsync(5);
        var osId = Guid.NewGuid();
        await _client.PostAsJsonAsync("/api/reservas", Pedido(osId, (peca.Id, 5)));

        var liberar = await _client.PostAsync($"/api/reservas/{osId}/liberar", null);
        var liberarDeNovo = await _client.PostAsync($"/api/reservas/{osId}/liberar", null);
        var consumir = await _client.PostAsync($"/api/reservas/{osId}/consumir", null);

        liberar.StatusCode.Should().Be(HttpStatusCode.NoContent);
        liberarDeNovo.StatusCode.Should().Be(HttpStatusCode.NoContent);
        consumir.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ObterEstoqueAsync(peca.Id)).QuantidadeDisponivel.Should().Be(5);
    }

    [Fact]
    public async Task Consumir_DeveDarBaixaDefinitivaUmaVezSo()
    {
        var peca = await CriarEstoqueAsync(8);
        var osId = Guid.NewGuid();
        await _client.PostAsJsonAsync("/api/reservas", Pedido(osId, (peca.Id, 3)));

        await _client.PostAsync($"/api/reservas/{osId}/consumir", null);
        var deNovo = await _client.PostAsync($"/api/reservas/{osId}/consumir", null);

        deNovo.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var estoque = await ObterEstoqueAsync(peca.Id);
        estoque.Quantidade.Should().Be(5);
        estoque.QuantidadeReservada.Should().Be(0);
    }

    [Fact]
    public async Task GET_Reserva_Inexistente_DeveRetornar404()
    {
        var response = await _client.GetAsync($"/api/reservas/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_EstoqueReservado_DeveRetornar409()
    {
        var peca = await CriarEstoqueAsync(2);
        await _client.PostAsJsonAsync("/api/reservas", Pedido(Guid.NewGuid(), (peca.Id, 1)));

        var response = await _client.DeleteAsync($"/api/estoques/{peca.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
