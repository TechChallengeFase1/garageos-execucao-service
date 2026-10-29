namespace GarageOS.Execucao.Domain.Exceptions;

public class TransicaoDeStatusInvalidaException : Exception
{
    public TransicaoDeStatusInvalidaException(string de, string para)
        : base($"Transição de status inválida: {de} -> {para}.") { }

    public TransicaoDeStatusInvalidaException(string mensagem)
        : base(mensagem) { }
}
