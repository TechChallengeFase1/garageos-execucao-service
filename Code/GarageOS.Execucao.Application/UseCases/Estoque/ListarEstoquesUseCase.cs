using GarageOS.Execucao.Application.DTOs.Estoques;
using GarageOS.Execucao.Application.Mappers;
using GarageOS.Execucao.Domain.Repositories;

namespace GarageOS.Execucao.Application.UseCases.Estoques;

public class ListarEstoquesUseCase
{
    private readonly IEstoqueRepository _repository;

    public ListarEstoquesUseCase(IEstoqueRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<EstoqueResponse>> ExecutarAsync()
    {
        var itens = await _repository.ListarTodosAsync();

        return itens.Select(e => e.ParaResponse());
    }
}
