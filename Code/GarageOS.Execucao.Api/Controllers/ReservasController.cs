using GarageOS.Execucao.Api.Seguranca;
using GarageOS.Execucao.Application.DTOs.Reservas;
using GarageOS.Execucao.Application.UseCases.Reservas;
using GarageOS.Execucao.Application.Validators.Reservas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GarageOS.Execucao.Api.Controllers;

/// <summary>Reserva de peças para as ordens de serviço</summary>
/// <remarks>
/// Estes passos fazem parte da saga da OS: reservar depois da aprovação,
/// consumir quando a execução termina, liberar como compensação. Na etapa 5 a
/// saga passa a chamá-los por mensagem; as rotas continuam para operação manual.
/// </remarks>
[Authorize(Policy = Politicas.Admin)]
[ApiController]
[Route("api/[controller]")]
public class ReservasController : ControllerBase
{
    private readonly ReservarPecasUseCase _reservarUseCase;
    private readonly LiberarReservaUseCase _liberarUseCase;
    private readonly ConsumirReservaUseCase _consumirUseCase;
    private readonly ObterReservaUseCase _obterUseCase;

    /// <summary>Inicializa o controller com os use cases de reserva</summary>
    public ReservasController(
        ReservarPecasUseCase reservarUseCase,
        LiberarReservaUseCase liberarUseCase,
        ConsumirReservaUseCase consumirUseCase,
        ObterReservaUseCase obterUseCase)
    {
        _reservarUseCase = reservarUseCase;
        _liberarUseCase = liberarUseCase;
        _consumirUseCase = consumirUseCase;
        _obterUseCase = obterUseCase;
    }

    /// <summary>Reserva as peças de uma ordem de serviço</summary>
    /// <remarks>
    /// Tudo ou nada: se faltar qualquer peça, nenhuma é reservada e a resposta é 409.
    /// Idempotente: repetir o pedido para a mesma OS devolve a reserva existente com 200.
    /// </remarks>
    /// <response code="201">Reserva criada</response>
    /// <response code="200">A OS já tinha reserva; a existente é devolvida</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="404">Item de estoque não encontrado</response>
    /// <response code="409">Estoque insuficiente</response>
    [HttpPost]
    [ProducesResponseType(typeof(ReservaPecasResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ReservaPecasResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reservar([FromBody] ReservarPecasRequest request)
    {
        var validation = await new ReservarPecasValidator().ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var (reserva, criada) = await _reservarUseCase.ExecutarAsync(request);

        return criada
            ? CreatedAtAction(nameof(Obter), new { ordemDeServicoId = reserva.OrdemDeServicoId }, reserva)
            : Ok(reserva);
    }

    /// <summary>Obtém a reserva de peças de uma ordem de serviço</summary>
    /// <response code="200">Reserva encontrada</response>
    /// <response code="404">A OS não tem reserva</response>
    [HttpGet("{ordemDeServicoId:guid}")]
    [ProducesResponseType(typeof(ReservaPecasResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(Guid ordemDeServicoId)
    {
        return Ok(await _obterUseCase.ExecutarAsync(ordemDeServicoId));
    }

    /// <summary>Libera a reserva (compensação: pagamento recusado, OS cancelada)</summary>
    /// <remarks>Idempotente. Uma reserva já consumida não pode ser liberada (409).</remarks>
    /// <response code="204">Reserva liberada (ou já estava)</response>
    /// <response code="404">A OS não tem reserva</response>
    /// <response code="409">A reserva já foi consumida</response>
    [HttpPost("{ordemDeServicoId:guid}/liberar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Liberar(Guid ordemDeServicoId)
    {
        await _liberarUseCase.ExecutarAsync(ordemDeServicoId);
        return NoContent();
    }

    /// <summary>Dá baixa definitiva nas peças reservadas</summary>
    /// <remarks>Idempotente. Uma reserva já liberada não pode ser consumida (409).</remarks>
    /// <response code="204">Peças baixadas (ou já estavam)</response>
    /// <response code="404">A OS não tem reserva</response>
    /// <response code="409">A reserva já foi liberada</response>
    [HttpPost("{ordemDeServicoId:guid}/consumir")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Consumir(Guid ordemDeServicoId)
    {
        await _consumirUseCase.ExecutarAsync(ordemDeServicoId);
        return NoContent();
    }
}
