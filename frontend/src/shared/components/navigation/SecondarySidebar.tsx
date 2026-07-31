"use client";

import React from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { Building2 } from "lucide-react";
import { useTenantStore } from "@/shared/stores/useTenantStore";
import { cn } from "@/shared/utils/cn";
import { getSubmenuForPath } from "@/shared/config/navigation";
import { useAuth } from "@/features/auth/context/AuthProvider";

export function SecondarySidebar() {
  const pathname = usePathname();
  const { user } = useAuth();
  const { activeTenant, fetchTenants } = useTenantStore();

  React.useEffect(() => {
    void fetchTenants();
  }, [fetchTenants]);

  const submenu = getSubmenuForPath(pathname);

  return (
    <aside className="z-20 hidden w-56 shrink-0 flex-col border-r border-neutral-200 bg-neutral-50 select-none md:flex">
      <div className="border-b border-neutral-200 p-3">
        <div className="flex items-center gap-2.5 rounded-md border border-neutral-200 bg-white p-2.5">
          <div className="flex h-7 w-7 items-center justify-center rounded-md bg-neutral-950 text-white">
            <Building2 className="h-3.5 w-3.5" />
          </div>
          <div className="min-w-0">
            <p className="truncate text-xs font-semibold text-neutral-950">
              {activeTenant?.name ?? "OCAP"}
            </p>
            <p className="truncate text-[10px] text-neutral-500">
              {user?.email ?? "Sin sesión"}
            </p>
          </div>
        </div>
      </div>

      <div className="flex-1 space-y-1 overflow-y-auto p-3">
        <p className="px-2 pb-2 text-[10px] font-semibold tracking-wider text-neutral-500 uppercase">
          {submenu.label}
        </p>
        {submenu.items.map((item) => {
          const base = item.href.split("?")[0].split("#")[0];
          const isActive =
            pathname === item.href ||
            (pathname === base && !item.href.includes("?"));
          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "flex items-center rounded-md px-2.5 py-2 text-xs font-medium text-neutral-600 transition-colors hover:bg-neutral-200/70 hover:text-neutral-950",
                isActive && "bg-neutral-950 text-white hover:bg-neutral-800 hover:text-white"
              )}
            >
              <span className="truncate">{item.label}</span>
            </Link>
          );
        })}
      </div>
    </aside>
  );
}
