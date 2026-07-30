"use client";

import React from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  LayoutDashboard,
  MessageSquare,
  Cpu,
  Bot,
  GitFork,
  ShieldCheck,
  Settings,
  Sparkles,
  Code2,
  Activity,
  BookOpen,
} from "lucide-react";
import { cn } from "@/shared/utils/cn";

const NAV_ITEMS = [
  { href: "/", label: "Resumen General", icon: LayoutDashboard },
  { href: "/channels", label: "Canales", icon: MessageSquare },
  { href: "/intelligence", label: "IA & Modelos", icon: Cpu },
  { href: "/agents", label: "Agentes", icon: Bot },
  { href: "/workflows", label: "Workflows", icon: GitFork },
  { href: "/knowledge", label: "Knowledge Base", icon: BookOpen },
  { href: "/monitoring", label: "Monitoreo", icon: Activity },
  { href: "/developer", label: "Developer Center", icon: Code2 },
  { href: "/security", label: "Seguridad", icon: ShieldCheck },
];

export function PrimaryRail() {
  const pathname = usePathname();

  return (
    <aside className="w-16 bg-zinc-950 flex flex-col items-center py-4 border-r border-zinc-800 z-30 shrink-0 select-none">
      {/* Brand Header */}
      <Link
        href="/"
        className="w-10 h-10 rounded-xl bg-blue-600 flex items-center justify-center text-white mb-6 hover:bg-blue-500 transition-colors shadow-lg shadow-blue-500/20"
        title="OCAP Platform v1.6.0"
      >
        <Sparkles className="w-5 h-5" />
      </Link>

      {/* Navigation Items */}
      <nav className="flex-1 w-full flex flex-col items-center gap-1.5 px-2">
        {NAV_ITEMS.map((item) => {
          const isActive = pathname === item.href || (item.href !== "/" && pathname.startsWith(item.href));
          const Icon = item.icon;

          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "w-11 h-11 rounded-lg flex items-center justify-center text-zinc-400 hover:text-zinc-100 hover:bg-zinc-800/60 transition-all group relative",
                isActive && "bg-blue-600/15 text-blue-400 hover:bg-blue-600/20 hover:text-blue-300 font-medium"
              )}
              title={item.label}
            >
              <Icon className="w-5 h-5" />
              {/* Tooltip Label */}
              <span className="absolute left-14 bg-zinc-900 text-zinc-100 text-xs font-medium px-2.5 py-1 rounded-md shadow-md border border-zinc-800 whitespace-nowrap opacity-0 pointer-events-none group-hover:opacity-100 transition-opacity z-50">
                {item.label}
              </span>
            </Link>
          );
        })}
      </nav>

      {/* Settings Bottom Footer */}
      <div className="w-full flex flex-col items-center px-2 pt-2 border-t border-zinc-800/80">
        <Link
          href="/settings"
          className={cn(
            "w-11 h-11 rounded-lg flex items-center justify-center text-zinc-400 hover:text-zinc-100 hover:bg-zinc-800/60 transition-all group relative",
            pathname === "/settings" && "bg-blue-600/15 text-blue-400"
          )}
          title="Configuración"
        >
          <Settings className="w-5 h-5" />
          <span className="absolute left-14 bg-zinc-900 text-zinc-100 text-xs font-medium px-2.5 py-1 rounded-md shadow-md border border-zinc-800 whitespace-nowrap opacity-0 pointer-events-none group-hover:opacity-100 transition-opacity z-50">
            Configuración
          </span>
        </Link>
      </div>
    </aside>
  );
}
