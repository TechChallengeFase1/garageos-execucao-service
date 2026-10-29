using GarageOS.Execucao.Domain.Entities;

namespace GarageOS.Execucao.Domain.Repositories;

/// <summary>Persistência das reservas de peças</summary>
/// <remarks>
/// Adicionar e Atualizar gravam a reserva JUNTO com os itens de estoque
/// alterados no mesmo escopo, numa única transação: ou todas as peças são
/// reservadas, ou nenhuma.
/// </remarks>
public interface IReservaPecasRepository
{
    Task<ReservaPecas?> ObterPorOrdemDeServicoIdAsync(Guid ordemDeServicoId);
    Task AdicionarAsync(ReservaPecas reserva);
    Task AtualizarAsync(ReservaPecas reserva);
}
