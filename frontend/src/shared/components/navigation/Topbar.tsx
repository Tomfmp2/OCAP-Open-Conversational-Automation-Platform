"use client";

import React from "react";
import { Search, Sun, Moon, LogOut, User } from "lucide-react";
import { useThemeStore } from "@/shared/stores/useThemeStore";
import { useAuth } from "@/features/auth/context/AuthProvider";
import { CommandPalette } from "./CommandPalette";
import { Badge } from "@/shared/components/ui";
import { apiClient } from "@/shared/api/apiClient";

interface TopbarProps {
  compact?: boolean;
}

export function Topbar({ compact = false }: TopbarProps) {
  const { theme, toggleTheme } = useThemeStore();
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
        setHealthLabel(data?.status || data?.Status || "Unknown");
      })
      .catch(() => {
        if (!cancelled) setHealthLabel("Unavailable");
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const healthTone =
    healthLabel.toLowerCase() === "healthy"
      ? "success"
      : healthLabel.toLowerCase() === "degraded"
        ? "warning"
        : "danger";

  return (
    <>
      <header className="z-10 flex h-14 shrink-0 items-center justify-between border-b border-zinc-200/80 bg-white/70 px-4 backdrop-blur-xl dark:border-zinc-800/80 dark:bg-zinc-950/60 select-none">
        <div className="flex items-center gap-3">
          {!compact && (
            <button
              type="button"
              onClick={() => setCommandPaletteOpen(true)}
              className="flex w-56 items-center justify-between gap-2 rounded-xl border border-zinc-200 bg-zinc-100 px-3 py-1.5 text-xs text-zinc-500 transition-colors hover:border-zinc-300 hover:text-zinc-900 sm:w-72 dark:border-zinc-800 dark:bg-zinc-900 dark:hover:border-zinc-700 dark:hover:text-zinc-100"
            >
              <span className="flex items-center gap-2">
                <Search className="h-3.5 w-3.5 text-zinc-400" />
                <span>Buscar módulos…</span>
              </span>
              <kbd className="hidden rounded border border-zinc-300 bg-zinc-200 px-1.5 py-0.5 font-mono text-[10px] text-zinc-600 sm:inline-block dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-300">
                Ctrl K
              </kbd>
            </button>
          )}
          {compact && (
            <div>
              <p className="text-sm font-bold text-zinc-900 dark:text-zinc-100">
                Diagnóstico OCAP
              </p>
              <p className="text-[11px] text-zinc-500">Acceso público de instalación</p>
            </div>
          )}
        </div>

        <div className="flex items-center gap-2">
          <Badge tone={healthTone as "success" | "warning" | "danger"}>
            {healthLabel}
          </Badge>

          <button
            type="button"
            onClick={toggleTheme}
            className="rounded-lg p-2 text-zinc-500 transition-colors hover:bg-zinc-100 hover:text-zinc-900 dark:hover:bg-zinc-900 dark:hover:text-zinc-100"
            title="Alternar tema"
          >
            {theme === "light" ? (
              <Moon className="h-4 w-4" />
            ) : (
              <Sun className="h-4 w-4 text-amber-400" />
            )}
          </button>

          {isAuthenticated && (
            <>
              <div className="mx-1 hidden h-4 w-px bg-zinc-200 sm:block dark:bg-zinc-800" />
              <div className="hidden items-center gap-2 pl-1 lg:flex">
                <div className="flex h-7 w-7 items-center justify-center rounded-full border border-zinc-300 bg-zinc-200 text-zinc-700 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-300">
                  <User className="h-4 w-4" />
                </div>
                <div className="text-left">
                  <p className="text-xs leading-none font-semibold text-zinc-900 dark:text-zinc-100">
                    {user?.fullName || user?.roleName || "Usuario"}
                  </p>
                  <p className="text-[10px] leading-tight text-zinc-500">
                    {user?.email || "—"}
                  </p>
                </div>
              </div>
              <button
                type="button"
                onClick={() => void logout()}
                className="rounded-lg p-2 text-zinc-500 transition-colors hover:bg-zinc-100 hover:text-zinc-900 dark:hover:bg-zinc-900 dark:hover:text-zinc-100"
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
