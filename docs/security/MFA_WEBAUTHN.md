# Autenticación Fuertemente Asegurada: MFA (TOTP) y WebAuthn / Passkeys (CAP-17)

Documentación técnica y especificaciones de seguridad para la autenticación multifactor y Passkeys en OCAP.

## Endpoints API REST

### MFA / TOTP (`/api/auth/mfa`)
- `POST /api/auth/mfa/setup`: Genera el secreto TOTP cifrado y la URI QR (`otpauth://totp/...`).
- `POST /api/auth/mfa/enable`: Verifica el código inicial de 6 dígitos, activa MFA y entrega 8 códigos de recuperación de uso único.
- `POST /api/auth/mfa/disable`: Desactiva MFA mediante verificación de código.
- `POST /api/auth/mfa/verify`: Valida un código TOTP o código de recuperación durante el flujo de inicio de sesión.
- `POST /api/auth/mfa/recovery-codes/regenerate`: Invalida los códigos anteriores y entrega 8 nuevos códigos de recuperación.

### WebAuthn / Passkeys (`/api/auth/webauthn`)
- `POST /api/auth/webauthn/register/options`: Obtiene el desafío FIDO2 para registrar un nuevo dispositivo.
- `POST /api/auth/webauthn/register/complete`: Registra la credencial WebAuthn pública y le asigna un nombre al dispositivo.
- `POST /api/auth/webauthn/assertion/options`: Obtiene las opciones de desafío para login con Passkey.
- `POST /api/auth/webauthn/assertion/complete`: Verifica la aserción criptográfica y valida el contador de firmas (`SignCount`).
- `GET /api/auth/webauthn/devices`: Lista los dispositivos Passkey vinculados al usuario.
- `DELETE /api/auth/webauthn/devices/{id}`: Elimina un dispositivo Passkey.
