using GarageOS.Contracts.V1;
using GarageOS.Execucao.Application.DTOs.Reservas;
using GarageOS.Execucao.Application.UseCases.Reservas;
using GarageOS.Execucao.Domain.Exceptions;
using MassTransit;

namespace GarageOS.Execucao.Infrastructure.Mensageria.Consumidores;

/// <summary>Passo da saga: reservar as peças da OS aprovada</summary>
/// <remarks>
/// Falta de peça não é erro técnico, é resposta de negócio: vira
/// PecasIndisponiveis e a saga compensa. Conflito de concorrência, banco fora
/// do ar e afins sobem como exceção, e o retry do MassTransit tenta de novo.
///
/// Repetida, a mensagem devolve a reserva que já existe (o use case é
/// idempotente por OS) e publica PecasReservadas de novo, o que a saga ignora.
/// </remarks>
public class ReservarPecasConsumer : IConsumer<ReservarPecas>
{
    private readonly ReservarPecasUseCase _reservar;

    public ReservarPecasConsumer(ReservarPecasUseCase reservar)
    {
        _reservar = reservar;
    }

    public async Task Consume(ConsumeContext<ReservarPecas> context)
    {
        var pedido = context.Message;
        try
        {
            await _reservar.ExecutarAsync(new ReservarPecasRequest
            {
                OrdemDeServicoId = pedido.OrdemDeServicoId,
                Itens = pedido.Pecas.Select(p => new ItemReservaRequest { EstoqueId = p.EstoqueId, Quantidade = p.Quantidade }).ToList()
            });
        }
        catch (Exception ex) when (ex is EstoqueInsuficienteException or EstoqueNaoEncontradoException)
        {
            await context.Publish(new PecasIndisponiveis(pedido.OrdemDeServicoId, ex.Message));
            return;
        }

        await context.Publish(new PecasReservadas(pedido.OrdemDeServicoId));
    }
}
