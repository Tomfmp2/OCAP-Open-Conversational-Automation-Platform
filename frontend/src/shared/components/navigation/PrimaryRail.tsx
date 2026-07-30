"use client";

import React from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { Sparkles } from "lucide-react";
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
    <aside className="z-30 flex w-16 shrink-0 flex-col items-center border-r border-zinc-800/80 bg-zinc-950/95 py-4 select-none">
      <Link
        href="/"
        className="mb-6 flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-blue-600 to-violet-600 text-white shadow-lg shadow-blue-500/25"
        title="OCAP Platform"
      >
        <Sparkles className="h-5 w-5" />
      </Link>

      <nav className="flex w-full flex-1 flex-col items-center gap-1.5 px-2">
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
                "group relative flex h-11 w-11 items-center justify-center rounded-xl text-zinc-400 transition-all hover:bg-zinc-800/70 hover:text-zinc-100",
                isActive &&
                  "bg-blue-600/15 text-blue-400 shadow-[inset_0_0_0_1px_rgba(59,130,246,0.35)]"
              )}
              title={item.label}
            >
              <Icon className="h-5 w-5" />
              {isActive && (
                <span className="absolute -right-2 h-5 w-1 rounded-full bg-blue-400" />
              )}
              <span className="pointer-events-none absolute left-14 z-50 rounded-md border border-zinc-800 bg-zinc-900 px-2.5 py-1 text-xs font-medium text-zinc-100 opacity-0 shadow-md transition-opacity group-hover:opacity-100">
                {item.label}
              </span>
            </Link>
          );
        })}
      </nav>

      <div className="flex w-full flex-col items-center gap-1 border-t border-zinc-800/80 px-2 pt-2">
        {SECONDARY_NAV_FOOTER.map((item) => {
          const Icon = item.icon;
          const isActive = pathname.startsWith(item.href);
          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "group relative flex h-11 w-11 items-center justify-center rounded-xl text-zinc-400 transition-all hover:bg-zinc-800/70 hover:text-zinc-100",
                isActive && "bg-blue-600/15 text-blue-400"
              )}
              title={item.label}
            >
              <Icon className="h-5 w-5" />
            </Link>
          );
        })}
      </div>
    </aside>
  );
}
