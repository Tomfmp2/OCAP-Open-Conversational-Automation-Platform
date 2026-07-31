import {
  LayoutDashboard,
  MessageSquare,
  Cpu,
  Bot,
  GitFork,
  Settings,
  BookOpen,
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
  { href: "/", label: "Resumen", icon: LayoutDashboard, section: "Core" },
  { href: "/channels", label: "Canales", icon: MessageSquare, section: "Core" },
  { href: "/intelligence", label: "IA y modelos", icon: Cpu, section: "Core" },
  { href: "/agents", label: "Agentes", icon: Bot, section: "Core" },
  { href: "/workflows", label: "Workflows", icon: GitFork, section: "Core" },
  { href: "/knowledge", label: "Conocimiento", icon: BookOpen, section: "Core" },
];

export const SECONDARY_NAV_FOOTER: NavItem[] = [
  { href: "/settings", label: "Ajustes", icon: Settings },
];

export const COMMAND_ITEMS = [
  { label: "Ir a Resumen", href: "/", category: "Navegación" },
  { label: "Canales", href: "/channels", category: "Navegación" },
  { label: "IA y modelos", href: "/intelligence", category: "Navegación" },
  { label: "Agentes", href: "/agents", category: "Navegación" },
  { label: "Workflows", href: "/workflows", category: "Navegación" },
  { label: "Conocimiento", href: "/knowledge", category: "Navegación" },
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
  if (pathname.startsWith("/workflows")) {
    return {
      label: "Workflows",
      items: [{ label: "Definiciones", href: "/workflows" }],
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
  if (pathname.startsWith("/settings")) {
    return {
      label: "Ajustes",
      items: [
        { label: "Preferencias", href: "/settings" },
        { label: "Instalador", href: "/installer" },
      ],
    };
  }
  if (pathname.startsWith("/installer")) {
    return {
      label: "Instalador",
      items: [{ label: "Configuración", href: "/installer" }],
    };
  }
  return {
    label: "Resumen",
    items: [{ label: "Vista general", href: "/" }],
  };
}
