using Amazon.SimpleNotificationService;
using Amazon.SQS;
using GarageOS.Execucao.Infrastructure.Data;
using GarageOS.Execucao.Infrastructure.Mensageria.Consumidores;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GarageOS.Execucao.Infrastructure.Mensageria;

/// <summary>Registro do MassTransit deste serviço (ADR 0006)</summary>
public static class MensageriaExtensions
{
    /// <summary>Serviço dono das filas: compõe o nome garageos-{ambiente}-execucao-*</summary>
    public const string Servico = "execucao";

    /// <remarks>
    /// Nomes por convenção, que é o que a policy IAM do pod libera:
    ///   filas   garageos-{ambiente}-execucao-{consumidor}  (e as _error, a DLQ do MassTransit)
    ///   tópicos garageos-{ambiente}-{EntityName da mensagem}
    ///
    /// Retry com intervalo crescente antes de mandar para a _error: falha
    /// transitória (banco piscando, outro serviço reiniciando) se resolve
    /// sozinha; o que sobra fica parado na fila de erro, visível no console.
    /// </remarks>
    public static IServiceCollection AddMensageria(this IServiceCollection services, IConfiguration configuration)
    {
        var opcoes = configuration.GetSection(MensageriaOptions.Secao).Get<MensageriaOptions>() ?? new MensageriaOptions();
        services.Configure<MensageriaOptions>(configuration.GetSection(MensageriaOptions.Secao));

        services.AddMassTransit(x =>
        {
            x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter($"{opcoes.Prefixo}-{Servico}", false));

            x.AddConsumer<ReservarPecasConsumer>();
            x.AddConsumer<LiberarReservaPecasConsumer>();
            x.AddConsumer<IniciarExecucaoConsumer>();

            // Outbox transacional no PostgreSQL do Execução. O inbox descarta a
            // mesma mensagem entregue duas vezes (mesmo MessageId), e o que os
            // consumidores publicam só sai depois do commit da transação.
            x.AddEntityFrameworkOutbox<ExecucaoDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });

            x.AddConfigureEndpointsCallback((context, _, cfg) =>
                cfg.UseEntityFrameworkOutbox<ExecucaoDbContext>(context));

            if (opcoes.EmMemoria)
            {
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseMessageRetry(r => r.Intervals(100, 500, 1000));
                    cfg.ConfigureEndpoints(context);
                });
                return;
            }

            x.UsingAmazonSqs((context, cfg) =>
            {
                cfg.Host(opcoes.Regiao, h =>
                {
                    if (string.IsNullOrWhiteSpace(opcoes.ServiceUrl))
                        return;

                    // LocalStack: mesmo endpoint para SQS e SNS, credencial qualquer.
                    h.Config(new AmazonSQSConfig { ServiceURL = opcoes.ServiceUrl });
                    h.Config(new AmazonSimpleNotificationServiceConfig { ServiceURL = opcoes.ServiceUrl });
                    h.AccessKey("local");
                    h.SecretKey("local");
                });

                cfg.MessageTopology.SetEntityNameFormatter(
                    new PrefixEntityNameFormatter(cfg.MessageTopology.EntityNameFormatter, $"{opcoes.Prefixo}-"));

                cfg.UseMessageRetry(r => r.Intervals(500, 2000, 5000, 15000));
                cfg.ConfigureEndpoints(context);
            });
        });


        services.AddScoped<GarageOS.Execucao.Application.Abstractions.IPublicadorDeEventos, PublicadorDeEventos>();

        return services;
    }
}
