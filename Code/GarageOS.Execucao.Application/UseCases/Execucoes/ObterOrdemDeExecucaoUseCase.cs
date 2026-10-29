using GarageOS.Execucao.Application.DTOs.Execucoes;
using GarageOS.Execucao.Application.Mappers;
using GarageOS.Execucao.Domain.Exceptions;
using GarageOS.Execucao.Domain.Repositories;

namespace GarageOS.Execucao.Application.UseCases.Execucoes;

public class ObterOrdemDeExecucaoUseCase
{
    private readonly IOrdemDeExecucaoRepository _repository;

    public ObterOrdemDeExecucaoUseCase(IOrdemDeExecucaoRepository repository)
    {
        _repository = repository;
    }

    public async Task<OrdemDeExecucaoResponse> ExecutarAsync(Guid id)
    {
        var ordem = await _repository.ObterPorIdAsync(id)
            ?? throw new OrdemDeExecucaoNaoEncontradaException(id);

        return ordem.ParaResponse();
    }
}
