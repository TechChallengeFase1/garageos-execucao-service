namespace GarageOS.Execucao.Domain.Exceptions;

public class ItemExecucaoNaoEncontradoException : Exception
{
    public ItemExecucaoNaoEncontradoException(Guid ordemDeExecucaoId, Guid itemId)
        : base($"Serviço '{itemId}' não encontrado na ordem de execução '{ordemDeExecucaoId}'.") { }
}
