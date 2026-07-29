using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Infrastructure.Services;

// Servicio de infraestructura para orquestación segura de MFA (TOTP / Recovery Codes / Encrypted Secrets) (CAP-17).
public class MfaService : IMfaService
{
    private readonly OCAPDbContext _dbContext;
    private readonly ITotpService _totpService;
    private readonly ICredentialVault _credentialVault;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISecurityAuditService _auditService;

    public MfaService(
        OCAPDbContext dbContext,
        ITotpService totpService,
        ICredentialVault credentialVault,
        IPasswordHasher passwordHasher,
        ISecurityAuditService auditService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _totpService = totpService ?? throw new ArgumentNullException(nameof(totpService));
        _credentialVault = credentialVault ?? throw new ArgumentNullException(nameof(credentialVault));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    public async Task<MfaSetupDto> SetupMfaAsync(Guid tenantId, Guid userId, string userEmail, CancellationToken cancellationToken = default)
    {
        var secretKey = _totpService.GenerateSecretKey();
        var secretRef = await _credentialVault.StoreSecretAsync(tenantId, $"MFA_TOTP_{userId}", secretKey, cancellationToken);

        var existing = await _dbContext.UserMfaSettings.FirstOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == userId, cancellationToken);
        if (existing == null)
        {
            var newSettings = new UserMfaSettings(Guid.NewGuid(), tenantId, userId, secretRef);
            _dbContext.UserMfaSettings.Add(newSettings);
        }
        else
        {
            existing.UpdateSecret(secretRef);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        var qrCodeUri = _totpService.GenerateQrCodeUri(userEmail, secretKey);

        await _auditService.LogSecurityEventAsync(tenantId, userId, "Mfa.SetupInitiated", "Secreto TOTP generado y almacenado de forma cifrada", "MfaService", true, cancellationToken);

        return new MfaSetupDto(secretKey, qrCodeUri);
    }

    public async Task<EnableMfaResponseDto> EnableMfaAsync(Guid tenantId, Guid userId, string code, CancellationToken cancellationToken = default)
    {
        var settings = await _dbContext.UserMfaSettings.FirstOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == userId, cancellationToken);
        if (settings == null) throw new InvalidOperationException("No se ha iniciado la configuración de MFA.");

        var secretKey = await _credentialVault.RetrieveSecretAsync(tenantId, settings.EncryptedTotpSecret, cancellationToken) ?? string.Empty;
        if (!_totpService.ValidateCode(secretKey, code))
        {
            await _auditService.LogSecurityEventAsync(tenantId, userId, "Mfa.EnableFailed", "Código TOTP no válido al intentar activar MFA", "MfaService", false, cancellationToken);
            throw new ArgumentException("El código TOTP ingresado no es válido.");
        }

        settings.Enable();

        // Generar 8 códigos de recuperación seguros
        var rawCodes = GenerateRawRecoveryCodes(8);
        var existingCodes = await _dbContext.UserRecoveryCodes.Where(r => r.TenantId == tenantId && r.UserId == userId).ToListAsync(cancellationToken);
        _dbContext.UserRecoveryCodes.RemoveRange(existingCodes);

        foreach (var rawCode in rawCodes)
        {
            var (hash, salt) = _passwordHasher.HashPassword(rawCode);
            _dbContext.UserRecoveryCodes.Add(new UserRecoveryCode(Guid.NewGuid(), tenantId, userId, hash, salt));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogSecurityEventAsync(tenantId, userId, "Mfa.Enabled", "MFA (TOTP) activado exitosamente", "MfaService", true, cancellationToken);

        return new EnableMfaResponseDto(rawCodes);
    }

    public async Task<bool> DisableMfaAsync(Guid tenantId, Guid userId, string code, CancellationToken cancellationToken = default)
    {
        var settings = await _dbContext.UserMfaSettings.FirstOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == userId, cancellationToken);
        if (settings == null || !settings.IsEnabled) return false;

        var isValid = await VerifyMfaCodeAsync(tenantId, userId, code, cancellationToken);
        if (!isValid)
        {
            await _auditService.LogSecurityEventAsync(tenantId, userId, "Mfa.DisableFailed", "Código de verificación no válido al intentar desactivar MFA", "MfaService", false, cancellationToken);
            return false;
        }

        settings.Disable();
        var existingCodes = await _dbContext.UserRecoveryCodes.Where(r => r.TenantId == tenantId && r.UserId == userId).ToListAsync(cancellationToken);
        _dbContext.UserRecoveryCodes.RemoveRange(existingCodes);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogSecurityEventAsync(tenantId, userId, "Mfa.Disabled", "MFA (TOTP) desactivado exitosamente", "MfaService", true, cancellationToken);

        return true;
    }

