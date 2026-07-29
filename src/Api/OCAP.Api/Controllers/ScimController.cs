using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;

namespace OCAP.Api.Controllers;

// Controlador REST de protocolo estandarizado SCIM 2.0 (RFC 7643 / RFC 7644) (CAP-19).
[ApiController]
[Route("scim/v2")]
[Produces("application/scim+json", "application/json")]
public class ScimController : ControllerBase
{
    private readonly IScimService _scimService;
    private readonly ITenantContext _tenantContext;

    public ScimController(IScimService scimService, ITenantContext tenantContext)
    {
        _scimService = scimService ?? throw new ArgumentNullException(nameof(scimService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    [HttpGet("Users")]
    [Authorize]
    public async Task<IActionResult> GetUsers([FromQuery] int startIndex = 1, [FromQuery] int count = 100, [FromQuery] string? filter = null, CancellationToken cancellationToken = default)
    {
        var result = await _scimService.GetUsersAsync(_tenantContext.TenantId, startIndex, count, filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("Users/{id}")]
    [Authorize]
    public async Task<IActionResult> GetUserById(string id, CancellationToken cancellationToken)
    {
        var user = await _scimService.GetUserByIdAsync(_tenantContext.TenantId, id, cancellationToken);
        if (user == null)
            return NotFound(new ScimErrorDto("404", "notFound", $"Usuario '{id}' no encontrado.", new List<string> { "urn:ietf:params:scim:api:messages:2.0:Error" }));

        return Ok(user);
    }

    [HttpPost("Users")]
    [Authorize]
    public async Task<IActionResult> CreateUser([FromBody] ScimUserDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _scimService.CreateUserAsync(_tenantContext.TenantId, dto, cancellationToken);
            return Created($"/scim/v2/Users/{user.id}", user);
        }
        catch (Exception ex)
        {
            return BadRequest(new ScimErrorDto("400", "invalidValue", ex.Message, new List<string> { "urn:ietf:params:scim:api:messages:2.0:Error" }));
        }
    }

    [HttpPut("Users/{id}")]
    [HttpPatch("Users/{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] ScimUserDto dto, CancellationToken cancellationToken)
    {
        var user = await _scimService.UpdateUserAsync(_tenantContext.TenantId, id, dto, cancellationToken);
        if (user == null)
            return NotFound(new ScimErrorDto("404", "notFound", $"Usuario '{id}' no encontrado.", new List<string> { "urn:ietf:params:scim:api:messages:2.0:Error" }));

        return Ok(user);
    }

    [HttpDelete("Users/{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteUser(string id, CancellationToken cancellationToken)
    {
        var success = await _scimService.DeleteUserAsync(_tenantContext.TenantId, id, cancellationToken);
        if (!success)
            return NotFound(new ScimErrorDto("404", "notFound", $"Usuario '{id}' no encontrado.", new List<string> { "urn:ietf:params:scim:api:messages:2.0:Error" }));

        return NoContent();
    }

    [HttpGet("Groups")]
    [Authorize]
    public async Task<IActionResult> GetGroups([FromQuery] int startIndex = 1, [FromQuery] int count = 100, [FromQuery] string? filter = null, CancellationToken cancellationToken = default)
    {
        var result = await _scimService.GetGroupsAsync(_tenantContext.TenantId, startIndex, count, filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("Groups/{id}")]
    [Authorize]
    public async Task<IActionResult> GetGroupById(string id, CancellationToken cancellationToken)
    {
        var group = await _scimService.GetGroupByIdAsync(_tenantContext.TenantId, id, cancellationToken);
        if (group == null)
            return NotFound(new ScimErrorDto("404", "notFound", $"Grupo '{id}' no encontrado.", new List<string> { "urn:ietf:params:scim:api:messages:2.0:Error" }));

        return Ok(group);
    }

    [HttpPost("Groups")]
    [Authorize]
    public async Task<IActionResult> CreateGroup([FromBody] ScimGroupDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var group = await _scimService.CreateGroupAsync(_tenantContext.TenantId, dto, cancellationToken);
            return Created($"/scim/v2/Groups/{group.id}", group);
        }
        catch (Exception ex)
        {
            return BadRequest(new ScimErrorDto("400", "invalidValue", ex.Message, new List<string> { "urn:ietf:params:scim:api:messages:2.0:Error" }));
        }
    }

    [HttpPut("Groups/{id}")]
    [HttpPatch("Groups/{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateGroup(string id, [FromBody] ScimGroupDto dto, CancellationToken cancellationToken)
    {
        var group = await _scimService.UpdateGroupAsync(_tenantContext.TenantId, id, dto, cancellationToken);
        if (group == null)
            return NotFound(new ScimErrorDto("404", "notFound", $"Grupo '{id}' no encontrado.", new List<string> { "urn:ietf:params:scim:api:messages:2.0:Error" }));

        return Ok(group);
    }

    [HttpDelete("Groups/{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteGroup(string id, CancellationToken cancellationToken)
    {
        var success = await _scimService.DeleteGroupAsync(_tenantContext.TenantId, id, cancellationToken);
        if (!success)
            return NotFound(new ScimErrorDto("404", "notFound", $"Grupo '{id}' no encontrado.", new List<string> { "urn:ietf:params:scim:api:messages:2.0:Error" }));

        return NoContent();
    }

    [HttpPost("Bulk")]
    [Authorize]
    public async Task<IActionResult> ProcessBulk([FromBody] ScimBulkRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _scimService.ProcessBulkRequestAsync(_tenantContext.TenantId, request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("ServiceProviderConfig")]
    [AllowAnonymous]
    public IActionResult GetServiceProviderConfig() => Ok(_scimService.GetServiceProviderConfig());

    [HttpGet("ResourceTypes")]
    [AllowAnonymous]
    public IActionResult GetResourceTypes() => Ok(_scimService.GetResourceTypes());

    [HttpGet("Schemas")]
    [AllowAnonymous]
    public IActionResult GetSchemas() => Ok(_scimService.GetSchemas());
}
