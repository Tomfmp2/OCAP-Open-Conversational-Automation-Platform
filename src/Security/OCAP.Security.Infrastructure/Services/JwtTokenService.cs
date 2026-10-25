using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using OCAP.Security.Abstractions;
using OCAP.Security.Abstractions.Options;
using OCAP.Security.Domain.Entities;

namespace OCAP.Security.Infrastructure.Services;

// Servicio de emisión y validación de tokens JWT estructurados con Claims de seguridad.
public class JwtTokenService : IJwtTokenService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpiryMinutes;

    public JwtTokenService(JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        _secretKey = options.SecretKey;
        _issuer = options.Issuer;
        _audience = options.Audience;
        _accessTokenExpiryMinutes = options.AccessTokenExpiryMinutes;
    }

    /// <summary>
    /// Constructor de compatibilidad para tests unitarios que inyectan el secreto explícitamente.
    /// </summary>
    public JwtTokenService(string secretKey, string issuer = "OCAP", string audience = "OCAP.Clients", int accessTokenExpiryMinutes = 60)
        : this(new JwtOptions
        {
            SecretKey = secretKey,
            Issuer = issuer,
            Audience = audience,
            AccessTokenExpiryMinutes = accessTokenExpiryMinutes
        })
    {
    }

    public string GenerateAccessToken(UserIdentity user, Tenant tenant, Role role, IEnumerable<string> permissions)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new("tenant_id", tenant.Id.ToString()),
            new("tenant_slug", tenant.Slug),
            new(ClaimTypes.Role, role.Name)
        };

        foreach (var perm in permissions)
        {
            claims.Add(new Claim("permission", perm));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public RefreshToken GenerateRefreshToken(Guid userId)
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var tokenString = Convert.ToBase64String(randomBytes);
        return new RefreshToken(Guid.NewGuid(), userId, tokenString, DateTime.UtcNow.AddDays(7));
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            return tokenHandler.ValidateToken(token, validationParameters, out _);
        }
        catch
        {
            return null;
        }
    }

    private const string MfaPurposeClaim = "purpose";
    private static readonly TimeSpan MfaChallengeLifetime = TimeSpan.FromMinutes(5);
    private string MfaAudience => $"{_audience}.mfa";

    public string GenerateMfaChallengeToken(Guid userId, Guid tenantId)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("tenant_id", tenantId.ToString()),
                new Claim(MfaPurposeClaim, "mfa")
            ]),
            Expires = DateTime.UtcNow.Add(MfaChallengeLifetime),
            Issuer = _issuer,
            Audience = MfaAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey)), SecurityAlgorithms.HmacSha256Signature)
        };
        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    public (Guid UserId, Guid TenantId)? ValidateMfaChallengeToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey)),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = MfaAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            if (principal.FindFirst(MfaPurposeClaim)?.Value != "mfa") return null;
            return Guid.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)
                   && Guid.TryParse(principal.FindFirst("tenant_id")?.Value, out var tenantId)
                ? (userId, tenantId)
                : null;
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            return null;
        }
    }
}
