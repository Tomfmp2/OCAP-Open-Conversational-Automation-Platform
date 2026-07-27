using Microsoft.AspNetCore.Mvc;
using OCAP.Application.UseCases;
using OCAP.Api.DTOs.Responses;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConversationsController : ControllerBase
{
    private readonly GetConversationHistoryUseCase _getConversationHistoryUseCase;

    public ConversationsController(GetConversationHistoryUseCase getConversationHistoryUseCase)
    {
        _getConversationHistoryUseCase = getConversationHistoryUseCase;
    }

    // Endpoint para obtener el historial de una conversación
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var conversation = await _getConversationHistoryUseCase.ExecuteAsync(id, cancellationToken);
        if (conversation == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = $"No se encontró la conversación con el ID {id}",
                Data = null
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Conversación obtenida con éxito",
            Data = conversation
        });
    }
}
