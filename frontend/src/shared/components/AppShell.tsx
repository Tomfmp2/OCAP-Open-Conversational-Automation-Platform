"use client";

import React from "react";
import { usePathname } from "next/navigation";
import { PrimaryRail } from "@/shared/components/navigation/PrimaryRail";
import { SecondarySidebar } from "@/shared/components/navigation/SecondarySidebar";
import { Topbar } from "@/shared/components/navigation/Topbar";

const MINIMAL_LAYOUT_ROUTES = ["/login", "/installer"];

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const isMinimal = MINIMAL_LAYOUT_ROUTES.some((route) => pathname.startsWith(route));

  if (isMinimal) {
    return <>{children}</>;
  }

  return (
    <div className="flex h-full min-h-0 w-full overflow-hidden bg-neutral-100">
      <PrimaryRail />
      <SecondarySidebar />
      <div className="flex min-w-0 flex-1 flex-col overflow-hidden bg-neutral-100">
        <Topbar />
        <main className="flex-1 overflow-y-auto p-4 sm:p-6">{children}</main>
      </div>
    </div>
  );
}
