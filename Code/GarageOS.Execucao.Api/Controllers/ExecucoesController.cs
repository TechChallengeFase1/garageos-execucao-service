using GarageOS.Execucao.Api.Seguranca;
using GarageOS.Execucao.Application.DTOs.Execucoes;
using GarageOS.Execucao.Application.UseCases.Execucoes;
using GarageOS.Execucao.Application.Validators.Execucoes;
using GarageOS.Execucao.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GarageOS.Execucao.Api.Controllers;

/// <summary>Fila de execução da oficina: diagnóstico, reparo e conclusão</summary>
[Authorize(Policy = Politicas.Admin)]
[ApiController]
[Route("api/[controller]")]
public class ExecucoesController : ControllerBase
{
    private readonly CriarOrdemDeExecucaoUseCase _criarUseCase;
    private readonly ListarFilaDeExecucaoUseCase _listarUseCase;
    private readonly ObterOrdemDeExecucaoUseCase _obterUseCase;
    private readonly AlterarStatusExecucaoUseCase _alterarStatusUseCase;
    private readonly AlterarStatusItemExecucaoUseCase _alterarStatusItemUseCase;
    private readonly CalcularAgingServicosUseCase _agingUseCase;

    /// <summary>Inicializa o controller com os use cases de execução</summary>
    public ExecucoesController(
        CriarOrdemDeExecucaoUseCase criarUseCase,
        ListarFilaDeExecucaoUseCase listarUseCase,
        ObterOrdemDeExecucaoUseCase obterUseCase,
        AlterarStatusExecucaoUseCase alterarStatusUseCase,
        AlterarStatusItemExecucaoUseCase alterarStatusItemUseCase,
        CalcularAgingServicosUseCase agingUseCase)
    {
        _criarUseCase = criarUseCase;
        _listarUseCase = listarUseCase;
        _obterUseCase = obterUseCase;
        _alterarStatusUseCase = alterarStatusUseCase;
        _alterarStatusItemUseCase = alterarStatusItemUseCase;
        _agingUseCase = agingUseCase;
    }

    /// <summary>Coloca uma ordem de serviço na fila de execução</summary>
    /// <remarks>
    /// Recebe a OS com os serviços a executar (ID e nome, copiados do OS Service).
    /// Idempotente: a mesma OS não entra duas vezes; o pedido repetido devolve a existente com 200.
    /// </remarks>
    /// <response code="201">OS colocada na fila</response>
    /// <response code="200">A OS já estava na fila</response>
    /// <response code="400">Dados inválidos</response>
    [HttpPost]
    [ProducesResponseType(typeof(OrdemDeExecucaoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(OrdemDeExecucaoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar([FromBody] CriarOrdemDeExecucaoRequest request)
    {
        var validation = await new CriarOrdemDeExecucaoValidator().ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        var (ordem, criada) = await _criarUseCase.ExecutarAsync(request);

        return criada
            ? CreatedAtAction(nameof(Obter), new { id = ordem.Id }, ordem)
            : Ok(ordem);
    }

    /// <summary>Lista a fila da oficina</summary>
    /// <remarks>
    /// Sem filtro, mostra o que ainda está na oficina, na ordem de atendimento:
    /// EmReparo, EmDiagnostico e NaFila, da mais antiga para a mais nova.
    /// Com <c>status</c>, mostra só as execuções naquele status, inclusive as finais.
    /// </remarks>
    /// <param name="status">Filtro opcional: NaFila, EmDiagnostico, EmReparo, Concluida ou Falhou</param>
    /// <response code="200">Fila retornada</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrdemDeExecucaoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] StatusExecucao? status)
    {
        return Ok(await _listarUseCase.ExecutarAsync(status));
    }

    /// <summary>Tempo médio de execução por serviço (aging)</summary>
    /// <response code="200">Relatório retornado</response>
    [HttpGet("aging")]
    [ProducesResponseType(typeof(IEnumerable<AgingServicoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Aging()
    {
        return Ok(await _agingUseCase.ExecutarAsync());
    }

    /// <summary>Obtém uma ordem de execução, com serviços e histórico</summary>
    /// <response code="200">Ordem de execução encontrada</response>
    /// <response code="404">Não encontrada</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrdemDeExecucaoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obter(Guid id)
    {
        return Ok(await _obterUseCase.ExecutarAsync(id));
    }

    /// <summary>Avança a execução</summary>
    /// <remarks>
    /// Transições permitidas:
    /// - NaFila → EmDiagnostico ou Falhou
    /// - EmDiagnostico → EmReparo ou Falhou
    /// - EmReparo → Concluida (todos os serviços finalizados) ou Falhou
    ///
    /// Falhou exige <c>motivo</c>.
    /// </remarks>
    /// <response code="200">Status alterado</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="404">Não encontrada</response>
    /// <response code="409">Transição não permitida</response>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(OrdemDeExecucaoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AlterarStatus(Guid id, [FromBody] AlterarStatusExecucaoRequest request)
    {
        var validation = await new AlterarStatusExecucaoValidator().ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage));

        return Ok(await _alterarStatusUseCase.ExecutarAsync(id, request));
    }

    /// <summary>Inicia ou finaliza um serviço da execução</summary>
    /// <response code="200">Serviço atualizado</response>
    /// <response code="400">Status inválido ou serviço já iniciado/finalizado</response>
    /// <response code="404">Execução ou serviço não encontrado</response>
    /// <response code="409">Execução já encerrada</response>
    [HttpPatch("{id:guid}/itens/{itemId:guid}/status")]
    [ProducesResponseType(typeof(OrdemDeExecucaoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AlterarStatusItem(Guid id, Guid itemId, [FromBody] AlterarStatusItemExecucaoRequest request)
    {
        return Ok(await _alterarStatusItemUseCase.ExecutarAsync(id, itemId, request));
    }
}
