using GarageOS.Execucao.Application.DTOs.Execucoes;
using GarageOS.Execucao.Application.Mappers;
using GarageOS.Execucao.Domain.Entities;
using GarageOS.Execucao.Domain.Repositories;

namespace GarageOS.Execucao.Application.UseCases.Execucoes;

/// <summary>Coloca uma OS na fila de execução</summary>
/// <remarks>Idempotente por OS: a mesma OS não entra duas vezes na fila.</remarks>
public class CriarOrdemDeExecucaoUseCase
{
    private readonly IOrdemDeExecucaoRepository _repository;

    public CriarOrdemDeExecucaoUseCase(IOrdemDeExecucaoRepository repository)
    {
        _repository = repository;
    }

    /// <returns>A ordem de execução e se ela foi criada agora (false = já existia)</returns>
    public async Task<(OrdemDeExecucaoResponse Ordem, bool Criada)> ExecutarAsync(CriarOrdemDeExecucaoRequest request)
    {
        var existente = await _repository.ObterPorOrdemDeServicoIdAsync(request.OrdemDeServicoId);
        if (existente is not null)
            return (existente.ParaResponse(), false);

        var ordem = new OrdemDeExecucao(
            request.OrdemDeServicoId,
            request.NumeroOS,
            request.FilialId,
            request.Itens.Select(i => (i.ServicoId, i.Nome)));

        await _repository.AdicionarAsync(ordem);

        return (ordem.ParaResponse(), true);
    }
}
