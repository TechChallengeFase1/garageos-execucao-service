using GarageOS.Execucao.Application.DTOs.Estoques;
using GarageOS.Execucao.Application.Mappers;
using GarageOS.Execucao.Domain.Exceptions;
using GarageOS.Execucao.Domain.Repositories;

namespace GarageOS.Execucao.Application.UseCases.Estoques;

public class AlterarEstoqueUseCase
{
    private readonly IEstoqueRepository _repository;

    public AlterarEstoqueUseCase(IEstoqueRepository repository)
    {
        _repository = repository;
    }

    public async Task<EstoqueResponse> ExecutarAsync(Guid id, AtualizarEstoqueRequest request)
    {
        var estoque = await _repository.ObterPorIdAsync(id)
            ?? throw new EstoqueNaoEncontradoException(id);

        estoque.Atualizar(
            request.Nome,
            request.Quantidade,
            request.Valor,
            request.DataEntrada,
            request.Fornecedor,
            request.DataSaida);

        await _repository.AtualizarAsync(estoque);

        return estoque.ParaResponse();
    }
}
