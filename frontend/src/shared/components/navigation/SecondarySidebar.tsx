"use client";

import React from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  ChevronDown,
  Search,
  CheckCircle2,
  Building2,
  Radio,
  SlidersHorizontal,
} from "lucide-react";
import { useTenantStore } from "@/shared/stores/useTenantStore";
import { cn } from "@/shared/utils/cn";

export function SecondarySidebar() {
  const pathname = usePathname();
  const { activeTenant, tenants, setActiveTenant } = useTenantStore();
  const [tenantDropdownOpen, setTenantDropdownOpen] = React.useState(false);
  const [searchQuery, setSearchQuery] = React.useState("");

  const getSubmenuItems = () => {
    if (pathname.startsWith("/channels")) {
      return [
        { label: "Telegram Adapter", href: "/channels?provider=Telegram", badge: "Online" },
        { label: "WhatsApp Business", href: "/channels?provider=WhatsApp", badge: "Connected" },
        { label: "Google Workspace", href: "/channels?provider=Google", badge: "Active" },
        { label: "Slack / Teams", href: "/channels?provider=Enterprise", badge: "Idle" },
      ];
    }
    if (pathname.startsWith("/intelligence")) {
      return [
        { label: "Proveedores Activos", href: "/intelligence#providers" },
        { label: "Credential Vault (AES-256)", href: "/intelligence#vault" },
        { label: "Modelos & Embeddings", href: "/intelligence#models" },
        { label: "Failover & Latencia", href: "/intelligence#failover" },
      ];
    }
    if (pathname.startsWith("/agents")) {
      return [
        { label: "Enterprise Assistant Core", href: "/agents#core" },
        { label: "Sub-Agentes Especializados", href: "/agents#subagents" },
        { label: "Tools Registry", href: "/agents#tools" },
        { label: "Trazas de Razonamiento", href: "/agents#traces" },
      ];
    }
    return [
      { label: "Vista General Dashboard", href: "/", badge: "Live" },
      { label: "Actividad Reciente", href: "/#activity" },
      { label: "Salud del Núcleo", href: "/#health" },
      { label: "Métricas Financieras IA", href: "/#cost" },
    ];
  };

  const submenuItems = getSubmenuItems();

  return (
    <aside className="w-60 bg-zinc-900/60 dark:bg-zinc-950/80 backdrop-blur border-r border-zinc-200 dark:border-zinc-800/80 flex flex-col z-20 shrink-0 select-none">
      {/* Tenant Selector Dropdown */}
      <div className="p-3 border-b border-zinc-200 dark:border-zinc-800/80 relative">
        <button
          onClick={() => setTenantDropdownOpen(!tenantDropdownOpen)}
          className="w-full flex items-center justify-between p-2 rounded-lg bg-zinc-100 dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 hover:border-zinc-300 dark:hover:border-zinc-700 transition-colors text-left"
        >
          <div className="flex items-center gap-2.5 min-w-0">
            <div className="w-6 h-6 rounded bg-blue-600/20 text-blue-500 flex items-center justify-center font-bold text-xs">
              <Building2 className="w-3.5 h-3.5" />
            </div>
            <div className="truncate">
              <p className="text-xs font-semibold text-zinc-900 dark:text-zinc-100 truncate">{activeTenant.name}</p>
              <p className="text-[10px] text-zinc-500 truncate">{activeTenant.slug}</p>
            </div>
          </div>
          <ChevronDown className="w-4 h-4 text-zinc-400 shrink-0 ml-1" />
        </button>

        {tenantDropdownOpen && (
          <div className="absolute top-16 left-3 right-3 bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-lg shadow-xl py-1 z-50">
            <p className="px-3 py-1.5 text-[10px] font-semibold uppercase tracking-wider text-zinc-400">Tenants Disponibles</p>
            {tenants.map((tenant) => (
              <button
                key={tenant.id}
                onClick={() => {
                  setActiveTenant(tenant);
                  setTenantDropdownOpen(false);
                }}
                className={cn(
                  "w-full text-left px-3 py-2 text-xs flex items-center justify-between hover:bg-zinc-100 dark:hover:bg-zinc-800 transition-colors",
                  activeTenant.id === tenant.id && "font-semibold text-blue-500 bg-blue-50/50 dark:bg-blue-950/30"
                )}
              >
                <span>{tenant.name}</span>
                {activeTenant.id === tenant.id && <CheckCircle2 className="w-3.5 h-3.5 text-blue-500" />}
              </button>
            ))}
          </div>
        )}
      </div>

      {/* Quick Search */}
      <div className="p-3 border-b border-zinc-200 dark:border-zinc-800/80">
        <div className="relative">
          <Search className="w-3.5 h-3.5 absolute left-2.5 top-2.5 text-zinc-400" />
          <input
            type="text"
            placeholder="Filtrar menú..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full bg-zinc-100 dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-md pl-8 pr-3 py-1.5 text-xs text-zinc-900 dark:text-zinc-100 placeholder:text-zinc-400 focus:outline-none focus:ring-1 focus:ring-blue-500"
          />
        </div>
      </div>

      {/* Dynamic Submenu List */}
      <div className="flex-1 overflow-y-auto p-3 space-y-1">
        <p className="px-2 pb-2 text-[10px] font-semibold uppercase tracking-wider text-zinc-400 flex items-center justify-between">
          <span>Secciones</span>
          <SlidersHorizontal className="w-3 h-3 text-zinc-400" />
        </p>

        {submenuItems
          .filter((item) => item.label.toLowerCase().includes(searchQuery.toLowerCase()))
          .map((item) => (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "flex items-center justify-between px-2.5 py-1.5 rounded-md text-xs font-medium text-zinc-600 dark:text-zinc-400 hover:text-zinc-900 dark:hover:text-zinc-100 hover:bg-zinc-200/50 dark:hover:bg-zinc-800/50 transition-colors",
                pathname === item.href && "text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-950/40 font-semibold"
              )}
            >
              <div className="flex items-center gap-2 truncate">
                <Radio className="w-3 h-3 text-blue-500" />
                <span className="truncate">{item.label}</span>
              </div>
              {item.badge && (
                <span className="text-[9px] px-1.5 py-0.5 rounded bg-zinc-200 dark:bg-zinc-800 text-zinc-700 dark:text-zinc-300 font-mono">
                  {item.badge}
                </span>
              )}
            </Link>
          ))}
      </div>

      {/* System Status Footer */}
      <div className="p-3 border-t border-zinc-200 dark:border-zinc-800/80 bg-zinc-50 dark:bg-zinc-900/40">
        <div className="flex items-center justify-between text-[11px] text-zinc-500">
          <span className="flex items-center gap-1.5">
            <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
            Núcleo OCAP Online
          </span>
          <span className="font-mono text-[10px]">v1.6.0</span>
        </div>
      </div>
    </aside>
  );
}
