"use client";

import React from "react";
import { usePathname } from "next/navigation";
import { PrimaryRail } from "@/shared/components/navigation/PrimaryRail";
import { SecondarySidebar } from "@/shared/components/navigation/SecondarySidebar";
import { Topbar } from "@/shared/components/navigation/Topbar";

const MINIMAL_LAYOUT_ROUTES = ["/login"];

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const isMinimal = MINIMAL_LAYOUT_ROUTES.includes(pathname);

  if (isMinimal) {
    return <>{children}</>;
  }

  return (
    <>
      <PrimaryRail />
      <SecondarySidebar />
      <div className="flex-1 flex flex-col min-w-0 h-full overflow-hidden">
        <Topbar />
        <main className="flex-1 overflow-y-auto p-6 bg-zinc-100/50 dark:bg-zinc-900/30">
          {children}
        </main>
      </div>
    </>
  );
}
