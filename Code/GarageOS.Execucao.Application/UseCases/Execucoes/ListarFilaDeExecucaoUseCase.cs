using GarageOS.Execucao.Application.DTOs.Execucoes;
using GarageOS.Execucao.Application.Mappers;
using GarageOS.Execucao.Domain.Enums;
using GarageOS.Execucao.Domain.Repositories;

namespace GarageOS.Execucao.Application.UseCases.Execucoes;

/// <summary>Fila da oficina, na ordem em que deve ser atendida</summary>
public class ListarFilaDeExecucaoUseCase
{
    private readonly IOrdemDeExecucaoRepository _repository;

    public ListarFilaDeExecucaoUseCase(IOrdemDeExecucaoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<OrdemDeExecucaoResponse>> ExecutarAsync(StatusExecucao? status)
    {
        var fila = await _repository.ListarFilaAsync(status);

        return fila.Select(o => o.ParaResponse());
    }
}
