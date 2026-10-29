using GarageOS.Execucao.Application.DTOs.Reservas;
using GarageOS.Execucao.Application.Mappers;
using GarageOS.Execucao.Domain.Exceptions;
using GarageOS.Execucao.Domain.Repositories;

namespace GarageOS.Execucao.Application.UseCases.Reservas;

public class ObterReservaUseCase
{
    private readonly IReservaPecasRepository _repository;

    public ObterReservaUseCase(IReservaPecasRepository repository)
    {
        _repository = repository;
    }

    public async Task<ReservaPecasResponse> ExecutarAsync(Guid ordemDeServicoId)
    {
        var reserva = await _repository.ObterPorOrdemDeServicoIdAsync(ordemDeServicoId)
            ?? throw new ReservaNaoEncontradaException(ordemDeServicoId);

        return reserva.ParaResponse();
    }
}
