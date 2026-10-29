using GarageOS.Execucao.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace GarageOS.Execucao.Infrastructure.Observabilidade;

/// <summary>Publica os eventos de negócio como custom events do New Relic</summary>
/// <remarks>
/// Custom events, e não métricas, de propósito: eventos permitem consulta por
/// NRQL com FACET e agregação arbitrária depois do fato.
///
/// Nenhuma chamada aqui lança: observabilidade quebrada não pode derrubar uma
/// execução. Falha vira log e a operação segue.
/// </remarks>
public class MetricasNewRelic : IMetricasDeNegocio
{
    private readonly ILogger<MetricasNewRelic> _logger;

    /// <summary>Inicializa a publicação de métricas com o logger de diagnóstico</summary>
    public MetricasNewRelic(ILogger<MetricasNewRelic> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void ExecucaoStatusAlterado(string numeroOS, string statusAnterior, string statusNovo, double minutosNoStatusAnterior) =>
        Registrar("ExecucaoStatus", new Dictionary<string, object>
        {
            ["numeroOS"] = numeroOS,
            ["statusAnterior"] = statusAnterior,
            ["statusNovo"] = statusNovo,
            ["minutosNoStatus"] = Math.Round(minutosNoStatusAnterior, 2)
        });

    /// <inheritdoc />
    public void ReservaRecusada(Guid ordemDeServicoId, string motivo) =>
        Registrar("ReservaRecusada", new Dictionary<string, object>
        {
            ["ordemDeServicoId"] = ordemDeServicoId.ToString(),
            ["motivo"] = motivo
        });

    private void Registrar(string evento, Dictionary<string, object> atributos)
    {
        try
        {
            NewRelic.Api.Agent.NewRelic.RecordCustomEvent(evento, atributos);
        }
        catch (Exception ex)
        {
            // Sem agente configurado (ambiente local, por exemplo) isto apenas
            // não faz nada. Nunca propaga.
            _logger.LogDebug(ex, "Nao foi possivel registrar o evento {Evento}", evento);
        }
    }
}
