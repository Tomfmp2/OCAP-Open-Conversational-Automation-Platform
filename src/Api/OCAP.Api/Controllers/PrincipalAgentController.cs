using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCAP.Agents.Abstractions.Models;
using OCAP.Agents.Application.Services;
using OCAP.Api.DTOs.Responses;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Controllers;

public class PrincipalAgentMessageRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Chat del agente madre (inicio del panel). No requiere canal WebChat.
/// </summary>
[ApiController]
[Authorize]
[Route("api/agents/principal")]
public class PrincipalAgentController : ControllerBase
{
    private readonly IEnterpriseAssistantAgent _assistantAgent;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly ILogger<PrincipalAgentController> _logger;

    public PrincipalAgentController(
        IEnterpriseAssistantAgent assistantAgent,
        ITenantContext tenantContext,
        IUserContext userContext,
        ILogger<PrincipalAgentController> logger)
    {
        _assistantAgent = assistantAgent;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Info()
    {
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Agente principal OCAP",
            Data = new
            {
                agentId = _assistantAgent.GlobalAgentId,
                name = "Agente principal",
                capabilities = new[]
                {
                    "Consultas del sistema OCAP",
                    "Gmail (enviar / listar)",
                    "Google Calendar",
                    "Google Sheets"
                }
            }
        });
    }

    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage(
        [FromBody] PrincipalAgentMessageRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _userContext.UserId;

        if (tenantId == Guid.Empty || userId == Guid.Empty)
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Sesión no identificada.",
                Data = null
            });
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Message es requerido.",
                Data = null
            });
        }

        var sessionId = string.IsNullOrWhiteSpace(request.SessionId)
            ? Guid.NewGuid().ToString("N")
            : request.SessionId.Trim();

        try
        {
            var context = new AgentContext(
                _assistantAgent.GlobalAgentId,
                tenantId,
                userId,
                request.Message.Trim());

            // Propagar sesión en variables de entorno del contexto si el modelo lo soporta
            context.EnvironmentVariables["SessionId"] = sessionId;

            var result = await _assistantAgent.ProcessRequestAsync(context, cancellationToken);
            var reply = string.IsNullOrWhiteSpace(result.OutputMessage)
                ? "He recibido tu mensaje."
                : result.OutputMessage;

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Mensaje procesado.",
                Data = new
                {
                    sessionId,
                    reply,
                    providerUsed = string.IsNullOrWhiteSpace(result.ProviderUsed)
                        ? "unknown"
                        : (result.Metadata.TryGetValue("ModelUsed", out var model) && model != null
                            ? $"{result.ProviderUsed}/{model}"
                            : result.ProviderUsed),
                    agentId = _assistantAgent.GlobalAgentId,
                    metadata = result.Metadata
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en chat del agente principal.");
            var friendly = ex is OperationCanceledException || ex is TaskCanceledException
                ? "Gemini no respondió a tiempo (timeout). Suele ser red/firewall hacia Google, API key lenta o modelo saturado. Prueba el proveedor en IA y modelos; si falla igual, revisa la key y la conexión a generativelanguage.googleapis.com."
                : ex.Message.Contains("no longer available", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("NOT_FOUND", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("is not found", StringComparison.OrdinalIgnoreCase)
                ? "Gemini rechazó el modelo (obsoleto para keys nuevas). Reinicia la API tras actualizar .env; OCAP ahora reintenta con gemini-3.5-flash / flash-latest."
                : ex.Message.Contains("429", StringComparison.Ordinal)
                || ex.Message.Contains("TooManyRequests", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("quota", StringComparison.OrdinalIgnoreCase)
                ? "Gemini rechazó la solicitud por cuota/límite (429). Espera unos segundos o revisa tu plan en Google AI Studio."
                : ex.Message.Contains("API Key", StringComparison.OrdinalIgnoreCase)
                    ? "Falta o es inválida la API key de Gemini. Revisa AiProviders__Gemini__ApiKey en .env."
                    : ex.Message;

            return StatusCode(503, new ApiResponse<object>
            {
                Success = false,
                Message = friendly,
                Data = null
            });
        }
    }
}
