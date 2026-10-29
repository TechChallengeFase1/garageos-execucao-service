using GarageOS.Execucao.Application.DTOs.Execucoes;
using GarageOS.Execucao.Application.Mappers;
using GarageOS.Execucao.Domain.Exceptions;
using GarageOS.Execucao.Domain.Repositories;

namespace GarageOS.Execucao.Application.UseCases.Execucoes;

/// <summary>Inicia ou finaliza um serviço da execução</summary>
public class AlterarStatusItemExecucaoUseCase
{
    private readonly IOrdemDeExecucaoRepository _repository;

    public AlterarStatusItemExecucaoUseCase(IOrdemDeExecucaoRepository repository)
    {
        _repository = repository;
    }

    public async Task<OrdemDeExecucaoResponse> ExecutarAsync(Guid id, Guid itemId, AlterarStatusItemExecucaoRequest request)
    {
        var ordem = await _repository.ObterPorIdAsync(id)
            ?? throw new OrdemDeExecucaoNaoEncontradaException(id);

        ordem.AlterarStatusItem(itemId, request.Status);

        await _repository.AtualizarAsync(ordem);

        return ordem.ParaResponse();
    }
}