    public async Task<bool> VerifyMfaCodeAsync(Guid tenantId, Guid userId, string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;

        var settings = await _dbContext.UserMfaSettings.FirstOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == userId, cancellationToken);
        if (settings != null && settings.IsEnabled)
        {
            var secretKey = await _credentialVault.RetrieveSecretAsync(tenantId, settings.EncryptedTotpSecret, cancellationToken) ?? string.Empty;
            if (_totpService.ValidateCode(secretKey, code))
            {
                await _auditService.LogSecurityEventAsync(tenantId, userId, "Mfa.Verified", "Código TOTP verificado correctamente", "MfaService", true, cancellationToken);
                return true;
            }
        }

        // Si falla la verificación TOTP, intentar con códigos de recuperación de uso único
        var recoveryCodes = await _dbContext.UserRecoveryCodes
            .Where(r => r.TenantId == tenantId && r.UserId == userId && !r.IsUsed)
            .ToListAsync(cancellationToken);

        foreach (var recovery in recoveryCodes)
        {
            if (_passwordHasher.VerifyPassword(code.Trim(), recovery.CodeHash, recovery.Salt))
            {
                recovery.MarkAsUsed();
                await _dbContext.SaveChangesAsync(cancellationToken);
                await _auditService.LogSecurityEventAsync(tenantId, userId, "RecoveryCode.Used", "Código de recuperación utilizado de forma segura", "MfaService", true, cancellationToken);
                return true;
            }
        }

        await _auditService.LogSecurityEventAsync(tenantId, userId, "Mfa.Failed", "Intento de verificación MFA o Código de Recuperación fallido", "MfaService", false, cancellationToken);
        return false;
    }

    public async Task<IReadOnlyList<string>> RegenerateRecoveryCodesAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        var settings = await _dbContext.UserMfaSettings.FirstOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == userId, cancellationToken);
        if (settings == null || !settings.IsEnabled) throw new InvalidOperationException("MFA debe estar habilitado para regenerar códigos de recuperación.");

        var existingCodes = await _dbContext.UserRecoveryCodes.Where(r => r.TenantId == tenantId && r.UserId == userId).ToListAsync(cancellationToken);
        _dbContext.UserRecoveryCodes.RemoveRange(existingCodes);

        var rawCodes = GenerateRawRecoveryCodes(8);
        foreach (var rawCode in rawCodes)
        {
            var (hash, salt) = _passwordHasher.HashPassword(rawCode);
            _dbContext.UserRecoveryCodes.Add(new UserRecoveryCode(Guid.NewGuid(), tenantId, userId, hash, salt));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogSecurityEventAsync(tenantId, userId, "RecoveryCode.Regenerated", "Nuevos códigos de recuperación generados", "MfaService", true, cancellationToken);

        return rawCodes;
    }

    private static List<string> GenerateRawRecoveryCodes(int count)
    {
        var codes = new List<string>(count);
        for (int i = 0; i < count; i++)
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(5);
            string hex = Convert.ToHexString(bytes).ToLowerInvariant();
            codes.Add($"{hex[..5]}-{hex[5..]}");
        }
        return codes;
    }
}
