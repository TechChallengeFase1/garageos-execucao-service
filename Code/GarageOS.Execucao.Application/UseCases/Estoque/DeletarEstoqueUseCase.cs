using GarageOS.Execucao.Domain.Exceptions;
using GarageOS.Execucao.Domain.Repositories;

namespace GarageOS.Execucao.Application.UseCases.Estoques;

public class DeletarEstoqueUseCase
{
    private readonly IEstoqueRepository _repository;

    public DeletarEstoqueUseCase(IEstoqueRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecutarAsync(Guid id)
    {
        var estoque = await _repository.ObterPorIdAsync(id)
            ?? throw new EstoqueNaoEncontradoException(id);

        // Uma peça reservada pertence a uma OS em andamento. Apagá-la deixaria a
        // reserva apontando para nada e a compensação da saga sem o que devolver.
        if (estoque.QuantidadeReservada > 0)
            throw new InvalidOperationException(
                $"'{estoque.Nome}' tem {estoque.QuantidadeReservada} unidade(s) reservada(s) e não pode ser removido.");

        await _repository.RemoverAsync(estoque);
    }
}
