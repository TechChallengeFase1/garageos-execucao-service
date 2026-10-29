using GarageOS.Execucao.Application.DTOs.Execucoes;
using GarageOS.Execucao.Domain.Repositories;

namespace GarageOS.Execucao.Application.UseCases.Execucoes;

/// <summary>Tempo médio de execução de cada serviço</summary>
/// <remarks>
/// No monólito a média era calculada em memória, depois de ler TODOS os itens
/// finalizados da história. Aqui a agregação é feita pelo banco (GROUP BY com
/// AVG), e só as linhas do relatório trafegam.
/// </remarks>
public class CalcularAgingServicosUseCase
{
    private readonly IOrdemDeExecucaoRepository _repository;

    public CalcularAgingServicosUseCase(IOrdemDeExecucaoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<AgingServicoResponse>> ExecutarAsync()
    {
        var linhas = await _repository.CalcularAgingAsync();

        return linhas
            .Select(l => new AgingServicoResponse
            {
                ServicoId = l.ServicoId,
                ServicoNome = l.Nome,
                TotalExecucoes = l.TotalExecucoes,
                TempoMedioMinutos = Math.Round(l.TempoMedioMinutos, 2),
                TempoMedioFormatado = FormatarTempo(l.TempoMedioMinutos)
            })
            .OrderBy(r => r.ServicoNome)
            .ToList();
    }

    internal static string FormatarTempo(double minutos)
    {
        if (minutos < 1)
            return $"{(int)(minutos * 60)}s";

        if (minutos < 60)
            return $"{(int)minutos}min";

        var horas = (int)(minutos / 60);
        var min = (int)(minutos % 60);
        return min > 0 ? $"{horas}h {min}min" : $"{horas}h";
    }
}
