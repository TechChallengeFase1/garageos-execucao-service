using GarageOS.Execucao.Domain.Enums;
using GarageOS.Execucao.Domain.Utils;

namespace GarageOS.Execucao.Domain.Entities;

/// <summary>Um serviço a executar dentro da ordem de execução</summary>
/// <remarks>
/// Herdeiro do OrdemDeServicoServico do monólito. O nome do serviço vem como
/// cópia (snapshot) no pedido de execução: o catálogo de serviços pertence ao
/// OS Service, e este serviço nunca lê o banco dele.
/// </remarks>
public class ItemExecucao
{
    public Guid Id { get; private set; }
    public Guid OrdemDeExecucaoId { get; private set; }
    public Guid ServicoId { get; private set; }
    public string Nome { get; private set; } = null!;
    public StatusItemExecucao Status { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime? IniciadaEm { get; private set; }
    public DateTime? FinalizadaEm { get; private set; }

    protected ItemExecucao() { }

    public ItemExecucao(Guid ordemDeExecucaoId, Guid servicoId, string nome)
    {
        if (servicoId == Guid.Empty)
            throw new ArgumentException("Serviço é obrigatório.", nameof(servicoId));

        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome do serviço é obrigatório.", nameof(nome));

        Id = Guid.NewGuid();
        OrdemDeExecucaoId = ordemDeExecucaoId;
        ServicoId = servicoId;
        Nome = nome;
        Status = StatusItemExecucao.Criada;
        CriadoEm = BrasiliaTime.Agora;
    }

    public void IniciarExecucao()
    {
        if (IniciadaEm.HasValue)
            throw new ArgumentException("Execução já foi iniciada.");

        Status = StatusItemExecucao.Iniciado;
        IniciadaEm = BrasiliaTime.Agora;
    }

    public void FinalizarExecucao()
    {
        if (!IniciadaEm.HasValue)
            throw new ArgumentException("Execução não foi iniciada.");

        if (FinalizadaEm.HasValue)
            throw new ArgumentException("Execução já foi finalizada.");

        Status = StatusItemExecucao.Finalizado;
        FinalizadaEm = BrasiliaTime.Agora;
    }
}
