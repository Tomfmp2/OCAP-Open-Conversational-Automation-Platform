/**
 * Permisos del rol tal como los emite la API en el JWT (claim "permission", uno o varios). Solo sirven para decidir
 * qué mostrar: la API vuelve a comprobar cada permiso en cada petición.
 */
export function permissionsFromToken(token: string | null | undefined): string[] {
  const payload = decodePayload(token);
  const claim = payload?.permission;
  if (Array.isArray(claim)) return claim.filter((p): p is string => typeof p === "string");
  return typeof claim === "string" ? [claim] : [];
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
