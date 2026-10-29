namespace GarageOS.Execucao.Application.DTOs.Reservas;

public class ReservarPecasRequest
{
    public Guid OrdemDeServicoId { get; set; }
    public List<ItemReservaRequest> Itens { get; set; } = [];
}

public class ItemReservaRequest
{
    public Guid EstoqueId { get; set; }
    public int Quantidade { get; set; }
}
