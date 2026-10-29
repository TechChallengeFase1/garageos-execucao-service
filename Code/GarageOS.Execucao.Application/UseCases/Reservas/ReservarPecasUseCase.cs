using GarageOS.Execucao.Application.Abstractions;
using GarageOS.Execucao.Application.DTOs.Reservas;
using GarageOS.Execucao.Application.Mappers;
using GarageOS.Execucao.Domain.Entities;
using GarageOS.Execucao.Domain.Exceptions;
using GarageOS.Execucao.Domain.Repositories;

namespace GarageOS.Execucao.Application.UseCases.Reservas;

/// <summary>Reserva as peças de uma OS: todas ou nenhuma</summary>
/// <remarks>
/// Passo da saga (Fase 4, etapa 5) e também exposto por REST. Duas garantias:
///
///   - Tudo ou nada: as peças são reservadas em memória e gravadas num único
///     SaveChanges. Se uma faltar, nenhuma é gravada.
///   - Idempotência: a reserva é única por OS. Pedir de novo devolve a que já
///     existe, sem reservar duas vezes.
/// </remarks>
public class ReservarPecasUseCase
{
    private readonly IReservaPecasRepository _reservaRepository;
    private readonly IEstoqueRepository _estoqueRepository;
    private readonly IMetricasDeNegocio _metricas;

    public ReservarPecasUseCase(
        IReservaPecasRepository reservaRepository,
        IEstoqueRepository estoqueRepository,
        IMetricasDeNegocio metricas)
    {
        _reservaRepository = reservaRepository;
        _estoqueRepository = estoqueRepository;
        _metricas = metricas;
    }

    /// <returns>A reserva e se ela foi criada agora (false = já existia)</returns>
    public async Task<(ReservaPecasResponse Reserva, bool Criada)> ExecutarAsync(ReservarPecasRequest request)
    {
        var existente = await _reservaRepository.ObterPorOrdemDeServicoIdAsync(request.OrdemDeServicoId);
        if (existente is not null)
            return (existente.ParaResponse(), false);

        var reserva = new ReservaPecas(
            request.OrdemDeServicoId,
            request.Itens.Select(i => (i.EstoqueId, i.Quantidade)));

        var estoques = await _estoqueRepository.ObterPorIdsAsync(reserva.Itens.Select(i => i.EstoqueId));

        try
        {
            foreach (var item in reserva.Itens)
            {
                var estoque = estoques.FirstOrDefault(e => e.Id == item.EstoqueId)
                    ?? throw new EstoqueNaoEncontradoException(item.EstoqueId);

                estoque.Reservar(item.Quantidade);
            }
        }
        catch (Exception ex) when (ex is EstoqueInsuficienteException or EstoqueNaoEncontradoException)
        {
            _metricas.ReservaRecusada(request.OrdemDeServicoId, ex.Message);
            throw;
        }

        await _reservaRepository.AdicionarAsync(reserva);

        return (reserva.ParaResponse(), true);
    }
}
