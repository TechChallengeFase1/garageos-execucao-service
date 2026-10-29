namespace GarageOS.Execucao.Application.Abstractions;

/// <summary>
/// Publica eventos de negócio para a ferramenta de observabilidade.
/// </summary>
/// <remarks>
/// Existe como interface na camada de Application para que os use cases não
/// conheçam o fornecedor. A implementação com New Relic vive na Infrastructure;
/// trocar de ferramenta não toca em regra de negócio.
/// </remarks>
public interface IMetricasDeNegocio
{
    /// <summary>Registra a transição de status da execução e quanto tempo durou o anterior</summary>
    void ExecucaoStatusAlterado(string numeroOS, string statusAnterior, string statusNovo, double minutosNoStatusAnterior);

    /// <summary>Registra uma reserva de peças recusada por falta de estoque</summary>
    void ReservaRecusada(Guid ordemDeServicoId, string motivo);
}
