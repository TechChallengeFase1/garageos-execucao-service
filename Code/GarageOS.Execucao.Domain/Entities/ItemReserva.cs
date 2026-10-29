namespace GarageOS.Execucao.Domain.Entities;

public class ItemReserva
{
    public Guid Id { get; private set; }
    public Guid ReservaPecasId { get; private set; }
    public Guid EstoqueId { get; private set; }
    public int Quantidade { get; private set; }

    protected ItemReserva() { }

    public ItemReserva(Guid reservaPecasId, Guid estoqueId, int quantidade)
    {
        if (estoqueId == Guid.Empty)
            throw new ArgumentException("Item de estoque é obrigatório.", nameof(estoqueId));

        if (quantidade <= 0)
            throw new ArgumentException("Quantidade deve ser maior que zero.", nameof(quantidade));

        Id = Guid.NewGuid();
        ReservaPecasId = reservaPecasId;
        EstoqueId = estoqueId;
        Quantidade = quantidade;
    }
}
