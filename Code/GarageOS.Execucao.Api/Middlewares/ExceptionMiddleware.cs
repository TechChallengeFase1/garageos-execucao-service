using System.Net;
using System.Text.Json;
using GarageOS.Execucao.Domain.Exceptions;
using GarageOS.Execucao.Domain.Utils;

namespace GarageOS.Execucao.Api.Middlewares;

/// <summary>Middleware global de tratamento de exceções</summary>
public class ExceptionMiddleware
{
    private const string MsgNaoEncontrado = "Não encontrado: {Message}";
    private const string MsgConflito = "Conflito: {Message}";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    /// <summary>Inicializa o middleware com o delegate e logger</summary>
    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>Processa a requisição e captura exceções não tratadas</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Erro: {Message}", ex.Message);
            await EscreverRespostaAsync(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex) when (ex is EstoqueNaoEncontradoException
                                       or ReservaNaoEncontradaException
                                       or OrdemDeExecucaoNaoEncontradaException
                                       or ItemExecucaoNaoEncontradoException)
        {
            _logger.LogWarning(ex, MsgNaoEncontrado, ex.Message);
            await EscreverRespostaAsync(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (Exception ex) when (ex is EstoqueInsuficienteException
                                       or TransicaoDeStatusInvalidaException
                                       or ReservaStatusInvalidoException
                                       or ConflitoDeConcorrenciaException
                                       or InvalidOperationException)
        {
            _logger.LogWarning(ex, MsgConflito, ex.Message);
            await EscreverRespostaAsync(context, HttpStatusCode.Conflict, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado: {Message}", ex.Message);
            await EscreverRespostaAsync(context, HttpStatusCode.InternalServerError,
                "Ocorreu um erro interno. Tente novamente mais tarde.");
        }
    }

    private static async Task EscreverRespostaAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string mensagem)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var resposta = new
        {
            status = (int)statusCode,
            erro = mensagem,
            timestamp = BrasiliaTime.Agora
        };

        var json = JsonSerializer.Serialize(resposta, JsonOptions);

        await context.Response.WriteAsync(json);
    }
}
