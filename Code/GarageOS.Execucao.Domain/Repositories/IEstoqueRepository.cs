using GarageOS.Execucao.Domain.Entities;

namespace GarageOS.Execucao.Domain.Repositories;

public interface IEstoqueRepository
{
    Task<IEnumerable<Estoque>> ListarTodosAsync();
    Task<Estoque?> ObterPorIdAsync(Guid id);
    Task<IReadOnlyList<Estoque>> ObterPorIdsAsync(IEnumerable<Guid> ids);
    Task AdicionarAsync(Estoque estoque);
    Task AtualizarAsync(Estoque estoque);
    Task RemoverAsync(Estoque estoque);
}
