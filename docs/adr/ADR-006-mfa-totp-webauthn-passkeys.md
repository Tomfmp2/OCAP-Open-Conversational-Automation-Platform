# ADR-006: Autenticación Fuerte mediante MFA (TOTP) y Passkeys (WebAuthn / FIDO2) (CAP-17)

## Estado
Aprobado

## Contexto
Para alcanzar cumplimiento estricto con OWASP ASVS (Authentication Verification Standard Level 2/3) y estándares empresariales FIDO2, OCAP requiere mecanismos de autenticación multifactor (MFA) basados en estándares abiertos (RFC 6238 TOTP y WebAuthn Level 2).

## Decisiones de Diseño

1. **TOTP (RFC 6238 / RFC 4226)**:
   - Los secretos TOTP se generan con 160 bits de entropía (20 bytes aleatorios mediante `RandomNumberGenerator`), codificados en Base32.
   - **Cifrado de Secretos**: Los secretos TOTP nunca se almacenan en texto plano en la base de datos; se cifran con AES-256-GCM a través de `ICredentialVault`.
   - **Protección contra Timing Attacks**: La verificación compara hashes/códigos utilizando `CryptographicOperations.FixedTimeEquals`.
   - **Tolerancia a Deriva**: Ventana temporal de $\pm 1$ paso (30s atrasado, actual, 30s adelantado).

2. **Códigos de Recuperación (Recovery Codes)**:
   - Se generan 8 códigos de uso único de 10 caracteres hexadecimales formateados (`xxxxx-xxxxx`).
   - Los códigos se almacenan exclusivamente como hashes PBKDF2 con Salt dinámico. Se destruyen al ser utilizados.

3. **Passkeys / WebAuthn Level 2**:
   - Registro y aserción FIDO2 almacenando `CredentialId`, `PublicKeyPem` y `SignCount`.
   - **Detección de Clonación/Replay**: Verificación estricta de incremento monótono en `SignCount`.

## Consecuencias
- Cero almacenamiento de secretos en claro.
- Compatibilidad nativa con Google Authenticator, Authy, YubiKey, Touch ID, Face ID y Windows Hello.
