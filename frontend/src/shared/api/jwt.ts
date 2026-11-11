/** Ajustes de toda la instalación: lo lleva una sola cuenta y ningún rol lo incluye. */
export const SYSTEM_MANAGE = "System.Manage";

/**
 * Permisos del rol tal como los emite la API en el JWT (claim "permission", uno o varios). Solo sirven para decidir
 * qué mostrar: la API vuelve a comprobar cada permiso en cada petición.
 */
export function permissionsFromToken(token: string | null | undefined): string[] {
  const payload = decodePayload(token);
  const claim = payload?.permission;
  const fromRole = Array.isArray(claim) ? claim.filter((p): p is string => typeof p === "string") : typeof claim === "string" ? [claim] : [];
  // El sistema no es un permiso de rol: la API lo marca aparte en la cuenta que instaló.
  const permissions = fromRole.filter((p) => p !== SYSTEM_MANAGE);
  return payload?.system_admin === "true" ? [...permissions, SYSTEM_MANAGE] : permissions;
}

function decodePayload(token: string | null | undefined): Record<string, unknown> | null {
  const part = token?.split(".")[1];
  if (!part) return null;
  try {
    const base64 = part.replace(/-/g, "+").replace(/_/g, "/").padEnd(Math.ceil(part.length / 4) * 4, "=");
    const json = new TextDecoder().decode(Uint8Array.from(atob(base64), (c) => c.charCodeAt(0)));
    return JSON.parse(json) as Record<string, unknown>;
  } catch {
    return null;
  }
}
