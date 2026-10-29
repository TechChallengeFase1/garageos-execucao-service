using GarageOS.Execucao.Domain.Entities;
using GarageOS.Execucao.Domain.Enums;
using GarageOS.Execucao.Domain.ReadModels;

namespace GarageOS.Execucao.Domain.Repositories;

public interface IOrdemDeExecucaoRepository
{
    Task<OrdemDeExecucao?> ObterPorIdAsync(Guid id);
    Task<OrdemDeExecucao?> ObterPorOrdemDeServicoIdAsync(Guid ordemDeServicoId);
    Task<IReadOnlyList<OrdemDeExecucao>> ListarFilaAsync(StatusExecucao? status);
    Task AdicionarAsync(OrdemDeExecucao ordem);
    Task AtualizarAsync(OrdemDeExecucao ordem);

    /// <summary>Tempo médio de execução por serviço, agregado no banco</summary>
    Task<IReadOnlyList<AgingServico>> CalcularAgingAsync();
}
