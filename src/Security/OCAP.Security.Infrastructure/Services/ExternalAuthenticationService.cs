using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OCAP.Infrastructure.Persistence.Context;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.DTOs;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Infrastructure.Services;

// Servicio orquestador de autenticación, aprovisionamiento y vinculación de proveedores de identidad externos (CAP-15).
public class ExternalAuthenticationService : IExternalAuthenticationService
{
    private readonly IEnumerable<IExternalAuthProvider> _providers;
    private readonly IExternalIdentityResolver _identityResolver;
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ISecurityAuditService _auditService;
    private readonly OCAPDbContext _dbContext;
    private readonly ExternalAuthenticationSettings _settings;

    public ExternalAuthenticationService(
        IEnumerable<IExternalAuthProvider> providers,
        IExternalIdentityResolver identityResolver,
        IIdentityService identityService,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        ISecurityAuditService auditService,
        OCAPDbContext dbContext,
        IOptions<ExternalAuthenticationSettings> options)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _identityResolver = identityResolver ?? throw new ArgumentNullException(nameof(identityResolver));
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _settings = options.Value ?? new ExternalAuthenticationSettings();
    }

    public Task<IReadOnlyList<ExternalProviderInfoDto>> GetEnabledProvidersAsync(CancellationToken cancellationToken = default)
    {
        var list = _providers
            .Where(p => p.IsEnabled)
            .Select(p => new ExternalProviderInfoDto(p.ProviderName, p.DisplayName, p.IsEnabled, null))
            .ToList();

        return Task.FromResult<IReadOnlyList<ExternalProviderInfoDto>>(list);
    }

    public Task<ExternalAuthChallengeDto> InitiateChallengeAsync(string provider, string redirectUri, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var providerInstance = GetProvider(provider);
        var state = Guid.NewGuid().ToString("N");
        var authUrl = providerInstance.BuildAuthorizationUrl(redirectUri, state);

        return Task.FromResult(new ExternalAuthChallengeDto(providerInstance.ProviderName, authUrl, state));
    }

    public async Task<ExternalAuthLoginResultDto> ProcessCallbackAsync(ExternalAuthCallbackRequestDto request, Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Code))
        {
            return Failure("Solicitud de callback nula o código faltante.");
        }

        var providerInstance = GetProvider(request.Provider);
        var redirectUri = request.RedirectUri ?? "https://localhost:7000/api/auth/external/callback/" + providerInstance.ProviderName;

        var payload = await providerInstance.ProcessCallbackAsync(request.Code, redirectUri, cancellationToken);
        if (payload == null || string.IsNullOrWhiteSpace(payload.ExternalId))
        {
            await _auditService.LogSecurityEventAsync(tenantId, Guid.Empty, "ExternalAuth.Failed", $"Autenticación fallida con proveedor {request.Provider}", "ExternalAuthenticationService", false, cancellationToken);
            return Failure($"No se pudo verificar la identidad con el proveedor '{request.Provider}'.");
        }

        // 1. Resolver UserId vinculado existente
        var userId = await _identityResolver.ResolveUserIdAsync(tenantId, payload.Provider, payload.ExternalId, cancellationToken);
        UserIdentity? user = null;

        if (userId.HasValue)
        {
            user = await _dbContext.UserIdentities.FirstOrDefaultAsync(u => u.Id == userId.Value && u.TenantId == tenantId, cancellationToken);
        }

        // 2. Si no está vinculado, buscar por Email
        if (user == null && !string.IsNullOrWhiteSpace(payload.Email))
        {
            user = await _dbContext.UserIdentities.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == payload.Email.ToLowerInvariant(), cancellationToken);
            if (user != null)
            {
                await _identityResolver.LinkExternalIdentityAsync(tenantId, user.Id, payload.Provider, payload.ExternalId, null, cancellationToken);
            }
        }

        // 3. Aprovisionamiento automático de usuario si no existe
        if (user == null)
        {
            if (!_settings.AutoProvisionUsers)
            {
                await _auditService.LogSecurityEventAsync(tenantId, Guid.Empty, "ExternalAuth.Denied", $"Usuario no registrado y auto-aprovisionamiento desactivado para email '{payload.Email}'", "ExternalAuthenticationService", false, cancellationToken);
                return Failure("La cuenta no está registrada en el sistema y el aprovisionamiento automático está deshabilitado.");
            }

            var newUserId = Guid.NewGuid();
            var dummyHash = Guid.NewGuid().ToString("N");
            var dummySalt = Guid.NewGuid().ToString("N");
            var email = string.IsNullOrWhiteSpace(payload.Email) ? $"{payload.ExternalId}@{payload.Provider}.external" : payload.Email;

            user = new UserIdentity(newUserId, tenantId, email, dummyHash, dummySalt, payload.FullName);
            _dbContext.UserIdentities.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _identityResolver.LinkExternalIdentityAsync(tenantId, user.Id, payload.Provider, payload.ExternalId, null, cancellationToken);

            await _auditService.LogSecurityEventAsync(tenantId, user.Id, "User.AutoProvisioned", $"Usuario auto-aprovisionado desde proveedor externo {payload.Provider}", "ExternalAuthenticationService", true, cancellationToken);
        }

        if (!user.IsActive)
        {
            await _auditService.LogSecurityEventAsync(tenantId, user.Id, "ExternalAuth.Denied", "Intento de inicio de sesión de usuario inactivo", "ExternalAuthenticationService", false, cancellationToken);
            return Failure("La cuenta de usuario se encuentra inactiva.");
        }

        // 4. Emitir tokens JWT y RefreshToken de OCAP
        var tenantObj = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            ?? new Tenant(tenantId, "Default Tenant", "default");

        var roles = await _identityService.GetUserRolesAsync(user.Id, tenantId, cancellationToken);
        var permissions = await _identityService.GetUserPermissionsAsync(user.Id, tenantId, cancellationToken);

        var primaryRole = roles.FirstOrDefault() ?? new Role(Guid.NewGuid(), tenantId, "User", "Default Role", permissions);
        var accessToken = _jwtTokenService.GenerateAccessToken(user, tenantObj, primaryRole, permissions);
        var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(user.Id, TimeSpan.FromDays(7), cancellationToken);

        await _auditService.LogSecurityEventAsync(tenantId, user.Id, "ExternalAuth.Success", $"Inicio de sesión exitoso mediante {payload.Provider}", "ExternalAuthenticationService", true, cancellationToken);

        return new ExternalAuthLoginResultDto(
            true,
            accessToken,
            refreshToken.Token,
            user.Id,
            tenantId,
            user.Email,
            user.FullName,
            null
        );
    }

    public async Task<bool> LinkProviderAsync(Guid tenantId, Guid userId, string provider, string code, string redirectUri, CancellationToken cancellationToken = default)
    {
        var providerInstance = GetProvider(provider);
        var payload = await providerInstance.ProcessCallbackAsync(code, redirectUri, cancellationToken);
        if (payload == null || string.IsNullOrWhiteSpace(payload.ExternalId)) return false;

        var linked = await _identityResolver.LinkExternalIdentityAsync(tenantId, userId, payload.Provider, payload.ExternalId, null, cancellationToken);
        if (linked)
        {
            await _auditService.LogSecurityEventAsync(tenantId, userId, "ExternalIdentity.Linked", $"Proveedor '{payload.Provider}' vinculado exitosamente", "ExternalAuthenticationService", true, cancellationToken);
        }

        return linked;
    }

    public async Task<bool> UnlinkProviderAsync(Guid tenantId, Guid userId, string provider, CancellationToken cancellationToken = default)
    {
        var unlinked = await _identityResolver.UnlinkExternalIdentityAsync(tenantId, userId, provider, cancellationToken);
        if (unlinked)
        {
            await _auditService.LogSecurityEventAsync(tenantId, userId, "ExternalIdentity.Unlinked", $"Proveedor '{provider}' desvinculado exitosamente", "ExternalAuthenticationService", true, cancellationToken);
        }

        return unlinked;
    }

    public async Task<IReadOnlyList<string>> GetLinkedProvidersAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        var identities = await _identityResolver.GetLinkedIdentitiesAsync(tenantId, userId, cancellationToken);
        return identities.Select(i => i.Provider).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private IExternalAuthProvider GetProvider(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("El nombre del proveedor es obligatorio.", nameof(providerName));

        var instance = _providers.FirstOrDefault(p => string.Equals(p.ProviderName, providerName, StringComparison.OrdinalIgnoreCase));
        if (instance == null)
        {
            throw new KeyNotFoundException($"El proveedor de autenticación externo '{providerName}' no está soportado o no está configurado.");
        }

        return instance;
    }

    private static ExternalAuthLoginResultDto Failure(string message) =>
        new(false, null, null, null, null, null, null, message);
}
