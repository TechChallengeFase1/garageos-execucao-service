namespace GarageOS.Execucao.Application.DTOs.Execucoes;

public class CriarOrdemDeExecucaoRequest
{
    public Guid OrdemDeServicoId { get; set; }
    public string NumeroOS { get; set; } = string.Empty;
    public Guid? FilialId { get; set; }
    public List<ItemExecucaoRequest> Itens { get; set; } = [];
}

public class ItemExecucaoRequest
{
    public Guid ServicoId { get; set; }
    public string Nome { get; set; } = string.Empty;
}
