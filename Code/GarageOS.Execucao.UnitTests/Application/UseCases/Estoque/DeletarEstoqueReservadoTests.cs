using FluentAssertions;
using GarageOS.Execucao.Application.UseCases.Estoques;
using GarageOS.Execucao.Domain.Repositories;
using Moq;
using EstoqueEntity = GarageOS.Execucao.Domain.Entities.Estoque;

namespace GarageOS.Execucao.UnitTests.Application.UseCases.Estoque;

public class DeletarEstoqueReservadoTests
{
    [Fact]
    public async Task ExecutarAsync_ComUnidadesReservadas_NaoDeveRemover()
    {
        var repository = new Mock<IEstoqueRepository>();
        var estoque = new EstoqueEntity("Pneu", 4, 300m, DateTime.UtcNow, "Pirelli");
        estoque.Reservar(1);
        repository.Setup(r => r.ObterPorIdAsync(estoque.Id)).ReturnsAsync(estoque);

        var acao = () => new DeletarEstoqueUseCase(repository.Object).ExecutarAsync(estoque.Id);

        await acao.Should().ThrowAsync<InvalidOperationException>();
        repository.Verify(r => r.RemoverAsync(It.IsAny<EstoqueEntity>()), Times.Never);
    }
}
