using GarageOS.Contracts.V1;
using GarageOS.Execucao.Application.Abstractions;
using GarageOS.Execucao.Application.DTOs.Execucoes;
using GarageOS.Execucao.Application.Mappers;
using GarageOS.Execucao.Domain.Enums;
using GarageOS.Execucao.Domain.Exceptions;
using GarageOS.Execucao.Domain.Repositories;
using GarageOS.Execucao.Domain.Utils;

namespace GarageOS.Execucao.Application.UseCases.Execucoes;

/// <summary>Avança a execução: diagnóstico, reparo, conclusão ou falha</summary>
/// <remarks>
/// Conclusão e falha são os dois finais que a saga da OS espera ouvir:
///
///   Concluida -> dá baixa definitiva nas peças reservadas e publica ExecucaoFinalizada
///   Falhou    -> publica ExecucaoFalhou; a reserva fica ativa e a saga manda liberar
///
/// Tudo na mesma transação: a mudança de status, a baixa e a mensagem no outbox.
/// </remarks>
public class AlterarStatusExecucaoUseCase
{
    private readonly IOrdemDeExecucaoRepository _repository;
    private readonly IReservaPecasRepository _reservas;
    private readonly IEstoqueRepository _estoques;
    private readonly IPublicadorDeEventos _publicador;
    private readonly IMetricasDeNegocio _metricas;

    public AlterarStatusExecucaoUseCase(
        IOrdemDeExecucaoRepository repository,
        IReservaPecasRepository reservas,
        IEstoqueRepository estoques,
        IPublicadorDeEventos publicador,
        IMetricasDeNegocio metricas)
    {
        _repository = repository;
        _reservas = reservas;
        _estoques = estoques;
        _publicador = publicador;
        _metricas = metricas;
    }

    public async Task<OrdemDeExecucaoResponse> ExecutarAsync(Guid id, AlterarStatusExecucaoRequest request)
    {
        var ordem = await _repository.ObterPorIdAsync(id)
            ?? throw new OrdemDeExecucaoNaoEncontradaException(id);

        var statusAnterior = ordem.Status;
        var desde = ordem.AtualizadaEm;

        ordem.AlterarStatus(request.Status, request.Motivo);

        if (ordem.Status == StatusExecucao.Concluida)
        {
            await ConsumirPecasAsync(ordem.OrdemDeServicoId);
            await _publicador.PublicarAsync(new ExecucaoFinalizada(ordem.OrdemDeServicoId));
        }
        else if (ordem.Status == StatusExecucao.Falhou)
        {
            await _publicador.PublicarAsync(new ExecucaoFalhou(ordem.OrdemDeServicoId, ordem.MotivoFalha!));
        }

        await _repository.AtualizarAsync(ordem);

        _metricas.ExecucaoStatusAlterado(
            ordem.NumeroOS,
            statusAnterior.ToString(),
            ordem.Status.ToString(),
            (BrasiliaTime.Agora - desde).TotalMinutes);

        return ordem.ParaResponse();
    }

    /// <remarks>OS sem peças não tem reserva: nada a baixar.</remarks>
    private async Task ConsumirPecasAsync(Guid ordemDeServicoId)
    {
        var reserva = await _reservas.ObterPorOrdemDeServicoIdAsync(ordemDeServicoId);
        if (reserva is null || !reserva.Consumir())
            return;

        var estoques = await _estoques.ObterPorIdsAsync(reserva.Itens.Select(i => i.EstoqueId));
        foreach (var item in reserva.Itens)
            estoques.First(e => e.Id == item.EstoqueId).ConsumirReserva(item.Quantidade);
    }
}
