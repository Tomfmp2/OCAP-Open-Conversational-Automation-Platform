"use client";

import React from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { Building2, Search } from "lucide-react";
import { useTenantStore } from "@/shared/stores/useTenantStore";
import { cn } from "@/shared/utils/cn";
import { getSubmenuForPath } from "@/shared/config/navigation";
import { useAuth } from "@/features/auth/context/AuthProvider";

export function SecondarySidebar() {
  const pathname = usePathname();
  const { user } = useAuth();
  const { activeTenant, fetchTenants } = useTenantStore();
  const [searchQuery, setSearchQuery] = React.useState("");

  React.useEffect(() => {
    void fetchTenants();
  }, [fetchTenants]);

  const submenu = getSubmenuForPath(pathname);
  const items = submenu.items.filter((item) =>
    item.label.toLowerCase().includes(searchQuery.toLowerCase())
  );

  return (
    <aside className="z-20 hidden w-60 shrink-0 flex-col border-r border-zinc-800/70 bg-zinc-950/55 backdrop-blur-xl select-none md:flex">
      <div className="border-b border-zinc-800/80 p-3">
        <div className="flex items-center gap-2.5 rounded-xl border border-zinc-800 bg-zinc-900/80 p-2.5">
          <div className="flex h-7 w-7 items-center justify-center rounded-lg bg-blue-600/20 text-blue-400">
            <Building2 className="h-3.5 w-3.5" />
          </div>
          <div className="min-w-0">
            <p className="truncate text-xs font-semibold text-zinc-100">
              {activeTenant?.name ?? "OCAP"}
            </p>
            <p className="truncate text-[10px] text-zinc-500">
              Tenant del token · {user?.email ?? "—"}
            </p>
          </div>
        </div>
        <p className="mt-2 px-1 text-[10px] text-zinc-500">
          El aislamiento multi-tenant se resuelve desde el JWT; el selector no
          cambia de organización.
        </p>
      </div>

      <div className="border-b border-zinc-800/80 p-3">
        <div className="relative">
          <Search className="absolute top-2.5 left-2.5 h-3.5 w-3.5 text-zinc-400" />
          <input
            type="text"
            placeholder="Filtrar sección..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full rounded-lg border border-zinc-800 bg-zinc-900 py-1.5 pr-3 pl-8 text-xs text-zinc-100 placeholder:text-zinc-500 focus-ring"
          />
        </div>
      </div>

      <div className="flex-1 space-y-1 overflow-y-auto p-3">
        <p className="px-2 pb-2 text-[10px] font-semibold tracking-wider text-zinc-500 uppercase">
          {submenu.label}
        </p>
        {items.map((item) => {
          const isActive =
            pathname === item.href ||
            (typeof window !== "undefined" &&
              window.location.pathname + window.location.search === item.href);
          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "flex items-center justify-between rounded-lg px-2.5 py-2 text-xs font-medium text-zinc-400 transition-colors hover:bg-zinc-800/60 hover:text-zinc-100",
                isActive &&
                  "bg-gradient-to-r from-blue-600/20 to-violet-600/10 text-blue-300 shadow-[inset_0_0_0_1px_rgba(59,130,246,0.25)]"
              )}
            >
              <span className="truncate">{item.label}</span>
              {isActive && (
                <span className="h-4 w-1 rounded-full bg-white/80" />
              )}
            </Link>
          );
        })}
      </div>

      <div className="border-t border-zinc-800/80 bg-zinc-950/40 p-3">
        <div className="flex items-center justify-between text-[11px] text-zinc-500">
          <span className="flex items-center gap-1.5">
            <span className="h-2 w-2 rounded-full bg-emerald-500" />
            Sesión activa
          </span>
          <span className="font-mono text-[10px]">OCAP</span>
        </div>
      </div>
    </aside>
  );
}
