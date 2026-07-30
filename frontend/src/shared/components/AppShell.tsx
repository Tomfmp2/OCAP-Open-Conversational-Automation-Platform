"use client";

import React from "react";
import { usePathname } from "next/navigation";
import { PrimaryRail } from "@/shared/components/navigation/PrimaryRail";
import { SecondarySidebar } from "@/shared/components/navigation/SecondarySidebar";
import { Topbar } from "@/shared/components/navigation/Topbar";
import { useThemeStore } from "@/shared/stores/useThemeStore";

const MINIMAL_LAYOUT_ROUTES = ["/login"];
const PUBLIC_DIAGNOSTIC_ROUTES = ["/installer"];

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const hydrateTheme = useThemeStore((s) => s.hydrate);
  const isMinimal = MINIMAL_LAYOUT_ROUTES.includes(pathname);
  const isPublicDiagnostic = PUBLIC_DIAGNOSTIC_ROUTES.some((route) =>
    pathname.startsWith(route)
  );

  React.useEffect(() => {
    hydrateTheme();
  }, [hydrateTheme]);

  if (isMinimal) {
    return <>{children}</>;
  }

  if (isPublicDiagnostic) {
    return (
      <div className="flex h-full min-h-0 w-full flex-1 flex-col overflow-hidden bg-[radial-gradient(circle_at_top_left,_rgba(59,130,246,0.18),_transparent_40%),radial-gradient(circle_at_bottom_right,_rgba(139,92,246,0.12),_transparent_35%),var(--background)]">
        <Topbar compact />
        <main className="flex-1 overflow-y-auto p-6">{children}</main>
      </div>
    );
  }

  return (
    <div className="flex h-full min-h-0 w-full overflow-hidden bg-[radial-gradient(circle_at_top_left,_rgba(59,130,246,0.16),_transparent_34%),radial-gradient(circle_at_80%_20%,_rgba(139,92,246,0.12),_transparent_28%),var(--background)]">
      <PrimaryRail />
      <SecondarySidebar />
      <div className="flex min-w-0 flex-1 flex-col overflow-hidden">
        <Topbar />
        <main className="flex-1 overflow-y-auto p-4 sm:p-6">{children}</main>
      </div>
    </div>
  );
}
