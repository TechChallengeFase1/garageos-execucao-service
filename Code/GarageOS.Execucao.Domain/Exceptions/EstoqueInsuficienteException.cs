namespace GarageOS.Execucao.Domain.Exceptions;

/// <summary>Não há quantidade disponível para reservar</summary>
public class EstoqueInsuficienteException : Exception
{
    public Guid EstoqueId { get; }

    public EstoqueInsuficienteException(Guid estoqueId, string nome, int disponivel, int solicitado)
        : base($"Estoque insuficiente para '{nome}'. Disponível: {disponivel}, Solicitado: {solicitado}.")
    {
        EstoqueId = estoqueId;
    }
}
