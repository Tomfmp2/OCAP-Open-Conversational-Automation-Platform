using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;

namespace OCAP.Api.Controllers;

// Controlador REST de administración de Grupos de Usuarios (CAP-16).
[ApiController]
[Route("api/groups")]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly IGroupService _groupService;
    private readonly ITenantContext _tenantContext;

    public GroupsController(IGroupService groupService, ITenantContext tenantContext)
    {
        _groupService = groupService ?? throw new ArgumentNullException(nameof(groupService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<GroupDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGroups(CancellationToken cancellationToken)
    {
        var groups = await _groupService.GetGroupsAsync(_tenantContext.TenantId, cancellationToken);
        return Ok(groups);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroupById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var group = await _groupService.GetGroupByIdAsync(_tenantContext.TenantId, id, cancellationToken);
        if (group == null) return NotFound(new { error = $"Grupo '{id}' no encontrado." });

        return Ok(group);
    }

    [HttpPost]
    [ProducesResponseType(typeof(GroupDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var group = await _groupService.CreateGroupAsync(_tenantContext.TenantId, request, cancellationToken);
            return CreatedAtAction(nameof(GetGroupById), new { id = group.Id }, group);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteGroup([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _groupService.DeleteGroupAsync(_tenantContext.TenantId, id, cancellationToken);
        if (!deleted) return BadRequest(new { error = $"No se pudo eliminar el grupo '{id}'." });

        return Ok(new { message = "Grupo eliminado exitosamente." });
    }

    [HttpPost("{id:guid}/users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddUserToGroup([FromRoute] Guid id, [FromBody] Guid userId, CancellationToken cancellationToken)
    {
        var added = await _groupService.AddUserToGroupAsync(_tenantContext.TenantId, id, userId, cancellationToken);
        if (!added) return BadRequest(new { error = "No se pudo añadir el usuario al grupo." });

        return Ok(new { message = "Usuario añadido al grupo exitosamente." });
    }

    [HttpDelete("{id:guid}/users/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveUserFromGroup([FromRoute] Guid id, [FromRoute] Guid userId, CancellationToken cancellationToken)
    {
        var removed = await _groupService.RemoveUserFromGroupAsync(_tenantContext.TenantId, id, userId, cancellationToken);
        if (!removed) return BadRequest(new { error = "No se pudo remover el usuario del grupo." });

        return Ok(new { message = "Usuario removido del grupo exitosamente." });
    }

    [HttpPost("{id:guid}/roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignRoleToGroup([FromRoute] Guid id, [FromBody] Guid roleId, CancellationToken cancellationToken)
    {
        var assigned = await _groupService.AssignRoleToGroupAsync(_tenantContext.TenantId, id, roleId, cancellationToken);
        if (!assigned) return BadRequest(new { error = "No se pudo asignar el rol al grupo." });

        return Ok(new { message = "Rol asignado al grupo exitosamente." });
    }
}
