namespace GarageOS.Execucao.Domain.Exceptions;

public class ReservaNaoEncontradaException : Exception
{
    public ReservaNaoEncontradaException(Guid ordemDeServicoId)
        : base($"Reserva de peças da OS '{ordemDeServicoId}' não encontrada.") { }
}
