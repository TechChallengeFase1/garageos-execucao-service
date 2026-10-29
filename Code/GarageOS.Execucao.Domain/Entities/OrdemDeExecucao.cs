using GarageOS.Execucao.Domain.Enums;
using GarageOS.Execucao.Domain.Exceptions;
using GarageOS.Execucao.Domain.Utils;

namespace GarageOS.Execucao.Domain.Entities;

/// <summary>Uma OS na fila da oficina, do diagnóstico ao reparo concluído</summary>
/// <remarks>
/// O ciclo de vida é uma máquina de estados explícita: só as transições da
/// tabela abaixo são aceitas. Concluída e Falhou são estados finais.
///
///   NaFila        -> EmDiagnostico | Falhou
///   EmDiagnostico -> EmReparo      | Falhou
///   EmReparo      -> Concluida     | Falhou
/// </remarks>
public class OrdemDeExecucao
{
    private static readonly Dictionary<StatusExecucao, StatusExecucao[]> TransicoesPermitidas = new()
    {
        [StatusExecucao.NaFila] = [StatusExecucao.EmDiagnostico, StatusExecucao.Falhou],
        [StatusExecucao.EmDiagnostico] = [StatusExecucao.EmReparo, StatusExecucao.Falhou],
        [StatusExecucao.EmReparo] = [StatusExecucao.Concluida, StatusExecucao.Falhou],
        [StatusExecucao.Concluida] = [],
        [StatusExecucao.Falhou] = []
    };

    private readonly List<ItemExecucao> _itens = [];
    private readonly List<HistoricoExecucao> _historico = [];

    public Guid Id { get; private set; }
    public Guid OrdemDeServicoId { get; private set; }
    public string NumeroOS { get; private set; } = null!;
    public Guid? FilialId { get; private set; }
    public StatusExecucao Status { get; private set; }
    public string? MotivoFalha { get; private set; }
    public DateTime CriadaEm { get; private set; }
    public DateTime AtualizadaEm { get; private set; }
    public IReadOnlyCollection<ItemExecucao> Itens => _itens.AsReadOnly();
    public IReadOnlyCollection<HistoricoExecucao> Historico => _historico.AsReadOnly();

    protected OrdemDeExecucao() { }

    public OrdemDeExecucao(Guid ordemDeServicoId, string numeroOS, Guid? filialId, IEnumerable<(Guid ServicoId, string Nome)> itens)
    {
        if (ordemDeServicoId == Guid.Empty)
            throw new ArgumentException("Ordem de serviço é obrigatória.", nameof(ordemDeServicoId));

        if (string.IsNullOrWhiteSpace(numeroOS))
            throw new ArgumentException("Número da OS é obrigatório.", nameof(numeroOS));

        var lista = itens.ToList();
        if (lista.Count == 0)
            throw new ArgumentException("A execução precisa de ao menos um serviço.", nameof(itens));

        Id = Guid.NewGuid();
        OrdemDeServicoId = ordemDeServicoId;
        NumeroOS = numeroOS;
        FilialId = filialId;
        Status = StatusExecucao.NaFila;
        CriadaEm = BrasiliaTime.Agora;
        AtualizadaEm = CriadaEm;

        foreach (var (servicoId, nome) in lista)
            _itens.Add(new ItemExecucao(Id, servicoId, nome));

        _historico.Add(new HistoricoExecucao(Id, null, Status, null));
    }

    public static bool TransicaoPermitida(StatusExecucao de, StatusExecucao para) =>
        TransicoesPermitidas[de].Contains(para);

    /// <summary>Move a execução para o próximo status, respeitando a tabela de transições</summary>
    public void AlterarStatus(StatusExecucao novoStatus, string? motivo = null)
    {
        if (!TransicaoPermitida(Status, novoStatus))
            throw new TransicaoDeStatusInvalidaException(Status.ToString(), novoStatus.ToString());

        if (novoStatus == StatusExecucao.Falhou && string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("Informe o motivo da falha.", nameof(motivo));

        if (novoStatus == StatusExecucao.Concluida && _itens.Any(i => i.Status != StatusItemExecucao.Finalizado))
            throw new TransicaoDeStatusInvalidaException(
                "A execução só pode ser concluída depois que todos os serviços forem finalizados.");

        var anterior = Status;
        Status = novoStatus;
        if (novoStatus == StatusExecucao.Falhou)
            MotivoFalha = motivo;
        AtualizadaEm = BrasiliaTime.Agora;
        _historico.Add(new HistoricoExecucao(Id, anterior, novoStatus, motivo));
    }

    /// <summary>Inicia ou finaliza um dos serviços da execução</summary>
    public ItemExecucao AlterarStatusItem(Guid itemId, StatusItemExecucao novoStatus)
    {
        if (Status is StatusExecucao.Concluida or StatusExecucao.Falhou)
            throw new TransicaoDeStatusInvalidaException(
                $"A execução está {Status}; os serviços dela não podem mais ser alterados.");

        var item = _itens.FirstOrDefault(i => i.Id == itemId)
            ?? throw new ItemExecucaoNaoEncontradoException(Id, itemId);

        switch (novoStatus)
        {
            case StatusItemExecucao.Iniciado:
                item.IniciarExecucao();
                break;
            case StatusItemExecucao.Finalizado:
                item.FinalizarExecucao();
                break;
            default:
                throw new ArgumentException("Status permitido: Iniciado ou Finalizado.", nameof(novoStatus));
        }

        AtualizadaEm = BrasiliaTime.Agora;
        return item;
    }
}
