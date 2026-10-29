using GarageOS.Execucao.Application.DTOs.Estoques;
using GarageOS.Execucao.Application.Mappers;
using GarageOS.Execucao.Domain.Exceptions;
using GarageOS.Execucao.Domain.Repositories;

namespace GarageOS.Execucao.Application.UseCases.Estoques;

public class ObterEstoqueUseCase
{
    private readonly IEstoqueRepository _repository;

    public ObterEstoqueUseCase(IEstoqueRepository repository)
    {
        _repository = repository;
    }

    public async Task<EstoqueResponse> ExecutarAsync(Guid id)
    {
        var estoque = await _repository.ObterPorIdAsync(id)
            ?? throw new EstoqueNaoEncontradoException(id);

        return estoque.ParaResponse();
    }
}
