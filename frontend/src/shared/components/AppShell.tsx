"use client";

import React from "react";
import { usePathname } from "next/navigation";
import { Menu, WifiOff, X } from "lucide-react";
import { isPublicRoute } from "@/shared/config/navigation";
import { Brand, NavigationPanel } from "@/shared/components/navigation/Navigation";
import { useApiReachable } from "@/shared/utils/useApiReachable";

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  if (isPublicRoute(pathname)) return <>{children}</>;

  return (
    <div className="flex h-dvh w-full overflow-hidden bg-neutral-50">
      <aside className="hidden w-60 shrink-0 flex-col border-r border-neutral-200 bg-white lg:flex">
        <div className="flex h-14 items-center px-5">
          <Brand />
        </div>
        <NavigationPanel />
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <MobileHeader />
        <ConnectionNotice />
        <main id="contenido" className="min-h-0 flex-1 overflow-y-auto">
          {children}
        </main>
      </div>
    </div>
  );
}

/** En pantallas chicas el menú vive en un panel lateral (<dialog>: foco atrapado y Escape para cerrar). */
function MobileHeader() {
  const ref = React.useRef<HTMLDialogElement>(null);
  const pathname = usePathname();
  const close = React.useCallback(() => ref.current?.close(), []);

  React.useEffect(close, [pathname, close]);

  return (
    <header className="flex h-14 shrink-0 items-center justify-between border-b border-neutral-200 bg-white px-4 lg:hidden">
      <Brand />
      <button
        type="button"
        onClick={() => ref.current?.showModal()}
        className="focus-ring -mr-2 rounded-md p-2 text-neutral-700 hover:bg-neutral-100"
        aria-label="Abrir menú"
      >
        <Menu className="h-5 w-5" aria-hidden />
      </button>
      <dialog
        ref={ref}
        aria-label="Menú"
        onClick={(e) => e.target === ref.current && close()}
        className="m-0 h-dvh max-h-none w-72 max-w-[85vw] border-r border-neutral-200 bg-white p-0 backdrop:bg-neutral-950/40"
      >
        <div className="flex h-full flex-col">
          <div className="flex h-14 items-center justify-between px-5">
            <Brand />
            <button
              type="button"
              onClick={close}
              className="focus-ring -mr-2 rounded-md p-2 text-neutral-700 hover:bg-neutral-100"
              aria-label="Cerrar menú"
            >
              <X className="h-5 w-5" aria-hidden />
            </button>
          </div>
          <NavigationPanel onNavigate={close} />
        </div>
      </dialog>
    </header>
  );
}

/** Solo aparece cuando hace falta: si el servidor no responde, lo que se haga no se guardará. */
function ConnectionNotice() {
  const reachable = useApiReachable();
  if (reachable) return null;
  return (
    <div role="status" className="flex items-center gap-2 border-b border-warning/30 bg-warning-subtle px-4 py-2 text-sm text-warning">
      <WifiOff className="h-4 w-4 shrink-0" aria-hidden />
      Sin conexión con el servidor. Reintentando…
    </div>
  );
}
