# OCAP — Especificación de Claves de API (X-API-Key)

## Formato y Almacenamiento
- Encabezado HTTP: `X-API-Key`.
- Estructura: `ocap_live_<32_caracteres_aleatorios>`.
- Almacenamiento: **Únicamente Hash SHA-256 en base de datos**.
- Nunca se almacenan ni muestran claves de API en texto plano tras su emisión.

## Rotación y Revocación
- Permite definir fecha de expiración personalizada.
- Marcado instantáneo de revocación mediante `IsRevoked = true`.
- Registro automático de fecha del último uso (`LastUsedAtUtc`).
