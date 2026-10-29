using FluentValidation;
using GarageOS.Execucao.Application.DTOs.Execucoes;
using GarageOS.Execucao.Domain.Enums;

namespace GarageOS.Execucao.Application.Validators.Execucoes;

public class AlterarStatusExecucaoValidator : AbstractValidator<AlterarStatusExecucaoRequest>
{
    public AlterarStatusExecucaoValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status inválido.")
            .NotEqual(StatusExecucao.NaFila).WithMessage("Uma execução não volta para a fila.");

        RuleFor(x => x.Motivo)
            .NotEmpty().WithMessage("Informe o motivo da falha.")
            .When(x => x.Status == StatusExecucao.Falhou);

        RuleFor(x => x.Motivo)
            .MaximumLength(500).WithMessage("Motivo deve ter no máximo 500 caracteres.");
    }
}
