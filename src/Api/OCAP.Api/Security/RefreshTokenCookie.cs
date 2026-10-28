using OCAP.Security.Abstractions;

namespace OCAP.Api.Security;

/// <summary>
/// El refresh token del panel viaja solo en una cookie HttpOnly: JavaScript (y por tanto un XSS) no puede leerlo.
/// SameSite=Strict y la ruta /api/auth hacen que el navegador la envíe únicamente al renovar o cerrar la sesión desde
/// el propio panel. El access token, de vida corta, queda en la memoria de la pestaña.
/// </summary>
public static class RefreshTokenCookie
{
    public const string Name = "ocap_refresh";
    private const string Path = "/api/auth";

    public static void Append(HttpResponse response, IssuedRefreshToken token) =>
        response.Cookies.Append(Name, token.Token, Options(response.HttpContext.Request, token.ExpiresAtUtc));

    public static string? Read(HttpRequest request) =>
        request.Cookies.TryGetValue(Name, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    public static void Delete(HttpResponse response) =>
        response.Cookies.Delete(Name, Options(response.HttpContext.Request, expires: null));

    private static CookieOptions Options(HttpRequest request, DateTime? expires) => new()
    {
        HttpOnly = true,
        // Detrás de Nginx con TLS llega como HTTPS gracias a los forwarded headers; en http://localhost no se puede
        // exigir Secure o el navegador no la guardaría.
        Secure = request.IsHttps,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        Expires = expires,
        IsEssential = true,
    };
}
