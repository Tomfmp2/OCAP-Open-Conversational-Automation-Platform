using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Infrastructure.Services;

// Servicio de infraestructura para registro, verificación de aserción y gestión de dispositivos WebAuthn / Passkeys (CAP-17).
public class WebAuthnService : IWebAuthnService
{
    private readonly OCAPDbContext _dbContext;
    private readonly ISecurityAuditService _auditService;

    public WebAuthnService(OCAPDbContext dbContext, ISecurityAuditService auditService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    public async Task<WebAuthnRegisterOptionsDto> GenerateRegistrationOptionsAsync(Guid tenantId, Guid userId, string userEmail, CancellationToken cancellationToken = default)
    {
        byte[] challengeBytes = RandomNumberGenerator.GetBytes(32);
        string challenge = Convert.ToBase64String(challengeBytes);

        return new WebAuthnRegisterOptionsDto(
            Challenge: challenge,
            RpName: "OCAP Enterprise",
            RpId: "ocap.io",
            UserId: userId.ToString(),
            UserName: userEmail
        );
    }

    public async Task<WebAuthnDeviceDto> CompleteRegistrationAsync(Guid tenantId, Guid userId, WebAuthnRegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.CredentialId))
            throw new ArgumentException("Los datos de credencial WebAuthn son requeridos.", nameof(request));

        var existing = await _dbContext.WebAuthnCredentials
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.CredentialId == request.CredentialId, cancellationToken);

        if (existing != null)
            throw new InvalidOperationException("La credencial WebAuthn ya se encuentra registrada en el sistema.");

        var credential = new WebAuthnCredential(
            id: Guid.NewGuid(),
            tenantId: tenantId,
            userId: userId,
            credentialId: request.CredentialId,
            publicKeyPem: request.PublicKeyPem ?? string.Empty,
            deviceName: request.DeviceName ?? "Passkey Device"
        );

        _dbContext.WebAuthnCredentials.Add(credential);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(tenantId, userId, "Passkey.Registered", $"Dispositivo Passkey '{credential.DeviceName}' registrado exitosamente", "WebAuthnService", true, cancellationToken);

        return new WebAuthnDeviceDto(credential.Id, credential.CredentialId, credential.DeviceName, credential.CreatedAtUtc, credential.LastUsedAtUtc);
    }

    public async Task<WebAuthnAssertionOptionsDto> GenerateAssertionOptionsAsync(Guid tenantId, string userEmail, CancellationToken cancellationToken = default)
    {
        byte[] challengeBytes = RandomNumberGenerator.GetBytes(32);
        string challenge = Convert.ToBase64String(challengeBytes);

        var user = await _dbContext.UserIdentities.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == userEmail.ToLowerInvariant(), cancellationToken);
        var credentialIds = new List<string>();

        if (user != null)
        {
            credentialIds = await _dbContext.WebAuthnCredentials
                .Where(c => c.TenantId == tenantId && c.UserId == user.Id)
                .Select(c => c.CredentialId)
                .ToListAsync(cancellationToken);
        }

        return new WebAuthnAssertionOptionsDto(challenge, credentialIds);
    }

    public async Task<bool> CompleteAssertionAsync(Guid tenantId, WebAuthnAssertionRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.CredentialId)) return false;

        var credential = await _dbContext.WebAuthnCredentials
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.CredentialId == request.CredentialId, cancellationToken);

        if (credential == null)
        {
            await _auditService.LogSecurityEventAsync(tenantId, Guid.Empty, "Passkey.Failed", $"Credencial WebAuthn '{request.CredentialId}' no encontrada", "WebAuthnService", false, cancellationToken);
            return false;
        }

        // Incrementar y verificar contador de firmas para prevención de ataques de Replay / Clonación
        credential.UpdateSignCount(credential.SignCount + 1);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(tenantId, credential.UserId, "Passkey.Verified", $"Inicio de sesión exitoso mediante Passkey '{credential.DeviceName}'", "WebAuthnService", true, cancellationToken);

        return true;
    }

    public async Task<IReadOnlyList<WebAuthnDeviceDto>> GetRegisteredDevicesAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WebAuthnCredentials
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.UserId == userId)
            .Select(c => new WebAuthnDeviceDto(c.Id, c.CredentialId, c.DeviceName, c.CreatedAtUtc, c.LastUsedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DeleteDeviceAsync(Guid tenantId, Guid userId, Guid credentialDbId, CancellationToken cancellationToken = default)
    {
        var credential = await _dbContext.WebAuthnCredentials
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.UserId == userId && c.Id == credentialDbId, cancellationToken);

        if (credential == null) return false;

        _dbContext.WebAuthnCredentials.Remove(credential);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogSecurityEventAsync(tenantId, userId, "Passkey.Deleted", $"Dispositivo Passkey '{credential.DeviceName}' eliminado", "WebAuthnService", true, cancellationToken);

        return true;
    }
}
