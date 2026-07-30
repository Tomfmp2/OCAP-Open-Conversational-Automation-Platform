using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCAP.Application.UseCases;
using OCAP.Api.DTOs.Responses;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Controllers;

public record ConversationSummaryDto(
    Guid Id,
    Guid UserId,
    string Status,
    DateTime CreatedAt,
    DateTime LastActivityAt
);

[ApiController]
[Route("api/[controller]")]
public class ConversationsController : ControllerBase
{
    private readonly GetConversationHistoryUseCase _getConversationHistoryUseCase;
    private readonly OCAPDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public ConversationsController(
        GetConversationHistoryUseCase getConversationHistoryUseCase,
        OCAPDbContext dbContext,
        ITenantContext tenantContext)
    {
        _getConversationHistoryUseCase = getConversationHistoryUseCase ?? throw new ArgumentNullException(nameof(getConversationHistoryUseCase));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    // GET /api/conversations - Lista conversaciones paginadas del tenant
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Conversations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.UserId.ToString().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var list = await query
            .OrderByDescending(c => c.LastActivityAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ConversationSummaryDto(
                c.Id,
                c.UserId,
                c.Status.ToString(),
                c.CreatedAt,
                c.LastActivityAt
            ))
            .ToListAsync(cancellationToken);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Conversaciones obtenidas con éxito.",
            Data = new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Items = list
            }
        });
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

    // DELETE /api/conversations/{id} - Elimina una conversación de la base de datos
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var conversation = await _dbContext.Conversations.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (conversation == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = $"No se encontró la conversación con ID {id}",
                Data = null
            });
        }

        _dbContext.Conversations.Remove(conversation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = $"Conversación {id} eliminada con éxito",
            Data = null
        });
    }
}
