using GarageOS.Execucao.Domain.Enums;
using GarageOS.Execucao.Domain.Utils;

namespace GarageOS.Execucao.Domain.Entities;

/// <summary>Registro de uma transição de status da ordem de execução</summary>
public class HistoricoExecucao
{
    public Guid Id { get; private set; }
    public Guid OrdemDeExecucaoId { get; private set; }
    public StatusExecucao? StatusAnterior { get; private set; }
    public StatusExecucao StatusNovo { get; private set; }
    public string? Motivo { get; private set; }
    public DateTime OcorridoEm { get; private set; }

    protected HistoricoExecucao() { }

    public HistoricoExecucao(Guid ordemDeExecucaoId, StatusExecucao? statusAnterior, StatusExecucao statusNovo, string? motivo)
    {
        Id = Guid.NewGuid();
        OrdemDeExecucaoId = ordemDeExecucaoId;
        StatusAnterior = statusAnterior;
        StatusNovo = statusNovo;
        Motivo = motivo;
        OcorridoEm = BrasiliaTime.Agora;
    }
}
