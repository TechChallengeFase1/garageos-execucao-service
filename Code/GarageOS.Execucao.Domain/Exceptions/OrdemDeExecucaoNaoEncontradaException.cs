namespace GarageOS.Execucao.Domain.Exceptions;

public class OrdemDeExecucaoNaoEncontradaException : Exception
{
    public OrdemDeExecucaoNaoEncontradaException(Guid id)
        : base($"Ordem de execução '{id}' não encontrada.") { }
}
