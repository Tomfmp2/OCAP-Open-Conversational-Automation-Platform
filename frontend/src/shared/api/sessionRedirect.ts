/**
 * Lleva al login con una recarga completa, no con el router: al perder o cerrar la sesión hay que descartar
 * el estado en memoria (caché de React Query, stores) de la cuenta anterior.
 */
export function redirectToLogin(): void {
  if (typeof window === "undefined" || window.location.pathname.startsWith("/login")) return;
  window.location.assign(new URL("/login", window.location.origin).href);
}
