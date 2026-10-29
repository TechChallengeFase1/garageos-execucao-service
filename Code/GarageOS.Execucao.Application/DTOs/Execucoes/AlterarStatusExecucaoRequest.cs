using GarageOS.Execucao.Domain.Enums;

namespace GarageOS.Execucao.Application.DTOs.Execucoes;

public class AlterarStatusExecucaoRequest
{
    public StatusExecucao Status { get; set; }
    public string? Motivo { get; set; }
}
