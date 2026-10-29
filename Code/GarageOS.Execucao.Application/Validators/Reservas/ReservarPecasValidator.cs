using FluentValidation;
using GarageOS.Execucao.Application.DTOs.Reservas;

namespace GarageOS.Execucao.Application.Validators.Reservas;

public class ReservarPecasValidator : AbstractValidator<ReservarPecasRequest>
{
    public ReservarPecasValidator()
    {
        RuleFor(x => x.OrdemDeServicoId)
            .NotEmpty().WithMessage("Ordem de serviço é obrigatória.");

        RuleFor(x => x.Itens)
            .NotEmpty().WithMessage("Informe ao menos uma peça.");

        RuleForEach(x => x.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.EstoqueId)
                .NotEmpty().WithMessage("Item de estoque é obrigatório.");

            item.RuleFor(i => i.Quantidade)
                .GreaterThan(0).WithMessage("Quantidade deve ser maior que zero.");
        });
    }
}
