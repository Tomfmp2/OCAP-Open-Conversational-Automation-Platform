"use client";

import React from "react";
import { Search, Bell, Sun, Moon, Globe, User } from "lucide-react";
import { useThemeStore } from "@/shared/stores/useThemeStore";
import { CommandPalette } from "./CommandPalette";

export function Topbar() {
  const { theme, toggleTheme } = useThemeStore();
  const [commandPaletteOpen, setCommandPaletteOpen] = React.useState(false);
  const [currentLocale, setCurrentLocale] = React.useState("es");

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

  return (
    <>
      <header className="h-14 bg-white dark:bg-zinc-950 border-b border-zinc-200 dark:border-zinc-800/80 px-4 flex items-center justify-between shrink-0 select-none z-10">
        {/* Command Palette Trigger Input */}
        <div className="flex items-center gap-3">
          <button
            onClick={() => setCommandPaletteOpen(true)}
            className="flex items-center gap-2.5 bg-zinc-100 dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-lg px-3 py-1.5 text-xs text-zinc-500 hover:text-zinc-900 dark:hover:text-zinc-100 hover:border-zinc-300 dark:hover:border-zinc-700 transition-colors w-72 justify-between"
          >
            <div className="flex items-center gap-2">
              <Search className="w-3.5 h-3.5 text-zinc-400" />
              <span>Buscar acciones, canales, agentes...</span>
            </div>
            <kbd className="hidden sm:inline-block bg-zinc-200 dark:bg-zinc-800 text-zinc-600 dark:text-zinc-300 text-[10px] font-mono px-1.5 py-0.5 rounded border border-zinc-300 dark:border-zinc-700">
              Ctrl K
            </kbd>
          </button>
        </div>

        {/* Right Tools & Indicators */}
        <div className="flex items-center gap-2">
          {/* Health Status Pill */}
          <div className="hidden md:flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-emerald-500/10 border border-emerald-500/20 text-emerald-600 dark:text-emerald-400 text-xs font-medium">
            <span className="w-1.5 h-1.5 rounded-full bg-emerald-500" />
            <span>Operational</span>
          </div>

          {/* Language Selector */}
          <button
            onClick={() => {
              const next = currentLocale === "es" ? "en" : currentLocale === "en" ? "de" : "es";
              setCurrentLocale(next);
            }}
            className="p-2 rounded-lg text-zinc-500 hover:text-zinc-900 dark:hover:text-zinc-100 hover:bg-zinc-100 dark:hover:bg-zinc-900 transition-colors flex items-center gap-1 text-xs font-mono"
            title="Cambiar Idioma"
          >
            <Globe className="w-4 h-4" />
            <span className="uppercase font-semibold text-[11px]">{currentLocale}</span>
          </button>

          {/* Theme Toggle */}
          <button
            onClick={toggleTheme}
            className="p-2 rounded-lg text-zinc-500 hover:text-zinc-900 dark:hover:text-zinc-100 hover:bg-zinc-100 dark:hover:bg-zinc-900 transition-colors"
            title="Alternar Tema"
          >
            {theme === "light" ? <Moon className="w-4 h-4" /> : <Sun className="w-4 h-4 text-amber-400" />}
          </button>

          {/* Notifications */}
          <button
            className="p-2 rounded-lg text-zinc-500 hover:text-zinc-900 dark:hover:text-zinc-100 hover:bg-zinc-100 dark:hover:bg-zinc-900 transition-colors relative"
            title="Notificaciones"
          >
            <Bell className="w-4 h-4" />
            <span className="absolute top-1.5 right-1.5 w-2 h-2 rounded-full bg-blue-500" />
          </button>

          <div className="w-px h-4 bg-zinc-200 dark:bg-zinc-800 mx-1" />

          {/* User Profile Dropdown */}
          <div className="flex items-center gap-2 pl-1 cursor-pointer">
            <div className="w-7 h-7 rounded-full bg-zinc-200 dark:bg-zinc-800 border border-zinc-300 dark:border-zinc-700 flex items-center justify-center text-xs font-bold text-zinc-700 dark:text-zinc-300">
              <User className="w-4 h-4" />
            </div>
            <div className="hidden lg:block text-left">
              <p className="text-xs font-semibold text-zinc-900 dark:text-zinc-100 leading-none">Admin Enterprise</p>
              <p className="text-[10px] text-zinc-500 leading-tight">admin@ocap.io</p>
            </div>
          </div>
        </div>
      </header>

      {/* Command Palette Modal */}
      <CommandPalette open={commandPaletteOpen} onClose={() => setCommandPaletteOpen(false)} />
    </>
  );
}
