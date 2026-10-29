using GarageOS.Execucao.Domain.Entities;
using GarageOS.Execucao.Domain.Enums;
using GarageOS.Execucao.Domain.ReadModels;
using GarageOS.Execucao.Domain.Repositories;
using GarageOS.Execucao.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Execucao.Infrastructure.Repositories;

public class OrdemDeExecucaoRepository : IOrdemDeExecucaoRepository
{
    private readonly ExecucaoDbContext _context;

    public OrdemDeExecucaoRepository(ExecucaoDbContext context)
    {
        _context = context;
    }

    public async Task<OrdemDeExecucao?> ObterPorIdAsync(Guid id)
    {
        return await _context.OrdensDeExecucao
            .Include(o => o.Itens)
            .Include(o => o.Historico)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<OrdemDeExecucao?> ObterPorOrdemDeServicoIdAsync(Guid ordemDeServicoId)
    {
        return await _context.OrdensDeExecucao
            .Include(o => o.Itens)
            .Include(o => o.Historico)
            .FirstOrDefaultAsync(o => o.OrdemDeServicoId == ordemDeServicoId);
    }

    /// <remarks>
    /// Sem filtro, mostra só o que ainda está na oficina, na ordem de atendimento:
    /// primeiro o que já está em reparo, depois em diagnóstico, depois a fila,
    /// e dentro de cada grupo a mais antiga primeiro. Mesma prioridade que o
    /// monólito usava na listagem de OS.
    /// </remarks>
    public async Task<IReadOnlyList<OrdemDeExecucao>> ListarFilaAsync(StatusExecucao? status)
    {
        var consulta = _context.OrdensDeExecucao
            .AsNoTracking()
            .Include(o => o.Itens)
            .Include(o => o.Historico)
            .AsQueryable();

        consulta = status.HasValue
            ? consulta.Where(o => o.Status == status.Value)
            : consulta.Where(o => o.Status != StatusExecucao.Concluida && o.Status != StatusExecucao.Falhou);

        return await consulta
            .OrderBy(o => o.Status == StatusExecucao.EmReparo ? 0
                    : o.Status == StatusExecucao.EmDiagnostico ? 1
                    : o.Status == StatusExecucao.NaFila ? 2
                    : 3)
            .ThenBy(o => o.CriadaEm)
            .AsSplitQuery()
            .ToListAsync();
    }

    public async Task AdicionarAsync(OrdemDeExecucao ordem)
    {
        await _context.OrdensDeExecucao.AddAsync(ordem);
        await _context.SalvarAsync("Criação da ordem de execução");
    }

    /// <remarks>
    /// A entidade veio do ObterPorIdAsync, com tracking. Itens de histórico
    /// novos são detectados pela coleção e entram como inseridos. Chamar
    /// Update() aqui marcaria esses itens como alterados e o UPDATE falharia.
    /// </remarks>
    public async Task AtualizarAsync(OrdemDeExecucao ordem)
    {
        await _context.SalvarAsync("Atualização da ordem de execução");
    }

    public async Task<IReadOnlyList<AgingServico>> CalcularAgingAsync()
    {
        // Agregado no PostgreSQL. EXTRACT(EPOCH ...) devolve a duração em
        // segundos; dividido por 60, em minutos.
        var linhas = await _context.Database
            .SqlQuery<LinhaAging>($"""
                SELECT "ServicoId",
                       "Nome",
                       COUNT(*)::int AS "TotalExecucoes",
                       AVG(EXTRACT(EPOCH FROM ("FinalizadaEm" - "IniciadaEm")) / 60.0)::float8 AS "TempoMedioMinutos"
                FROM "ItensExecucao"
                WHERE "IniciadaEm" IS NOT NULL AND "FinalizadaEm" IS NOT NULL
                GROUP BY "ServicoId", "Nome"
                ORDER BY "Nome"
                """)
            .ToListAsync();

        return linhas
            .Select(l => new AgingServico(l.ServicoId, l.Nome, l.TotalExecucoes, l.TempoMedioMinutos))
            .ToList();
    }

    private sealed class LinhaAging
    {
        public Guid ServicoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int TotalExecucoes { get; set; }
        public double TempoMedioMinutos { get; set; }
    }
}
