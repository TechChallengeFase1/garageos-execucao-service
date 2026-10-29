using GarageOS.Execucao.Domain.Exceptions;
using GarageOS.Execucao.Domain.Repositories;

namespace GarageOS.Execucao.Application.UseCases.Reservas;

/// <summary>Baixa definitiva das peças reservadas, quando a execução termina</summary>
/// <remarks>Idempotente: consumir uma reserva já consumida não faz nada.</remarks>
public class ConsumirReservaUseCase
{
    private readonly IReservaPecasRepository _reservaRepository;
    private readonly IEstoqueRepository _estoqueRepository;

    public ConsumirReservaUseCase(IReservaPecasRepository reservaRepository, IEstoqueRepository estoqueRepository)
    {
        _reservaRepository = reservaRepository;
        _estoqueRepository = estoqueRepository;
    }

    public async Task ExecutarAsync(Guid ordemDeServicoId)
    {
        var reserva = await _reservaRepository.ObterPorOrdemDeServicoIdAsync(ordemDeServicoId)
            ?? throw new ReservaNaoEncontradaException(ordemDeServicoId);

        if (!reserva.Consumir())
            return;

        var estoques = await _estoqueRepository.ObterPorIdsAsync(reserva.Itens.Select(i => i.EstoqueId));
        foreach (var item in reserva.Itens)
            estoques.First(e => e.Id == item.EstoqueId).ConsumirReserva(item.Quantidade);

        await _reservaRepository.AtualizarAsync(reserva);
    }
}
