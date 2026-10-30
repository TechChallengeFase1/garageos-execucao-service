using GarageOS.Contracts.V1;
using GarageOS.Execucao.Application.UseCases.Reservas;
using GarageOS.Execucao.Domain.Exceptions;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace GarageOS.Execucao.Infrastructure.Mensageria.Consumidores;

/// <summary>Compensação da saga: devolver ao disponível as peças reservadas</summary>
/// <remarks>
/// Idempotente. Sem reserva (a OS não chegou a reservar) ou já liberada, não
/// há o que desfazer: confirma do mesmo jeito.
/// </remarks>
public class LiberarReservaPecasConsumer : IConsumer<LiberarReservaPecas>
{
    private readonly LiberarReservaUseCase _liberar;
    private readonly ILogger<LiberarReservaPecasConsumer> _logger;

    public LiberarReservaPecasConsumer(LiberarReservaUseCase liberar, ILogger<LiberarReservaPecasConsumer> logger)
    {
        _liberar = liberar;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<LiberarReservaPecas> context)
    {
        var osId = context.Message.OrdemDeServicoId;
        try
        {
            await _liberar.ExecutarAsync(osId);
        }
        catch (ReservaNaoEncontradaException)
        {
            _logger.LogInformation("OS {OrdemDeServicoId} sem reserva de pecas: nada a liberar", osId);
        }
        catch (ReservaStatusInvalidoException ex)
        {
            // Já consumida: as peças foram usadas no reparo e não voltam sozinhas.
            _logger.LogWarning(ex, "Reserva da OS {OrdemDeServicoId} ja consumida; liberacao ignorada", osId);
        }

        await context.Publish(new ReservaLiberada(osId));
    }
}
