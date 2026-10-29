using GarageOS.Execucao.Application.DTOs.Estoques;
using GarageOS.Execucao.Application.Mappers;
using GarageOS.Execucao.Domain.Repositories;
using EstoqueEntity = GarageOS.Execucao.Domain.Entities.Estoque;

namespace GarageOS.Execucao.Application.UseCases.Estoques;

public class CadastrarEstoqueUseCase
{
    private readonly IEstoqueRepository _repository;

    public CadastrarEstoqueUseCase(IEstoqueRepository repository)
    {
        _repository = repository;
    }

    public async Task<EstoqueResponse> ExecutarAsync(CriarEstoqueRequest request)
    {
        var estoque = new EstoqueEntity(
            request.Nome,
            request.Quantidade,
            request.Valor,
            request.DataEntrada,
            request.Fornecedor,
            request.DataSaida);

        await _repository.AdicionarAsync(estoque);

        return estoque.ParaResponse();
    }
}
