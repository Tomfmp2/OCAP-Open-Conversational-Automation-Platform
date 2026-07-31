"use client";

import React from "react";
import { Search, LogOut, User } from "lucide-react";
import { useAuth } from "@/features/auth/context/AuthProvider";
import { CommandPalette } from "./CommandPalette";
import { Badge } from "@/shared/components/ui";
import { apiClient } from "@/shared/api/apiClient";

interface TopbarProps {
  compact?: boolean;
}

export function Topbar({ compact = false }: TopbarProps) {
  const { user, logout, isAuthenticated } = useAuth();
  const [commandPaletteOpen, setCommandPaletteOpen] = React.useState(false);
  const [healthLabel, setHealthLabel] = React.useState<string>("…");

  React.useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "k") {
        e.preventDefault();
        setCommandPaletteOpen((prev) => !prev);
      }
    };
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, []);

  React.useEffect(() => {
    let cancelled = false;
    void apiClient
      .get<{ status?: string; Status?: string }>("/api/health", { skipAuth: true })
      .then((data) => {
        if (cancelled) return;
        setHealthLabel(data?.status || data?.Status || "Desconocido");
      })
      .catch(() => {
        if (!cancelled) setHealthLabel("No disponible");
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const healthTone =
    healthLabel.toLowerCase() === "healthy" || healthLabel.toLowerCase() === "ok"
      ? "success"
      : healthLabel.toLowerCase() === "degraded"
        ? "warning"
        : "neutral";

  return (
    <>
      <header className="z-10 flex h-12 shrink-0 items-center justify-between border-b border-neutral-200 bg-white px-4 select-none">
        <div className="flex items-center gap-3">
          {!compact && (
            <button
              type="button"
              onClick={() => setCommandPaletteOpen(true)}
              className="flex w-52 items-center justify-between gap-2 rounded-md border border-neutral-300 bg-neutral-50 px-3 py-1.5 text-xs text-neutral-500 transition-colors hover:border-neutral-400 hover:text-neutral-900 sm:w-64"
            >
              <span className="flex items-center gap-2">
                <Search className="h-3.5 w-3.5 text-neutral-400" />
                <span>Buscar…</span>
              </span>
              <kbd className="hidden rounded border border-neutral-300 bg-white px-1.5 py-0.5 font-mono text-[10px] text-neutral-600 sm:inline-block">
                Ctrl K
              </kbd>
            </button>
          )}
          {compact && (
            <div>
              <p className="text-sm font-semibold text-neutral-950">OCAP</p>
              <p className="text-[11px] text-neutral-500">Instalación</p>
            </div>
          )}
        </div>

        <div className="flex items-center gap-2">
          <Badge tone={healthTone}>API {healthLabel}</Badge>

          {isAuthenticated && (
            <>
              <div className="mx-1 hidden h-4 w-px bg-neutral-200 sm:block" />
              <div className="hidden items-center gap-2 pl-1 lg:flex">
                <div className="flex h-7 w-7 items-center justify-center rounded-md border border-neutral-300 bg-neutral-100 text-neutral-700">
                  <User className="h-4 w-4" />
                </div>
                <div className="text-left">
                  <p className="text-xs leading-none font-semibold text-neutral-950">
                    {user?.fullName || user?.roleName || "Usuario"}
                  </p>
                  <p className="text-[10px] leading-tight text-neutral-500">
                    {user?.email || "—"}
                  </p>
                </div>
              </div>
              <button
                type="button"
                onClick={() => void logout()}
                className="rounded-md p-2 text-neutral-500 transition-colors hover:bg-neutral-100 hover:text-neutral-950"
                title="Cerrar sesión"
              >
                <LogOut className="h-4 w-4" />
              </button>
            </>
          )}
        </div>
      </header>

      <CommandPalette
        open={commandPaletteOpen}
        onClose={() => setCommandPaletteOpen(false)}
      />
    </>
  );
}
