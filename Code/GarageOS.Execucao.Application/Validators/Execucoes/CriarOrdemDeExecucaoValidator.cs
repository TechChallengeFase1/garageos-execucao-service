using FluentValidation;
using GarageOS.Execucao.Application.DTOs.Execucoes;

namespace GarageOS.Execucao.Application.Validators.Execucoes;

public class CriarOrdemDeExecucaoValidator : AbstractValidator<CriarOrdemDeExecucaoRequest>
{
    public CriarOrdemDeExecucaoValidator()
    {
        RuleFor(x => x.OrdemDeServicoId)
            .NotEmpty().WithMessage("Ordem de serviço é obrigatória.");

        RuleFor(x => x.NumeroOS)
            .NotEmpty().WithMessage("Número da OS é obrigatório.")
            .MaximumLength(20).WithMessage("Número da OS deve ter no máximo 20 caracteres.");

        RuleFor(x => x.Itens)
            .NotEmpty().WithMessage("Informe ao menos um serviço.");

        RuleForEach(x => x.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.ServicoId)
                .NotEmpty().WithMessage("Serviço é obrigatório.");

            item.RuleFor(i => i.Nome)
                .NotEmpty().WithMessage("Nome do serviço é obrigatório.")
                .MaximumLength(150).WithMessage("Nome do serviço deve ter no máximo 150 caracteres.");
        });
    }
}
