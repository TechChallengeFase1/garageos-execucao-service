using GarageOS.Execucao.Domain.Entities;
using GarageOS.Execucao.Domain.Repositories;
using GarageOS.Execucao.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Execucao.Infrastructure.Repositories;

public class ReservaPecasRepository : IReservaPecasRepository
{
    private readonly ExecucaoDbContext _context;

    public ReservaPecasRepository(ExecucaoDbContext context)
    {
        _context = context;
    }

    public async Task<ReservaPecas?> ObterPorOrdemDeServicoIdAsync(Guid ordemDeServicoId)
    {
        return await _context.ReservasPecas
            .Include(r => r.Itens)
            .FirstOrDefaultAsync(r => r.OrdemDeServicoId == ordemDeServicoId);
    }

    /// <remarks>
    /// Um SaveChanges só: grava a reserva E os saldos de estoque alterados pelo
    /// use case, na mesma transação. Ou tudo, ou nada.
    /// </remarks>
    public async Task AdicionarAsync(ReservaPecas reserva)
    {
        await _context.ReservasPecas.AddAsync(reserva);
        await _context.SalvarAsync("Reserva de peças");
    }

    public async Task AtualizarAsync(ReservaPecas reserva)
    {
        await _context.SalvarAsync("Atualização da reserva de peças");
    }
}
