namespace GarageOS.Execucao.Domain.ReadModels;

/// <summary>Linha do relatório de aging: tempo médio de execução de um serviço</summary>
public record AgingServico(Guid ServicoId, string Nome, int TotalExecucoes, double TempoMedioMinutos);
