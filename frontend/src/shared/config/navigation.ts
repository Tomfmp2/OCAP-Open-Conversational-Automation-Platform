import { BookOpen, Bot, Cpu, Inbox, MessageSquare, Settings, Sparkles, Users, type LucideIcon } from "lucide-react";

export interface NavItem {
  href: string;
  label: string;
  icon: LucideIcon;
  /** Permiso del rol (el mismo código que exige la API); sin él, cualquier sesión. */
  permission?: string;
}

/** Pantallas sin sesión y sin el marco del panel. */
export const PUBLIC_ROUTES = ["/login", "/setup", "/accept", "/forgot-password"];

export function isPublicRoute(pathname: string): boolean {
  return PUBLIC_ROUTES.some((route) => pathname === route || pathname.startsWith(`${route}/`));
}

// En el orden en que se usa el producto: hablar, atender, conectar, configurar. Un Operador ve el asistente y la bandeja.
export const MAIN_NAV: NavItem[] = [
  { href: "/", label: "Asistente", icon: Sparkles, permission: "AI.Execute" },
  { href: "/inbox", label: "Conversaciones", icon: Inbox, permission: "Conversation.Read" },
  { href: "/channels", label: "Canales", icon: MessageSquare, permission: "Channel.Manage" },
  { href: "/agents", label: "Agentes", icon: Bot, permission: "Agent.Write" },
  { href: "/knowledge", label: "Conocimiento", icon: BookOpen, permission: "Knowledge.Manage" },
  { href: "/intelligence", label: "Modelos de IA", icon: Cpu, permission: "Settings.Manage" },
];

export const ADMIN_NAV: NavItem[] = [
  { href: "/team", label: "Equipo", icon: Users, permission: "Security.Manage" },
  { href: "/settings", label: "Organización", icon: Settings, permission: "Settings.Manage" },
];

export function isActive(pathname: string, href: string): boolean {
  return href === "/" ? pathname === "/" : pathname === href || pathname.startsWith(`${href}/`);
}

/** Permiso que exige una ruta del panel (el más específico que coincida); undefined = cualquier sesión. */
export function requiredPermissionFor(pathname: string): string | undefined {
  const candidates = [...MAIN_NAV, ...ADMIN_NAV]
    .filter((item) => item.permission && isActive(pathname, item.href))
    .sort((a, b) => b.href.length - a.href.length);
  return candidates[0]?.permission;
}
