using GarageOS.Execucao.Domain.Enums;

namespace GarageOS.Execucao.Domain.Exceptions;

public class ReservaStatusInvalidoException : Exception
{
    public ReservaStatusInvalidoException(Guid ordemDeServicoId, StatusReserva atual, StatusReserva pretendido)
        : base($"A reserva da OS '{ordemDeServicoId}' está {atual} e não pode passar para {pretendido}.") { }
}
