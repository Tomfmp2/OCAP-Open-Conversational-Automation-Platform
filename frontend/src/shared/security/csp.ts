/**
 * Content Security Policy del panel. Los scripts solo corren con el nonce de la petición: aunque se colara HTML
 * (XSS), un <script> inyectado no se ejecuta, y los tokens de sesión del almacenamiento local quedan fuera de su
 * alcance. Sin upgrade-insecure-requests: el panel también se sirve por http en la red local.
 */
export function buildContentSecurityPolicy(nonce: string, options: { isDev: boolean; apiUrl?: string }): string {
  const connect = ["'self'"];
  const api = apiOrigin(options.apiUrl);
  if (api) connect.push(api, api.replace(/^http/, "ws"));

  return [
    "default-src 'self'",
    `script-src 'self' 'nonce-${nonce}' 'strict-dynamic'${options.isDev ? " 'unsafe-eval'" : ""}`,
    // Estilos en línea: los usan Framer Motion y los componentes (atributo style); no ejecutan código.
    "style-src 'self' 'unsafe-inline'",
    "img-src 'self' blob: data:",
    "font-src 'self'",
    `connect-src ${connect.join(" ")}`,
    "object-src 'none'",
    "base-uri 'self'",
    "form-action 'self'",
    "frame-ancestors 'none'",
  ].join("; ");
}

function apiOrigin(apiUrl?: string): string | null {
  if (!apiUrl) return null;
  try {
    return new URL(apiUrl).origin;
  } catch {
    return null;
  }
}
