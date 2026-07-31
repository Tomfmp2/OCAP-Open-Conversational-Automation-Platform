import {
  LayoutDashboard,
  MessageSquare,
  Cpu,
  Bot,
  GitFork,
  ShieldCheck,
  Settings,
  Code2,
  Activity,
  BookOpen,
  Wrench,
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

export const PRIMARY_NAV: NavItem[] = [
  { href: "/", label: "Resumen", icon: LayoutDashboard, section: "Overview" },
  { href: "/channels", label: "Canales", icon: MessageSquare, section: "Overview" },
  { href: "/intelligence", label: "IA & Modelos", icon: Cpu, section: "Overview" },
  { href: "/agents", label: "Agentes", icon: Bot, section: "Overview" },
  { href: "/workflows", label: "Workflows", icon: GitFork, section: "Activity" },
  { href: "/knowledge", label: "Knowledge", icon: BookOpen, section: "Activity" },
  { href: "/monitoring", label: "Monitoreo", icon: Activity, section: "Activity" },
  { href: "/developer", label: "Developer", icon: Code2, section: "Account" },
  {
    href: "/security",
    label: "Seguridad",
    icon: ShieldCheck,
    section: "Account",
    permission: "Security.Manage",
  },
];

export const SECONDARY_NAV_FOOTER: NavItem[] = [
  { href: "/settings", label: "Ajustes", icon: Settings },
  { href: "/installer", label: "Instalador", icon: Wrench },
];

export const COMMAND_ITEMS = [
  { label: "Ir a Resumen", href: "/", category: "Navegación" },
  { label: "Canales", href: "/channels", category: "Navegación" },
  { label: "IA & Modelos", href: "/intelligence", category: "Navegación" },
  { label: "Agentes", href: "/agents", category: "Navegación" },
  { label: "Workflows", href: "/workflows", category: "Navegación" },
  { label: "Knowledge Base", href: "/knowledge", category: "Navegación" },
  { label: "Monitoreo", href: "/monitoring", category: "Navegación" },
  { label: "Developer Center", href: "/developer", category: "Navegación" },
  { label: "Seguridad", href: "/security", category: "Navegación" },
  { label: "Ajustes", href: "/settings", category: "Navegación" },
  { label: "Instalador", href: "/installer", category: "Sistema" },
] as const;

export function getSubmenuForPath(pathname: string): NavSection {
  if (pathname.startsWith("/channels")) {
    return {
      label: "Canales",
      items: [
        { label: "Conexiones", href: "/channels" },
        { label: "Telegram", href: "/channels?provider=Telegram" },
        { label: "WhatsApp", href: "/channels?provider=WhatsApp" },
      ],
    };
  }
  if (pathname.startsWith("/intelligence")) {
    return {
      label: "Inteligencia",
      items: [
        { label: "Proveedores", href: "/intelligence" },
        { label: "Credential Vault", href: "/intelligence#vault" },
      ],
    };
  }
  if (pathname.startsWith("/agents")) {
    return {
      label: "Agentes",
      items: [
        { label: "Catálogo", href: "/agents" },
        { label: "Trazas", href: "/agents#traces" },
      ],
    };
  }
  if (pathname.startsWith("/workflows")) {
    return {
      label: "Workflows",
      items: [
        { label: "Definiciones", href: "/workflows" },
        { label: "Ejecuciones", href: "/workflows#executions" },
      ],
    };
  }
  if (pathname.startsWith("/knowledge")) {
    return {
      label: "Knowledge",
      items: [
        { label: "Bases", href: "/knowledge" },
        { label: "Búsqueda", href: "/knowledge#search" },
      ],
    };
  }
  if (pathname.startsWith("/monitoring")) {
    return {
      label: "Monitoreo",
      items: [
        { label: "Métricas", href: "/monitoring" },
        { label: "Auditoría", href: "/monitoring#audit" },
      ],
    };
  }
  if (pathname.startsWith("/developer")) {
    return {
      label: "Developer",
      items: [
        { label: "API Keys", href: "/developer" },
        { label: "Webhooks", href: "/developer#webhooks" },
      ],
    };
  }
  if (pathname.startsWith("/security")) {
    return {
      label: "Seguridad",
      items: [
        { label: "RBAC", href: "/security" },
        { label: "Sesiones", href: "/security#sessions" },
      ],
    };
  }
  if (pathname.startsWith("/settings")) {
    return {
      label: "Ajustes",
      items: [{ label: "Preferencias", href: "/settings" }],
    };
  }
  if (pathname.startsWith("/installer")) {
    return {
      label: "Instalador",
      items: [
        { label: "Asistente de configuración", href: "/installer" },
        { label: "Diagnóstico", href: "/installer" },
      ],
    };
  }
  return {
    label: "Resumen",
    items: [
      { label: "Vista general", href: "/" },
      { label: "Actividad", href: "/#activity" },
      { label: "Tiempo real", href: "/#live" },
    ],
  };
}
