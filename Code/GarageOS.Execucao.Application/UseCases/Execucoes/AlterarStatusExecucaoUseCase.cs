using GarageOS.Execucao.Application.Abstractions;
using GarageOS.Execucao.Application.DTOs.Execucoes;
using GarageOS.Execucao.Application.Mappers;
using GarageOS.Execucao.Domain.Exceptions;
using GarageOS.Execucao.Domain.Repositories;
using GarageOS.Execucao.Domain.Utils;

namespace GarageOS.Execucao.Application.UseCases.Execucoes;

/// <summary>Avança a execução: diagnóstico, reparo, conclusão ou falha</summary>
public class AlterarStatusExecucaoUseCase
{
    private readonly IOrdemDeExecucaoRepository _repository;
    private readonly IMetricasDeNegocio _metricas;

    public AlterarStatusExecucaoUseCase(IOrdemDeExecucaoRepository repository, IMetricasDeNegocio metricas)
    {
        _repository = repository;
        _metricas = metricas;
    }

    public async Task<OrdemDeExecucaoResponse> ExecutarAsync(Guid id, AlterarStatusExecucaoRequest request)
    {
        var ordem = await _repository.ObterPorIdAsync(id)
            ?? throw new OrdemDeExecucaoNaoEncontradaException(id);

        var statusAnterior = ordem.Status;
        var desde = ordem.AtualizadaEm;

        ordem.AlterarStatus(request.Status, request.Motivo);

        await _repository.AtualizarAsync(ordem);

        _metricas.ExecucaoStatusAlterado(
            ordem.NumeroOS,
            statusAnterior.ToString(),
            ordem.Status.ToString(),
            (BrasiliaTime.Agora - desde).TotalMinutes);

        return ordem.ParaResponse();
    }
}
