namespace GarageOS.Execucao.Application.DTOs.Execucoes;

public class OrdemDeExecucaoResponse
{
    public Guid Id { get; set; }
    public Guid OrdemDeServicoId { get; set; }
    public string NumeroOS { get; set; } = string.Empty;
    public Guid? FilialId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? MotivoFalha { get; set; }
    public DateTime CriadaEm { get; set; }
    public DateTime AtualizadaEm { get; set; }
    public List<ItemExecucaoResponse> Itens { get; set; } = [];
    public List<HistoricoExecucaoResponse> Historico { get; set; } = [];
}

public class ItemExecucaoResponse
{
    public Guid Id { get; set; }
    public Guid ServicoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? IniciadaEm { get; set; }
    public DateTime? FinalizadaEm { get; set; }
}

public class HistoricoExecucaoResponse
{
    public string? StatusAnterior { get; set; }
    public string StatusNovo { get; set; } = string.Empty;
    public string? Motivo { get; set; }
    public DateTime OcorridoEm { get; set; }
}
