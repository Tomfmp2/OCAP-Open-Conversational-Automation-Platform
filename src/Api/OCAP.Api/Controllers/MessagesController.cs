using Microsoft.AspNetCore.Mvc;
using OCAP.Application.UseCases;
using OCAP.Api.DTOs.Requests;
using OCAP.Api.DTOs.Responses;

namespace OCAP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly ReceiveMessageUseCase _receiveMessageUseCase;

    public MessagesController(ReceiveMessageUseCase receiveMessageUseCase)
    {
        _receiveMessageUseCase = receiveMessageUseCase;
    }

    // Endpoint para recibir un nuevo mensaje
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] IncomingMessageRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "La petición contiene errores de validación",
                Data = ModelState
            });
        }

        await _receiveMessageUseCase.ExecuteAsync(request.UserId!.Value, request.MessageContent, request.Provider, cancellationToken);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Mensaje procesado con éxito",
            Data = null
        });
    }
}
