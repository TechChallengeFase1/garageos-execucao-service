namespace GarageOS.Execucao.Domain.Exceptions;

/// <summary>Outra requisição alterou o mesmo registro ao mesmo tempo</summary>
/// <remarks>
/// Lançada pelos repositórios quando o token de concorrência (xmin) não bate.
/// A operação não foi gravada; quem chamou pode tentar de novo.
/// </remarks>
public class ConflitoDeConcorrenciaException : Exception
{
    public ConflitoDeConcorrenciaException(string mensagem, Exception? interna = null)
        : base(mensagem, interna) { }
}
