using GarageOS.Execucao.Domain.Exceptions;
using GarageOS.Execucao.Domain.Repositories;

namespace GarageOS.Execucao.Application.UseCases.Reservas;

/// <summary>Devolve ao disponível as peças reservadas para uma OS (compensação)</summary>
/// <remarks>Idempotente: liberar uma reserva já liberada não faz nada.</remarks>
public class LiberarReservaUseCase
{
    private readonly IReservaPecasRepository _reservaRepository;
    private readonly IEstoqueRepository _estoqueRepository;

    public LiberarReservaUseCase(IReservaPecasRepository reservaRepository, IEstoqueRepository estoqueRepository)
    {
        _reservaRepository = reservaRepository;
        _estoqueRepository = estoqueRepository;
    }

    public async Task ExecutarAsync(Guid ordemDeServicoId)
    {
        var reserva = await _reservaRepository.ObterPorOrdemDeServicoIdAsync(ordemDeServicoId)
            ?? throw new ReservaNaoEncontradaException(ordemDeServicoId);

        if (!reserva.Liberar())
            return;

        var estoques = await _estoqueRepository.ObterPorIdsAsync(reserva.Itens.Select(i => i.EstoqueId));
        foreach (var item in reserva.Itens)
            estoques.First(e => e.Id == item.EstoqueId).LiberarReserva(item.Quantidade);

        await _reservaRepository.AtualizarAsync(reserva);
    }
}
