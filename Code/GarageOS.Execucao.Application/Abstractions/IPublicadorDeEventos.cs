namespace GarageOS.Execucao.Application.Abstractions;

/// <summary>Publicação de eventos de integração para os outros serviços</summary>
/// <remarks>
/// Os use cases conhecem só esta interface; o MassTransit e o SQS ficam na
/// Infrastructure, no mesmo padrão do IMetricasDeNegocio.
///
/// Aqui a publicação passa pelo outbox do MassTransit no PostgreSQL do Execução: a mensagem é gravada na mesma transação da mudança e só sai depois do commit.
/// </remarks>
public interface IPublicadorDeEventos
{
    Task PublicarAsync<T>(T mensagem, CancellationToken cancellationToken = default) where T : class;
}
