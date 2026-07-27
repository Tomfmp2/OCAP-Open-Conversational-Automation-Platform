# OCAP — Políticas y Medidas de Seguridad de Producción

## Principios
1. **Fail Secure**: Ante cualquier falla de autenticación o permiso no concedido, el acceso se deniega por defecto (`401 Unauthorized` / `403 Forbidden`).
2. **Security by Design**: Todas las claves de API, secretos de JWT y contraseñas se gestionan mediante variables de entorno y algoritmos seguros.

## Encabezados de Seguridad
El middleware `SecurityHeadersMiddleware` inyecta automáticamente:
- `Content-Security-Policy`: Inyección restrictiva de scripts y fuentes.
- `Strict-Transport-Security` (HSTS): Coerción de conexiones HTTPS en producción.
- `X-Content-Type-Options`: `nosniff`.
- `X-Frame-Options`: `DENY`.
- `Referrer-Policy`: `strict-origin-when-cross-origin`.
- `X-XSS-Protection`: `1; mode=block`.
