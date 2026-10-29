using GarageOS.Execucao.Domain.Enums;
using GarageOS.Execucao.Domain.Exceptions;
using GarageOS.Execucao.Domain.Utils;

namespace GarageOS.Execucao.Domain.Entities;

/// <summary>Peças separadas para uma ordem de serviço</summary>
/// <remarks>
/// Uma reserva por OS (OrdemDeServicoId é único). É isso que torna a operação
/// idempotente: pedir de novo a reserva da mesma OS devolve a que já existe, em
/// vez de reservar as peças duas vezes. Com mensageria entregando "pelo menos
/// uma vez", mensagem repetida é certeza, não hipótese.
/// </remarks>
public class ReservaPecas
{
    private readonly List<ItemReserva> _itens = [];

    public Guid Id { get; private set; }
    public Guid OrdemDeServicoId { get; private set; }
    public StatusReserva Status { get; private set; }
    public DateTime CriadaEm { get; private set; }
    public DateTime AtualizadaEm { get; private set; }
    public IReadOnlyCollection<ItemReserva> Itens => _itens.AsReadOnly();

    protected ReservaPecas() { }

    public ReservaPecas(Guid ordemDeServicoId, IEnumerable<(Guid EstoqueId, int Quantidade)> itens)
    {
        if (ordemDeServicoId == Guid.Empty)
            throw new ArgumentException("Ordem de serviço é obrigatória.", nameof(ordemDeServicoId));

        var lista = itens.ToList();
        if (lista.Count == 0)
            throw new ArgumentException("A reserva precisa de ao menos um item.", nameof(itens));

        Id = Guid.NewGuid();
        OrdemDeServicoId = ordemDeServicoId;
        Status = StatusReserva.Ativa;
        CriadaEm = BrasiliaTime.Agora;
        AtualizadaEm = CriadaEm;

        // A mesma peça pedida em duas linhas vira uma linha só, com a soma.
        foreach (var grupo in lista.GroupBy(i => i.EstoqueId))
            _itens.Add(new ItemReserva(Id, grupo.Key, grupo.Sum(i => i.Quantidade)));
    }

    /// <summary>Marca a reserva como liberada. Devolve false se já estava liberada.</summary>
    public bool Liberar()
    {
        if (Status == StatusReserva.Liberada)
            return false;

        if (Status == StatusReserva.Consumida)
            throw new ReservaStatusInvalidoException(OrdemDeServicoId, Status, StatusReserva.Liberada);

        Status = StatusReserva.Liberada;
        AtualizadaEm = BrasiliaTime.Agora;
        return true;
    }

    /// <summary>Marca a reserva como consumida. Devolve false se já estava consumida.</summary>
    public bool Consumir()
    {
        if (Status == StatusReserva.Consumida)
            return false;

        if (Status == StatusReserva.Liberada)
            throw new ReservaStatusInvalidoException(OrdemDeServicoId, Status, StatusReserva.Consumida);

        Status = StatusReserva.Consumida;
        AtualizadaEm = BrasiliaTime.Agora;
        return true;
    }
}
