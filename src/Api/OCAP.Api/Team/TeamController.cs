using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCAP.Api.Security;
using OCAP.Security.Abstractions;

namespace OCAP.Api.Team;

public sealed record InviteMemberRequest(string Email, string FullName, TeamRole Role);
public sealed record ChangeMemberRoleRequest(TeamRole Role);
public sealed record InviteMemberResponse(TeamMemberDto Member, PasswordLinkDto Link);

/// <summary>Equipo de la organización (Ajustes → Equipo).</summary>
[ApiController]
[Route("api/team")]
[Authorize(Policy = OcapPermissions.SecurityManage)]
public sealed class TeamController : ControllerBase
{
    private readonly TeamService _team;
    private readonly ITenantContext _tenant;
    private readonly IUserContext _user;

    public TeamController(TeamService team, ITenantContext tenant, IUserContext user)
    {
        _team = team;
        _tenant = tenant;
        _user = user;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(new { members = await _team.ListAsync(TenantId, cancellationToken), emailConfigured = await _team.IsEmailConfiguredAsync(TenantId, cancellationToken) });

    [HttpPost("invitations")]
    public async Task<IActionResult> Invite([FromBody] InviteMemberRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Length > 100)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "El nombre es obligatorio (máximo 100 caracteres).");
        if (!System.Net.Mail.MailAddress.TryCreate(request.Email?.Trim(), out _) || request.Email!.Length > 254)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "El correo no es válido.");

        return await Rule(async () =>
        {
            var (member, link) = await _team.InviteAsync(TenantId, UserId, request.Email, request.FullName, request.Role, cancellationToken);
            return Ok(new InviteMemberResponse(member, link));
        });
    }

    [HttpPost("{id:guid}/password-link")]
    public async Task<IActionResult> PasswordLink(Guid id, CancellationToken cancellationToken)
    {
        var link = await _team.CreatePasswordLinkAsync(TenantId, UserId, id, cancellationToken);
        return link is null ? NotFound() : Ok(link);
    }

    [HttpPut("{id:guid}/role")]
    public Task<IActionResult> ChangeRole(Guid id, [FromBody] ChangeMemberRoleRequest request, CancellationToken cancellationToken) =>
        Rule(async () => await _team.ChangeRoleAsync(TenantId, UserId, id, request.Role, cancellationToken) ? NoContent() : NotFound());

    [HttpPost("{id:guid}/deactivate")]
    public Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken) =>
        Rule(async () => await _team.SetActiveAsync(TenantId, UserId, id, active: false, cancellationToken) ? NoContent() : NotFound());

    [HttpPost("{id:guid}/activate")]
    public Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken) =>
        Rule(async () => await _team.SetActiveAsync(TenantId, UserId, id, active: true, cancellationToken) ? NoContent() : NotFound());

    [HttpPost("{id:guid}/unlock")]
    public async Task<IActionResult> Unlock(Guid id, CancellationToken cancellationToken) =>
        await _team.UnlockAsync(TenantId, UserId, id, cancellationToken) ? NoContent() : NotFound();

    private Guid TenantId => TenantSecurity.RequireTenantId(_tenant);
    private Guid UserId => TenantSecurity.RequireUserId(_user);

    private async Task<IActionResult> Rule(Func<Task<IActionResult>> action)
    {
        try
        {
            return await action();
        }
        catch (TeamRuleException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: ex.Message);
        }
    }
}
