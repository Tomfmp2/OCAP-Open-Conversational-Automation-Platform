import {
  MessageSquare,
  Cpu,
  Bot,
  Settings,
  BookOpen,
  Sparkles,
  Inbox,
  Users,
  type LucideIcon,
} from "lucide-react";

export interface NavItem {
  href: string;
  label: string;
  icon: LucideIcon;
  permission?: string;
  section?: string;
}

export interface NavSection {
  label: string;
  items: Array<{ label: string; href: string }>;
}

/** Pantallas sin sesión y sin el marco del panel (menú, barra superior). */
export const PUBLIC_ROUTES = ["/login", "/setup", "/accept", "/forgot-password"];

export function isPublicRoute(pathname: string): boolean {
  return PUBLIC_ROUTES.some((route) => pathname === route || pathname.startsWith(`${route}/`));
}

// Permisos del rol (los mismos códigos que exige la API). Un Operador ve el asistente y la bandeja.
export const PRIMARY_NAV: NavItem[] = [
  { href: "/", label: "Asistente", icon: Sparkles, section: "Core", permission: "AI.Execute" },
  { href: "/inbox", label: "Conversaciones", icon: Inbox, section: "Core", permission: "Conversation.Read" },
  { href: "/channels", label: "Canales", icon: MessageSquare, section: "Core", permission: "Channel.Manage" },
  { href: "/intelligence", label: "IA y modelos", icon: Cpu, section: "Core", permission: "Settings.Manage" },
  { href: "/agents", label: "Agentes", icon: Bot, section: "Core", permission: "Agent.Write" },
  { href: "/knowledge", label: "Conocimiento", icon: BookOpen, section: "Core", permission: "Knowledge.Manage" },
];

export const SECONDARY_NAV_FOOTER: NavItem[] = [
  { href: "/team", label: "Equipo", icon: Users, permission: "Security.Manage" },
  { href: "/settings", label: "Ajustes", icon: Settings },
];

// Rutas sin entrada propia en el menú.
const EXTRA_ROUTE_PERMISSIONS: Array<{ href: string; permission: string }> = [
  { href: "/channels/webchat", permission: "Channel.Manage" },
  { href: "/security", permission: "Security.Manage" },
  { href: "/developer", permission: "Deployment.Manage" },
  { href: "/monitoring", permission: "Deployment.Manage" },
];

/** Permiso que exige una ruta del panel (el más específico que coincida); undefined = cualquier sesión. */
export function requiredPermissionFor(pathname: string): string | undefined {
  const candidates = [...PRIMARY_NAV, ...SECONDARY_NAV_FOOTER, ...EXTRA_ROUTE_PERMISSIONS]
    .filter((item) => item.permission)
    .filter((item) => (item.href === "/" ? pathname === "/" : pathname === item.href || pathname.startsWith(`${item.href}/`)))
    .sort((a, b) => b.href.length - a.href.length);
  return candidates[0]?.permission;
}

export const COMMAND_ITEMS = [
  { label: "Asistente principal", href: "/", category: "Navegación" },
  { label: "Conversaciones", href: "/inbox", category: "Navegación" },
  { label: "Canales", href: "/channels", category: "Navegación" },
  { label: "WebChat", href: "/channels/webchat", category: "Navegación" },
  { label: "IA y modelos", href: "/intelligence", category: "Navegación" },
  { label: "Agentes", href: "/agents", category: "Navegación" },
  { label: "Conocimiento", href: "/knowledge", category: "Navegación" },
  { label: "Equipo", href: "/team", category: "Navegación" },
  { label: "Mi cuenta", href: "/account", category: "Navegación" },
  { label: "Ajustes", href: "/settings", category: "Navegación" },
] as const;

export function getSubmenuForPath(pathname: string): NavSection {
  if (pathname === "/" || pathname === "") {
    return {
      label: "Asistente",
      items: [{ label: "Chat principal", href: "/" }],
    };
  }
  if (pathname.startsWith("/inbox")) {
    return {
      label: "Conversaciones",
      items: [{ label: "Bandeja", href: "/inbox" }],
    };
  }
  if (pathname.startsWith("/channels")) {
    return {
      label: "Canales",
      items: [
        { label: "Conexiones", href: "/channels" },
        { label: "Telegram", href: "/channels?provider=Telegram" },
        { label: "WhatsApp", href: "/channels?provider=WhatsApp" },
        { label: "WebChat", href: "/channels/webchat" },
      ],
    };
  }
  if (pathname.startsWith("/intelligence")) {
    return {
      label: "IA y modelos",
      items: [{ label: "Proveedores", href: "/intelligence" }],
    };
  }
  if (pathname.startsWith("/agents")) {
    return {
      label: "Agentes",
      items: [{ label: "Catálogo", href: "/agents" }],
    };
  }
  if (pathname.startsWith("/knowledge")) {
    return {
      label: "Conocimiento",
      items: [
        { label: "Bases", href: "/knowledge" },
        { label: "Búsqueda", href: "/knowledge#search" },
      ],
    };
  }
  if (pathname.startsWith("/team")) {
    return {
      label: "Equipo",
      items: [{ label: "Personas", href: "/team" }],
    };
  }
  if (pathname.startsWith("/settings") || pathname.startsWith("/account")) {
    return {
      label: "Ajustes",
      items: [
        { label: "Preferencias", href: "/settings" },
        { label: "Mi cuenta", href: "/account" },
      ],
    };
  }
  return {
    label: "Asistente",
    items: [{ label: "Chat principal", href: "/" }],
  };
}
