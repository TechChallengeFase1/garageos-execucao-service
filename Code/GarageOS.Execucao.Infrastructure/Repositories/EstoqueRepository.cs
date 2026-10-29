using GarageOS.Execucao.Domain.Entities;
using GarageOS.Execucao.Domain.Repositories;
using GarageOS.Execucao.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Execucao.Infrastructure.Repositories;

public class EstoqueRepository : IEstoqueRepository
{
    private readonly ExecucaoDbContext _context;

    public EstoqueRepository(ExecucaoDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Estoque>> ListarTodosAsync()
    {
        return await _context.Estoques
            .AsNoTracking()
            .OrderBy(e => e.Nome)
            .ToListAsync();
    }

    public async Task<Estoque?> ObterPorIdAsync(Guid id)
    {
        return await _context.Estoques.FindAsync(id);
    }

    /// <remarks>
    /// Com tracking de propósito: quem chama altera os saldos e grava tudo junto
    /// com a reserva, no mesmo SaveChanges.
    /// </remarks>
    public async Task<IReadOnlyList<Estoque>> ObterPorIdsAsync(IEnumerable<Guid> ids)
    {
        var lista = ids.Distinct().ToList();

        return await _context.Estoques
            .Where(e => lista.Contains(e.Id))
            .ToListAsync();
    }

    public async Task AdicionarAsync(Estoque estoque)
    {
        await _context.Estoques.AddAsync(estoque);
        await _context.SalvarAsync("Cadastro de estoque");
    }

    public async Task AtualizarAsync(Estoque estoque)
    {
        await _context.SalvarAsync("Atualização de estoque");
    }

    public async Task RemoverAsync(Estoque estoque)
    {
        _context.Estoques.Remove(estoque);
        await _context.SalvarAsync("Remoção de estoque");
    }
}
