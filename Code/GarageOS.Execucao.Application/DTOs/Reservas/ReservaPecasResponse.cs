namespace GarageOS.Execucao.Application.DTOs.Reservas;

public class ReservaPecasResponse
{
    public Guid Id { get; set; }
    public Guid OrdemDeServicoId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CriadaEm { get; set; }
    public DateTime AtualizadaEm { get; set; }
    public List<ItemReservaResponse> Itens { get; set; } = [];
}

public class ItemReservaResponse
{
    public Guid EstoqueId { get; set; }
    public int Quantidade { get; set; }
}
