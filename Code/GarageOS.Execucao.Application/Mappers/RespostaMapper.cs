using GarageOS.Execucao.Application.DTOs.Estoques;
using GarageOS.Execucao.Application.DTOs.Execucoes;
using GarageOS.Execucao.Application.DTOs.Reservas;
using GarageOS.Execucao.Domain.Entities;

namespace GarageOS.Execucao.Application.Mappers;

/// <summary>Converte entidades em respostas da API</summary>
/// <remarks>
/// Num lugar só. No monólito o mesmo mapeamento estava copiado em cada use case,
/// e um campo novo precisava ser lembrado em sete arquivos.
/// </remarks>
public static class RespostaMapper
{
    public static EstoqueResponse ParaResponse(this Estoque estoque) => new()
    {
        Id = estoque.Id,
        Nome = estoque.Nome,
        Quantidade = estoque.Quantidade,
        QuantidadeReservada = estoque.QuantidadeReservada,
        QuantidadeDisponivel = estoque.QuantidadeDisponivel,
        Valor = estoque.Valor,
        DataEntrada = estoque.DataEntrada,
        DataSaida = estoque.DataSaida,
        Fornecedor = estoque.Fornecedor,
        Status = estoque.Status.ToString()
    };

    public static ReservaPecasResponse ParaResponse(this ReservaPecas reserva) => new()
    {
        Id = reserva.Id,
        OrdemDeServicoId = reserva.OrdemDeServicoId,
        Status = reserva.Status.ToString(),
        CriadaEm = reserva.CriadaEm,
        AtualizadaEm = reserva.AtualizadaEm,
        Itens = reserva.Itens
            .Select(i => new ItemReservaResponse { EstoqueId = i.EstoqueId, Quantidade = i.Quantidade })
            .ToList()
    };

    public static OrdemDeExecucaoResponse ParaResponse(this OrdemDeExecucao ordem) => new()
    {
        Id = ordem.Id,
        OrdemDeServicoId = ordem.OrdemDeServicoId,
        NumeroOS = ordem.NumeroOS,
        FilialId = ordem.FilialId,
        Status = ordem.Status.ToString(),
        MotivoFalha = ordem.MotivoFalha,
        CriadaEm = ordem.CriadaEm,
        AtualizadaEm = ordem.AtualizadaEm,
        Itens = ordem.Itens.Select(i => new ItemExecucaoResponse
        {
            Id = i.Id,
            ServicoId = i.ServicoId,
            Nome = i.Nome,
            Status = i.Status.ToString(),
            IniciadaEm = i.IniciadaEm,
            FinalizadaEm = i.FinalizadaEm
        }).ToList(),
        Historico = ordem.Historico
            .OrderBy(h => h.OcorridoEm)
            .Select(h => new HistoricoExecucaoResponse
            {
                StatusAnterior = h.StatusAnterior?.ToString(),
                StatusNovo = h.StatusNovo.ToString(),
                Motivo = h.Motivo,
                OcorridoEm = h.OcorridoEm
            }).ToList()
    };
}
