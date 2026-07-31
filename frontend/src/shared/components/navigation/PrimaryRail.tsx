"use client";

import React from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/shared/utils/cn";
import { PRIMARY_NAV, SECONDARY_NAV_FOOTER } from "@/shared/config/navigation";
import { useAuth } from "@/features/auth/context/AuthProvider";

export function PrimaryRail() {
  const pathname = usePathname();
  const { hasPermission, isAuthenticated } = useAuth();

  const items = PRIMARY_NAV.filter((item) => {
    if (!item.permission) return true;
    return isAuthenticated && hasPermission(item.permission);
  });

  return (
    <aside className="z-30 flex w-14 shrink-0 flex-col items-center border-r border-neutral-200 bg-white py-4 select-none">
      <Link
        href="/"
        className="mb-6 flex h-9 w-9 items-center justify-center rounded-md bg-neutral-950 text-[11px] font-semibold tracking-tight text-white"
        title="OCAP"
      >
        O
      </Link>

      <nav className="flex w-full flex-1 flex-col items-center gap-1 px-2">
        {items.map((item) => {
          const isActive =
            pathname === item.href ||
            (item.href !== "/" && pathname.startsWith(item.href));
          const Icon = item.icon;

          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "group relative flex h-10 w-10 items-center justify-center rounded-md text-neutral-500 transition-colors hover:bg-neutral-100 hover:text-neutral-950",
                isActive && "bg-neutral-950 text-white hover:bg-neutral-800 hover:text-white"
              )}
              title={item.label}
            >
              <Icon className="h-4 w-4" />
              <span className="pointer-events-none absolute left-12 z-50 rounded-md border border-neutral-200 bg-white px-2 py-1 text-xs font-medium text-neutral-900 opacity-0 shadow-sm transition-opacity group-hover:opacity-100">
                {item.label}
              </span>
            </Link>
          );
        })}
      </nav>

      <div className="flex w-full flex-col items-center gap-1 border-t border-neutral-200 px-2 pt-2">
        {SECONDARY_NAV_FOOTER.map((item) => {
          const Icon = item.icon;
          const isActive = pathname.startsWith(item.href);
          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "group relative flex h-10 w-10 items-center justify-center rounded-md text-neutral-500 transition-colors hover:bg-neutral-100 hover:text-neutral-950",
                isActive && "bg-neutral-950 text-white hover:bg-neutral-800 hover:text-white"
              )}
              title={item.label}
            >
              <Icon className="h-4 w-4" />
            </Link>
          );
        })}
      </div>
    </aside>
  );
}
