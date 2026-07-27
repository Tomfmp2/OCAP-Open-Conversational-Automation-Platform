namespace OCAP.Api.Middlewares;

// Middleware de seguridad que inyecta encabezados de protección HTTP (CSP, HSTS, X-Frame-Options, X-Content-Type-Options).
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Content Security Policy (CSP) restrictiva para mitigar vulnerabilidades XSS e inyecciones de script.
        context.Response.Headers.Append("Content-Security-Policy",
            "default-src 'self'; script-src 'self' 'unsafe-wasm'; style-src 'self' 'unsafe-inline'; font-src 'self' data:; img-src 'self' data:; connect-src 'self';");

        // 2. HTTP Strict Transport Security (HSTS) para forzar conexiones HTTPS seguras en producción.
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");

        // 3. Previene la inferencia de tipos MIME no declarados (MIME-sniffing).
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        // 4. Previene la incrustación del sitio en iFrames para evitar ataques de Clickjacking.
        context.Response.Headers.Append("X-Frame-Options", "DENY");

        // 5. Restringe la fuga de información de origen en el encabezado Referrer.
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // 6. Habilita la protección nativa del navegador contra Cross-Site Scripting (XSS).
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

        await _next(context);
    }
}
