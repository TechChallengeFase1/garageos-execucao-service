using GarageOS.Contracts.V1;
using GarageOS.Execucao.Application.DTOs.Execucoes;
using GarageOS.Execucao.Application.UseCases.Execucoes;
using MassTransit;

namespace GarageOS.Execucao.Infrastructure.Mensageria.Consumidores;

/// <summary>Passo da saga: pagamento confirmado, a OS entra na fila da oficina</summary>
/// <remarks>Idempotente: a mesma OS não entra duas vezes na fila.</remarks>
public class IniciarExecucaoConsumer : IConsumer<IniciarExecucao>
{
    private readonly CriarOrdemDeExecucaoUseCase _criar;

    public IniciarExecucaoConsumer(CriarOrdemDeExecucaoUseCase criar)
    {
        _criar = criar;
    }

    public async Task Consume(ConsumeContext<IniciarExecucao> context)
    {
        var pedido = context.Message;

        var (ordem, _) = await _criar.ExecutarAsync(new CriarOrdemDeExecucaoRequest
        {
            OrdemDeServicoId = pedido.OrdemDeServicoId,
            NumeroOS = pedido.NumeroOS,
            Itens = pedido.Servicos.Select(s => new ItemExecucaoRequest { ServicoId = s.ServicoId, Nome = s.Nome }).ToList()
        });

        await context.Publish(new ExecucaoIniciada(pedido.OrdemDeServicoId, ordem.Id));
    }
}
