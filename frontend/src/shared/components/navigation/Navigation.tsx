"use client";

import React from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { LogOut } from "lucide-react";
import { cn } from "@/shared/utils/cn";
import { ADMIN_NAV, MAIN_NAV, isActive, type NavItem } from "@/shared/config/navigation";
import { useAuth } from "@/features/auth/context/AuthProvider";
import { useOrganization } from "@/features/settings/api/useOrganization";

/** Logo y nombre de la organización. */
export function Brand() {
  const { data } = useOrganization();
  return (
    <Link href="/" className="focus-ring flex min-w-0 items-center gap-2.5 rounded-md">
      <span
        className="flex h-7 w-7 shrink-0 items-center justify-center rounded-md bg-neutral-950 text-xs font-semibold text-white"
        aria-hidden
      >
        O
      </span>
      <span className="truncate text-sm font-semibold text-neutral-950">{data?.name ?? "OCAP"}</span>
    </Link>
  );
}

function NavLink({ item, onNavigate }: { item: NavItem; onNavigate?: () => void }) {
  const pathname = usePathname();
  const active = isActive(pathname, item.href);
  const Icon = item.icon;
  return (
    <Link
      href={item.href}
      onClick={onNavigate}
      aria-current={active ? "page" : undefined}
      className={cn(
        "focus-ring flex h-9 items-center gap-3 rounded-md px-2.5 text-sm transition-colors",
        active ? "bg-neutral-100 font-medium text-neutral-950" : "text-neutral-600 hover:bg-neutral-50 hover:text-neutral-950"
      )}
    >
      <Icon className="h-4 w-4 shrink-0" aria-hidden />
      {item.label}
    </Link>
  );
}

/** Menú según los permisos del rol, y la cuenta de la persona al pie. */
export function NavigationPanel({ onNavigate }: { onNavigate?: () => void }) {
  const { user, hasPermission, logout } = useAuth();
  const pathname = usePathname();
  const allowed = (item: NavItem) => !item.permission || hasPermission(item.permission);
  const admin = ADMIN_NAV.filter(allowed);

  return (
    <div className="flex h-full min-h-0 flex-col">
      <nav aria-label="Principal" className="flex-1 space-y-0.5 overflow-y-auto px-3 py-2">
        {MAIN_NAV.filter(allowed).map((item) => (
          <NavLink key={item.href} item={item} onNavigate={onNavigate} />
        ))}
        {admin.length > 0 && (
          <div className="mt-4 space-y-0.5 border-t border-neutral-200 pt-4">
            {admin.map((item) => (
              <NavLink key={item.href} item={item} onNavigate={onNavigate} />
            ))}
          </div>
        )}
      </nav>

      <div className="flex items-center gap-1 border-t border-neutral-200 px-3 py-3">
        <Link
          href="/account"
          onClick={onNavigate}
          aria-current={pathname === "/account" ? "page" : undefined}
          className="focus-ring min-w-0 flex-1 rounded-md px-2.5 py-1.5 hover:bg-neutral-50"
        >
          <span className="block truncate text-sm font-medium text-neutral-950">{user?.fullName}</span>
          <span className="block truncate text-xs text-neutral-600">Mi cuenta</span>
        </Link>
        <button
          type="button"
          onClick={() => void logout()}
          className="focus-ring rounded-md p-2 text-neutral-600 hover:bg-neutral-100 hover:text-neutral-950"
          aria-label="Cerrar sesión"
          title="Cerrar sesión"
        >
          <LogOut className="h-4 w-4" aria-hidden />
        </button>
      </div>
    </div>
  );
}
